using DOOMExtract;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace DOOMModLoader;
partial class Program
{
	enum SteamAppId // DOOM (2016) doesn't allow you to open the game executable directly,
	{ // so we need the app ID in order to launch the game through Steam instead
		DOOM_2016 = 379720,
		DOOM_VFR  = 650000,
	}



	static void PrintUsage()
	{
		string executable = "./DOOMModLoader";
		if (OperatingSystem.IsWindows())
			executable = "DOOMModLoader.exe";

		Console.WriteLine("Usage: " + executable + " [-[no]launchgame] [-moddir <pathToModsFolder>] [-[no]patchgame] [-[no]showconflicts] [-[no]snapmap]");
		Console.WriteLine();
		Console.WriteLine("    -help              - Display this text");
		Console.WriteLine("    -[no]launchgame    - [Don't] Launch the game after installing mods");
		Console.WriteLine("    -moddir <path>     - Use mods from a given path instead of \"Mods\"");
		Console.WriteLine("    -[no]patchgame     - [Don't] Patch the game to not require developer mode, if possible");
		Console.WriteLine("    -[no]showconflicts - [Don't] Show mod file conflicts");
		Console.WriteLine("    -[no]snapmap       - [Don't] Install mods for SnapMap instead of Campaign/Multiplayer");
		Console.WriteLine();
		Console.WriteLine("All options besides \"-moddir\" can also be set in \"DOOMModLoader Settings.txt\"");
	}

