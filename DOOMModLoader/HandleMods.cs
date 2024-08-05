using DOOMExtract;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

// This file handles installing and uninstalling mods

namespace DOOMModLoader;
partial class Program
{
	static List<string> modConflicts = new(); // Files that conflict across multiple mods



	// Uninstalls currently-installed mods
	static void UninstallMods(bool snapMap)
	{
		// Delete "./base/(snap_)gameresources_###.[patch/pindex]" mod containers
		foreach (string file in Directory.GetFiles("./base/", (snapMap ? "snap_" : "") + "gameresources_*"))
		{
			if (Path.GetExtension(file).ToLowerInvariant() is ".patch" or ".pindex")
				File.Delete(file);
		}

		// Delete video mod files
		if (!snapMap && Directory.Exists("./base/video/mods/"))
			Directory.Delete("./base/video/mods/", true);
		else if (snapMap && Directory.Exists("./base/video/snap_mods/"))
			Directory.Delete("./base/video/snap_mods/", true);
	}

	// Installs mods. Returns true if mods exist, false otherwise
	static bool InstallMods(string modDir, bool snapMap, bool showConflicts)
	{
		if (!Directory.EnumerateFileSystemEntries(modDir).Any())
			return false; // No mods found

		Console.WriteLine(snapMap ? "Installing SnapMods..." : "Installing mods...");

		// Todo: Load mods directly instead of copying files to a temporary folder first
		string extractedPath = Path.GetFullPath(Directory.CreateTempSubdirectory("DOOMModLoader_").FullName + Path.DirectorySeparatorChar);

		try
		{
			// Display where mods are being copied to, but hide the user's username
			string userName = (Path.DirectorySeparatorChar + Environment.UserName + Path.DirectorySeparatorChar);
			string maskedName = (Path.DirectorySeparatorChar + "[username]" + Path.DirectorySeparatorChar);
			string maskedPath = extractedPath.Replace(userName, maskedName);
			Console.WriteLine("Extracting/Copying mods into \"" + maskedPath + "\"...");

			ExtractMods(modDir, extractedPath);

			if (modConflicts.Count != 0 && showConflicts)
			{
				// Todo: List all zips (and loose folder) that a given file came from
				modConflicts.Sort();
				Console.WriteLine();

				Utility.WriteWarning("The following files were found in multiple mods:", true);
				foreach (string conflict in modConflicts)
					Console.WriteLine("    " + conflict.Replace('\\', '/'));

				Console.ResetColor();
				Console.WriteLine();
			}

			// Todo: Once resource replacements are case-insensitive, remove this mod-specific Linux hack
			if (!OperatingSystem.IsWindows() && File.Exists(Path.Combine(extractedPath, "generated/decls/missionSelectInfoList/arcadelist.decl;missionSelectInfoList")))
				File.Move(
					Path.Combine(extractedPath, "generated/decls/missionSelectInfoList/arcadelist.decl;missionSelectInfoList"),
					Path.Combine(extractedPath, "generated/decls/missionselectinfolist/arcadelist.decl;missionSelectInfoList"),
				true);

			HandleVideos(extractedPath, snapMap);
			HandleResources(extractedPath, snapMap); // Resources must be handled last
		}
		finally
			{Directory.Delete(extractedPath, true);} // Clean-up

		return true;
	}

	// Extracts/Copies mods from "modDir" to "extractedPath"
	static void ExtractMods(string modDir, string extractedPath)
	{
		// copy loose mods from modDir into extract path
		List<string> zips = new();
		foreach (string file in Directory.GetFiles(modDir))
		{
			if (Path.GetExtension(file).ToLowerInvariant() == ".zip")
				zips.Add(file); // don't copy zips, we'll extract them later instead
			else
				File.Copy(file, Path.Combine(extractedPath, Path.GetFileName(file)));
		}
		foreach (string dir in Directory.GetDirectories(modDir))
			CloneDirectory(dir, Path.Combine(extractedPath, Path.GetFileName(dir)));

		// extract mod zips
		string modInfoPath = Path.Combine(extractedPath, "modinfo.txt");
		string fileIdsPath = Path.Combine(extractedPath, "fileIds.txt");
		string fileIds = "";
		if (File.Exists(fileIdsPath))
		{
			fileIds = File.ReadAllText(fileIdsPath) + "\n";
			File.Delete(fileIdsPath);
		}

		try
		{
			foreach (string zipfile in zips)
			{
				string modInfo = "";
				Console.Write("Extracting \"" + Path.GetFileName(zipfile) + "\"...");
				ExtractZipFile(zipfile, extractedPath);
				if (File.Exists(modInfoPath))
				{
					modInfo = File.ReadAllText(modInfoPath);
					if (!String.IsNullOrEmpty(modInfo))
						modInfo = " " + modInfo;

					File.Delete(modInfoPath); // delete so no conflicts
				}
				if (File.Exists(fileIdsPath))
				{
					// todo: make this use a dictionary instead, so we can detect conflicts
					fileIds += File.ReadAllText(fileIdsPath) + "\n";
					File.Delete(fileIdsPath);
				}

				Console.WriteLine(" Extracted" + modInfo + "!");
			}
		}
		catch
		{
			Directory.Delete(extractedPath, true); // Clean-up
			FatalError("Failed to extract zips!");
			return;
		}
		if (!String.IsNullOrEmpty(fileIds))
			File.WriteAllText(fileIdsPath, fileIds);
	}

