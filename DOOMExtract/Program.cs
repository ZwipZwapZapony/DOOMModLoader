using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace DOOMExtract;
class Program
{
	enum ProgramMode
	{
		Extract,
		Repack,
		CreatePatch,
		Delete,
	}

	static List<string> filterNames = new(); // File name wildcard filters
	static List<string> filterTypes = new(); // Resource type exact-match filters



	static void PrintUsage(bool fullHelp, ProgramMode mode)
	{
		string executable = "./DOOMExtract";
		if (OperatingSystem.IsWindows())
			executable = "DOOMExtract.exe";

		Console.WriteLine("Usage:");

		if (fullHelp || mode == ProgramMode.Extract)
		{
			Console.WriteLine("Extraction: " + executable + " <pathToIndexFile> [destFolder] [options]");
			Console.WriteLine("    If destFolder isn't specified, a folder will be created next to the .index/.pindex file");
			Console.WriteLine("    Files with fileType != \"file\" will have the fileType appended to the file name,");
			Console.WriteLine("    e.g. \"allowoverlays.decl;renderParm\" for fileType \"renderParm\"");
			Console.WriteLine("    A list of each file's ID will be written to [destFolder]/fileIds.txt");
			Console.WriteLine();
			Console.WriteLine("    \"Options\" can be any/all or none of the following:");
			if (fullHelp)
			{
				Console.WriteLine("    -filter <filters> - Filter by partial file names, e.g. \"-filter intro.entities decls/entitydef/\"");
				Console.WriteLine("    -force            - Extract even if the destination folder isn't empty");
				Console.WriteLine("    -nopindex         - Stop DOOMExtract from loading .pindex instead of .index files");
				Console.WriteLine("    -simulate         - Print would-be extracted files without actually extracting them");
				Console.WriteLine("    -type <types>     - Filter by types, e.g. \"-type renderParm weapon\"");
				Console.WriteLine("    -verbose          - Display more information while extracting resources");
			}
			else // Short list of all option names
			{
				Console.WriteLine("    -fileids, -filter <filters>, -force, -nopindex, -simulate, -type <types>, -verbose");
				Console.WriteLine("    See " + executable + " -help for more information");
			}
			Console.WriteLine();
		}
		if (fullHelp || mode == ProgramMode.Repack)
		{
			Console.WriteLine("Repacking: " + executable + " <pathToIndexFile> -repack <repackFolder>");
			Console.WriteLine("    Will repack the resources with the files in the repack folder");
			Console.WriteLine("    Files that don't already exist in the resources will be added");
			Console.WriteLine("    To set a new file's fileType, append the fileType to its file name,");
			Console.WriteLine("    e.g. \"allowoverlays.decl;renderParm\" for fileType \"renderParm\"");
			Console.WriteLine("    To set/change a files ID, add a line for it in <repackFolder>/fileIds.txt");
			Console.WriteLine("    of the format [full file path]=[file id]");
			Console.WriteLine("    e.g. \"generated/decls/renderparm/allowoverlays.decl=1337\"");
			Console.WriteLine("    (Note that you should only rebuild the latest patch index file,");
			Console.WriteLine("    as patches rely on the data in earlier files!)");
			Console.WriteLine();
		}
		if (fullHelp || mode == ProgramMode.CreatePatch)
		{
			Console.WriteLine("Patch creation: " + executable + " <pathToLatestPatchIndex> -createPatch <patchContentsFolder>");
			Console.WriteLine("    Allows you to create your own patch files");
			Console.WriteLine("    Works like repacking above, but the resulting patch files will");
			Console.WriteLine("    only contain the files you've added/changed");
			Console.WriteLine("    Make sure to point it to the highest-numbered .pindex file!");
			Console.WriteLine("    Once completed, a new .patch/.pindex file pair should be created");
			Console.WriteLine();
		}
		if (fullHelp || mode == ProgramMode.Delete)
		{
			Console.WriteLine("Deleting files: " + executable + " <pathToIndexFile> -delete <file1> [file2] [file3] [...]");
			Console.WriteLine("    Will delete files from the resources container");
			Console.WriteLine("    Full filepaths should be specified, e.g. \"generated/decls/renderparm/allowoverlays.decl\"");
			Console.WriteLine("    If a file isn't found in the container a warning will be given");
			Console.WriteLine("    This should only be used on the latest patch file, as modifying");
			Console.WriteLine("    earlier patch files may break later ones");
			Console.WriteLine();
		}

		Console.WriteLine("Show help: " + executable + " -help");
		if (fullHelp)
			Console.WriteLine("    Displays this text");
		else switch (mode)
		{
			case ProgramMode.Extract:     Console.WriteLine("    Displays help for all options, not just extraction");     break;
			case ProgramMode.Repack:      Console.WriteLine("    Displays help for all options, not just repacking");      break;
			case ProgramMode.CreatePatch: Console.WriteLine("    Displays help for all options, not just patch creation"); break;
			case ProgramMode.Delete:      Console.WriteLine("    Displays help for all options, not just deleting files"); break;
		}
	}