	static int Main(string[] args)
	{
		Console.WriteLine("DOOMModLoader originally by infogram, v0.3 by Zwip-Zwap Zapony and PowerBall253");
		Console.WriteLine("https://github.com/ZwipZwapZapony/DOOMModLoader");
		Console.WriteLine();

		LoadConfig();
		bool launchGame    = configLaunchGame;
		bool patchGame     = configPatchGame;
		bool showConflicts = configShowConflicts;
		bool snapMap       = configSnapMap;
		string modDir = "Mods";

		for (int i = 0; i < args.Length; i++) // Parse command-line arguments
		{
			string arg = args[i].Replace("--", "-").Replace('/', '-'); // Support "-", "--", and "/" prefixes
			switch (arg.ToLowerInvariant()) // Case-insensitive
			{
				case "-?":
				case "-h":
				case "-help":
					PrintUsage();
					return 0;
				case "-launchgame":
				case "-nolaunchgame":
					launchGame = (arg[1] != 'n');
					if (askToLaunchGame)
					{
						askToLaunchGame = false; // If "-(no)launchGame" was specified on the command-line, don't ask whether to launch it
						shouldSaveConfig = false; // Likewise, don't save a settings file that skips that question if the user has never been asked
					}
					break;
				case "-moddir":
					if ((i + 1) < args.Length)
						modDir = args[++i];
					break;
				case "-patchgame":
				case "-nopatchgame":
					patchGame = (arg[1] != 'n');
					break;
				case "-showconflicts":
				case "-noshowconflicts":
					showConflicts = (arg[1] != 'n');
					break;
				case "-snap":
				case "-snapmap":
				case "-nosnap":
				case "-nosnapmap":
					snapMap = (arg[1] != 'n');
					break;
				default:
					Console.WriteLine("Unknown option \"" + arg + "\"!");
					return 1;
			}
		}

		SteamAppId appId;
		if (File.Exists("./DOOMx64.exe"))
		{
			appId = SteamAppId.DOOM_2016;
			if (!File.Exists("./base/" + (snapMap ? "snap_" : "") + "gameresources.pindex"))
			{
				FatalError("Failed to find \"" + (snapMap ? "snap_" : "") + "gameresources.pindex\" in the base folder!");
				return 1;
			}
		}
		else if (File.Exists("./DOOMVFRx64.exe"))
		{
			appId = SteamAppId.DOOM_VFR;
			snapMap = false;
			if (!File.Exists("./base/gameresources.index")) // DOOM VFR doesn't have a patch container
			{
				FatalError("Failed to find \"gameresources.index\" in the base folder!");
				return 1;
			}
		}
		else
		{
			FatalError("Failed to find \"DOOMx64.exe\" in the current directory!");
			return 1;
		}

		if (!Directory.Exists(modDir))
		{
			if (Directory.Exists(modDir.ToLowerInvariant())) // The default changed from "mods" to "Mods", and Linux is case-sensitive, so check for an old "mods" folder
				modDir = modDir.ToLowerInvariant();
			else
				Directory.CreateDirectory(modDir);
		}

		UninstallMods(snapMap); // Delete previously-installed mods
		bool hasMods = InstallMods(modDir, snapMap, showConflicts); // Install new mods

		GameExeStatus exeStatus = CheckGameExecutables();
		if (exeStatus == GameExeStatus.Vanilla_New && patchGame)
		{
			if (PatchGameExecutables())
				exeStatus = GameExeStatus.Patched_New;
			else
			{
				Utility.WriteWarning("Failed to patch the game executables", true);
				Console.WriteLine("Developer mode may be necessary to launch the game with mods installed");
				Console.ResetColor();
				if (!askToLaunchGame)
					Console.WriteLine();
			}
		}

		if (askToLaunchGame)
		{
			Console.WriteLine();
			Console.WriteLine("Mods were successfully " + (hasMods ? "" : "un") + "installed");
			Console.WriteLine("Do you want to launch the game? You can change this later by editing \"DOOMModLoader Settings.txt\"");
			Console.WriteLine("Press [Y] to launch the game, or [N] to exit...");

			configLaunchGame = YesNoPrompt();
			launchGame = configLaunchGame;

			if (!launchGame) // Exit immediately if the user pressed N
			{
				SaveConfig();
				return 0;
			}
		}
		else
		{
			if (hasMods)
				Console.WriteLine("Mods were successfully installed!");
			else
				Console.WriteLine("No mods found. Mods were successfully uninstalled!");
		}

		SaveConfig(); // Yes, this belongs outside (but still after!) the "if (askToLaunchGame)" block

		if (launchGame)
		{
			string doomLauncherFile = "." + Path.DirectorySeparatorChar + "DOOMLauncher.exe"; // A forward slash doesn't work here

			Console.WriteLine("Launching game...");

			ProcessStartInfo startInfo = new();
			startInfo.UseShellExecute = true; // Don't wait for the process to end, and make it more likely for "steam://" URLs to work

			if ((exeStatus is GameExeStatus.Vanilla_Old or GameExeStatus.Vanilla_New)
			&& OperatingSystem.IsWindows() && File.Exists(doomLauncherFile))
			{
				// If the game executables weren't patched, use DOOMLauncher if it exists
				startInfo.FileName = doomLauncherFile;
				startInfo.WindowStyle = ProcessWindowStyle.Hidden;
				if (snapMap)
					startInfo.Arguments = "+com_gameType 1";
			}
			else
				startInfo.FileName = "steam://run/" + (int)appId + (snapMap ? "//+com_gameType 1" : "");

			try
			{
				Process.Start(startInfo);
				Console.WriteLine("Launched game!");
			}
			catch (Exception e) when (e is Win32Exception) // "Win32Exception" is cross-platform, used when the file doesn't exist
			{
				Console.WriteLine("Failed to launch the game, but mods were successfully " + (hasMods ? "" : "un") + "installed");
				PressKeyPrompt();
				return 0; // Mods were installed, so it's okay to exit with 0
			}
		}

		if (!skipPrompts)
		{
			Console.CancelKeyPress += ExitCtrlCHandler;
			Console.WriteLine();
			Console.WriteLine("Exiting in 10 seconds... (Press [Ctrl+C] to keep this window open...)");
			Thread.Sleep(10000);

			if (ctrlCHandlerUsed) // Apparently using Ctrl+C during a sleep will return to the post-sleep code when the sleep expires
				Thread.Sleep(Timeout.Infinite); // In that case, sleep again, forever, and let the Ctrl+C handler end the process instead
		}
		return 0;
	}
}