	// Moves video files (and edits materials); videos only work as loose files, not as resources
	static void HandleVideos(string extractedPath, bool snapMap)
	{
		string videoFolder = Path.GetFullPath(Path.Combine(extractedPath, "video/"));
		if (!Directory.Exists(videoFolder))
			return; // No videos found

		// Check for videos outside of "video/mods/"
		bool badPath = false;
		if (Directory.EnumerateFiles(videoFolder).Any())
			badPath = true;
		foreach (string dir in Directory.EnumerateDirectories(videoFolder))
		{
			if (Path.GetFileName(dir).ToLowerInvariant() != "mods")
			{
				badPath = true;
				break;
			}
		}
		if (badPath)
		{
			Utility.WriteWarning("Custom videos are only allowed within \"video/mods/\"!");
			Console.WriteLine();
		}

		// Move (don't copy!) videos to -/DOOM/base/video/(snap_)mods/
		int chopIndex = Path.Combine(videoFolder, "mods/").Length;
		string outFolder = ("./base/video/" + (snapMap ? "snap_" : "") + "mods/");
		foreach (string file in Directory.EnumerateFiles(Path.Combine(videoFolder, "mods/"), "*", SearchOption.AllDirectories))
		{
			if (Path.GetExtension(file).ToLowerInvariant() is ".bik" or ".bk2")
			{
				string filePath = Path.Combine(outFolder, file.Substring(chopIndex));
				Directory.CreateDirectory(Path.GetDirectoryName(filePath));
				File.Move(file, filePath, true);
			}
		}

		// For SnapMap mods, edit materials to point to the substituted video folder
		if (snapMap)
		{
			string materialFolder = Path.Combine(extractedPath, "generated/decls/material/");

			if (Directory.Exists(materialFolder))
			{
				foreach (string file in Directory.EnumerateFiles(materialFolder, "*", SearchOption.AllDirectories))
				{
					string material = File.ReadAllText(file);
					material = material.Replace("\"video/mods/", "\"video/snap_mods/");
					File.WriteAllText(file, material);
				}
			}
		}
	}

	// Installs custom resources into a new "(snap_)gameresources_002.patch" container
	static void HandleResources(string extractedPath, bool snapMap)
	{
		int customPfi = 2; // This should match "destPath"'s numbers, which must be at least 002

		string basePath = ("./base/" + (snapMap ? "snap_" : "") + "gameresources.pindex");
		if (File.Exists("./DOOMVFRx64.exe") && !File.Exists("./DOOMx64.exe"))
			basePath = "./base/gameresources.index"; // DOOM VFR doesn't have a patch container

		string destPath = ("./base/" + (snapMap ? "snap_" : "") + "gameresources_002.pindex");

		File.Copy(basePath, destPath);

		var index = new DOOMResourceIndex(destPath);
		if (!index.Load())
		{
			Directory.Delete(extractedPath, true); // Clean-up
			File.Delete(destPath); // Clean-up
			FatalError("Failed to load custom container \"" + Path.GetFileName(destPath) + "\"!");
			return;
		}
		index.PatchFileNumber = (byte)customPfi;

		index.Rebuild(Path.ChangeExtension(destPath, ".patch"), extractedPath, true);
		index.Close();
	}

	// Copies all files and subfolders from "src" to "dest"
	static void CloneDirectory(string src, string dest)
	{
		if (!Directory.Exists(dest))
			Directory.CreateDirectory(dest);

		foreach (string directory in Directory.GetDirectories(src))
		{
			string dirName = Path.GetFileName(directory);
			if (!Directory.Exists(Path.Combine(dest, dirName)))
				Directory.CreateDirectory(Path.Combine(dest, dirName));

			CloneDirectory(directory, Path.Combine(dest, dirName));
		}

		foreach (string file in Directory.GetFiles(src))
			File.Copy(file, Path.Combine(dest, Path.GetFileName(file)));
	}

	// Extracts a zip, while checking for file conflicts
	static void ExtractZipFile(string archiveFile, string outFolder)
	{
		outFolder = Path.GetFullPath(outFolder + Path.DirectorySeparatorChar);

		using ZipArchive archive = ZipFile.OpenRead(archiveFile);
		foreach (ZipArchiveEntry entry in archive.Entries)
		{
			string outFile = Path.GetFullPath(Path.Combine(outFolder, entry.FullName));

			if (!outFile.StartsWith(outFolder, StringComparison.Ordinal))
			{
				Directory.Delete(outFolder, true); // Clean-up
				FatalError("\"" + Path.GetFileName(archiveFile) + "\" tried to back out of its extraction path!");
				return;
			}

			if (File.Exists(outFile) && !modConflicts.Contains(entry.FullName))
				modConflicts.Add(entry.FullName);
		}
		archive.ExtractToDirectory(outFolder, true);
	}
}