	static int Main(string[] args)
	{
		Console.WriteLine("DOOMExtract originally by infogram, v0.3 by Zwip-Zwap Zapony");
		Console.WriteLine("https://github.com/ZwipZwapZapony/DOOMModLoader");
		Console.WriteLine();

		string indexFilePath = null;
		bool force = false;
		bool noPIndex = false;
		bool quietMode = false;
		bool simulate = false;
		bool verbose = false;

		ProgramMode mode = ProgramMode.Extract; // Default to extraction mode
		List<string> pathArgs = new(); // Destination/Repack folder, or files to delete
		for (int i = 0; i < args.Length; i++) // Parse command-line arguments
		{
			string arg = args[i].Replace("--", "-").Replace('/', '-'); // Support "-", "--", and "/" prefixes
			switch (arg.ToLowerInvariant()) // Case-insensitive
			{
				case "-?":
				case "-h":
				case "-help":
					PrintUsage(true, mode);
					return 0;
				case "-createpatch":
					mode = ProgramMode.CreatePatch;
					break;
				case "-delete":
					mode = ProgramMode.Delete;
					break;
				case "-extract":
					mode = ProgramMode.Extract;
					break;
				case "-filter":
				case "-type":
					if (pathArgs.Count == 0)
					{
						// With "./DOOMExtract abc -filter def ghi", it's ambiguous as to whether "ghi" is a second filter
						// or it's the destination, so it's safest to just require the destination beforehand
						Console.WriteLine("Please specify both an .index/.pindex file and destination folder before \"" + arg + "\"");
						return 1;
					}

					List<string> list = ((arg == "-filter") ? filterNames : filterTypes);
					for (; i+1 < args.Length;) // Look for upcoming filters
					{
						if (args[i+1][0] == '-') // Stop when we reach another "-" (but not "/") option
							break;
						else
							list.Add(args[++i].ToLowerInvariant()); // Case-insensitive
					}
					if (list.Count == 0)
					{
						Console.WriteLine("\"" + arg + "\" was used, but no filters were specified!");
						if (i+1 != args.Length) // If "-filter" wasn't the final option, then the filter was skipped due to "-"
							Console.WriteLine("(Filters cannot start with \"-\".)");
						return 1;
					}
					break;
				case "-force":
					force = true;
					break;
				case "-nopindex":
					noPIndex = true;
					break;
				case "-quiet":
					quietMode = true;
					break;
				case "-repack":
					mode = ProgramMode.Repack;
					break;
				case "-simulate":
					simulate = true;
					break;
				case "-verbose":
					verbose = true;
					break;
				default:
					if (args[i][0] == '-') // Warn about unrecognised "-" and "--" commands, but not "/"
					{
						Console.WriteLine("Unknown option \"" + arg + "\"!");
						return 1;
					}
					else if (String.IsNullOrEmpty(indexFilePath))
						indexFilePath = args[i]; // Use "args[i]" instead of "arg" to un-replace "--" and "/"
					else
						pathArgs.Add(args[i]);
					break;
			}
		}

		if (String.IsNullOrEmpty(indexFilePath) // No .index/.pindex file specified?
		|| (pathArgs.Count <= 0 && mode != ProgramMode.Extract) // The destination is only optional for extraction
		|| (pathArgs.Count > 1 && mode != ProgramMode.Delete) // Only deletion supports more than one extra argument
		|| ((noPIndex || simulate) && mode != ProgramMode.Extract)) // Don't falsely pretend to suport "-nopindex"/"-simulate" in other modes
		{
			PrintUsage(false, mode);
			return 1;
		}

		// If an .index file was specified but a .pindex file exists, use the latter for extraction
		if (File.Exists(Path.ChangeExtension(indexFilePath, ".pindex"))
		&& mode == ProgramMode.Extract
		&& !noPIndex)
			indexFilePath = Path.ChangeExtension(indexFilePath, ".pindex");

		Console.WriteLine($"Loading {indexFilePath}...");
		var index = new DOOMResourceIndex(indexFilePath);
		if(!index.Load())
		{
			Console.WriteLine("Failed to load index file for some reason, is it a valid DOOM index file?");
			return 1;
		}

		Console.WriteLine($"Index loaded ({index.Entries.Count} files)" + (!quietMode ? ", data file contents:" : ""));

		if (!quietMode)
		{
			var pfis = new Dictionary<int, int>();

			foreach (var entry in index.Entries)
			{
				if (!pfis.ContainsKey(entry.PatchFileNumber))
					pfis.Add(entry.PatchFileNumber, 0);
				pfis[entry.PatchFileNumber]++;
			}

			var pfiKeys = pfis.Keys.ToList();
			pfiKeys.Sort();

			int total = 0;
			foreach (var key in pfiKeys)
			{
				var resName = Path.GetFileName(index.ResourceFilePath(key));
				Console.WriteLine($"    {resName}: {pfis[key]} files");
				total += pfis[key];
			}

			Console.WriteLine();
		}

		if (mode == ProgramMode.CreatePatch)
		{
			if (!Directory.Exists(pathArgs[0]))
			{
				Console.WriteLine($"Patch folder \"{Path.GetFullPath(pathArgs[0])}\" doesn't exist!");
				return 1;
			}

			// clone old index and increment the patch file number

			byte pfi = (byte)(index.PatchFileNumber + 1);
			var destPath = Path.ChangeExtension(index.ResourceFilePath(pfi), ".pindex");
			index.Close();

			if (File.Exists(destPath))
				File.Delete(destPath); // !!!!

			File.Copy(indexFilePath, destPath);
			indexFilePath = destPath;

			index = new DOOMResourceIndex(destPath);
			if(!index.Load())
			{
				Console.WriteLine("Copied patch file failed to load? (this should never happen!)");
				return 1;
			}
			index.PatchFileNumber = pfi;
		}

		if (mode is ProgramMode.Repack or ProgramMode.CreatePatch)
		{
			// REPACK (and patch creation) MODE!!!
			if (!Directory.Exists(pathArgs[0]))
			{
				Console.WriteLine($"Repack folder \"{Path.GetFullPath(pathArgs[0])}\" doesn't exist!");
				return 1;
			}

			var resFile = index.ResourceFilePath(index.PatchFileNumber);

			Console.WriteLine((mode == ProgramMode.CreatePatch ? "Creating" : "Repacking") + $" {Path.GetFileName(indexFilePath)} from folder \"{Path.GetFullPath(pathArgs[0])}\"...");

			index.Rebuild(resFile + "_tmp", pathArgs[0], true);
			index.Close();
			if (!File.Exists(resFile + "_tmp"))
			{
				Console.WriteLine("Failed to create new resource data file!");
				return 1;
			}

			if (File.Exists(resFile))
				File.Delete(resFile);

			File.Move(resFile + "_tmp", resFile);
			Console.WriteLine(mode == ProgramMode.CreatePatch ? "Patch file created!" : "Repack complete!");
			return 0;
		}

		if(mode == ProgramMode.Delete)
		{
			// DELETE MODE!!
			int deleted = 0;
			foreach (string realPath in pathArgs)
			{
				string path = realPath.Replace('\\', '/').ToLowerInvariant();

				int delIdx = -1;
				for (int i = 0; i < index.Entries.Count; i++)
				{
					if (index.Entries[i].GetFullName().ToLowerInvariant() == path)
					{
						delIdx = i;
						break;
					}
				}

				if (delIdx == -1)
					Console.WriteLine($"Failed to find file {realPath} in container");
				else
				{
					index.Entries.RemoveAt(delIdx);
					deleted++;
					Console.WriteLine($"Deleted {realPath}!");
				}
			}


			if (deleted > 0)
			{
				Console.WriteLine("Repacking/rebuilding resources file...");
				index.Rebuild(index.ResourceFilePath(index.PatchFileNumber) + "_tmp", String.Empty, true);
				index.Close();
				File.Delete(index.ResourceFilePath(index.PatchFileNumber));
				File.Move(index.ResourceFilePath(index.PatchFileNumber) + "_tmp", index.ResourceFilePath(index.PatchFileNumber));
			}
			Console.WriteLine($"Deleted {deleted} files from resources");
			return 0;
		}

		// EXTRACT MODE!

		if (pathArgs.Count == 0) // Add a default path if necessary
		{
			string destFolder = Path.GetDirectoryName(indexFilePath);
			destFolder = Path.Combine(destFolder, Path.GetFileNameWithoutExtension(indexFilePath));
			pathArgs.Add(destFolder);
		}

		if (!Directory.Exists(pathArgs[0]))
			Directory.CreateDirectory(pathArgs[0]);
		else if (Directory.EnumerateFileSystemEntries(pathArgs[0]).Any() && !force)
		{
			Console.WriteLine("Tried to " + (simulate ? "simulate" : "extract") + " to \"" + Path.GetFullPath(pathArgs[0]) + "\", but there are already files there!");
			return 1;
		}

		if (!simulate)
		{
			Console.WriteLine("Extracting contents to:");
			Console.WriteLine("    " + Path.GetFullPath(pathArgs[0]));
		}

		var fileIds = new List<string>();
		bool shouldFilter = (filterNames.Count != 0 || filterTypes.Count != 0);
		int numExtracted = 0;
		int numFiltered = 0;
		int numProcessed = 0;
		foreach(var entry in index.Entries)
		{
			numProcessed++;
			if(entry.Size == 0) // blank entry?
				continue;

			fileIds.Add(entry.GetFullName() + "=" + entry.ID);

			if (shouldFilter && FilterResource(in entry))
			{
				numFiltered++;
				continue;
			}

			if (verbose)
			{
				Console.WriteLine((simulate ? "Simulating " : "Extracting ") + entry.GetFullName() + "...");
				Console.WriteLine($"    id: {entry.ID}, type: {entry.FileType}, size: {entry.Size} ({entry.CompressedSize} bytes compressed)");
				Console.WriteLine($"    source: {Path.GetFileName(index.ResourceFilePath(entry.PatchFileNumber))}");
				Console.WriteLine($"--------------({numProcessed}/{index.Entries.Count})--------------");
			}
			else
				Console.WriteLine($"{numProcessed, 5}/{index.Entries.Count}: {entry.GetFullName()}");
			numExtracted++;

			if (simulate)
				continue; // Don't actually extract anything

			var destFilePath = Path.Combine(pathArgs[0], entry.GetFullName());
			if (entry.FileType != "file")
				destFilePath += ";" + entry.FileType;

			var destFileFolder = Path.GetDirectoryName(destFilePath);

			if (!Directory.Exists(destFileFolder))
				Directory.CreateDirectory(destFileFolder);

			using (FileStream fs = File.OpenWrite(destFilePath))
				index.CopyEntryDataToStream(entry, fs);

		}

		if (fileIds.Count > 0 && !simulate)
		{
			// "File.WriteAllLines" has \r\n newlines on Windows, but we only want \n, so we'll use a custom writer
			fileIds.Sort();
			string idFile = Path.Combine(pathArgs[0], "fileIds.txt");
			using var writer = new StreamWriter(idFile); // Replaces any existing file
			foreach (string id in fileIds)
				writer.Write(id + "\n");
		}

		Console.WriteLine();
		string text = (simulate ? "Simulation complete! Simulated " : "Extraction complete! Extracted ");
		if (numFiltered == 0)
			text += ($"{numExtracted} files (skipped {numProcessed - numExtracted} empty files)");
		else
			text += ($"{numExtracted} files (skipped {numProcessed - numExtracted - numFiltered} empty files and filtered {numFiltered} files)");
		if (!simulate)
			text += " to:\n    " + Path.GetFullPath(pathArgs[0]);
		Console.WriteLine(text);
		return 0;
	}

	// Used while extracting resources, returns "true" if the resource should be skipped
	static bool FilterResource(in DOOMResourceEntry entry)
	{
		if (filterTypes.Count != 0 && !filterTypes.Contains(entry.FileType.ToLowerInvariant()))
			return true;
		else if (filterNames.Count != 0)
		{
			foreach (string filter in filterNames)
			{
				if (entry.GetFullName().ToLowerInvariant().Contains(filter))
					return false;
			}
			return true;
		}

		return false;
	}
}
