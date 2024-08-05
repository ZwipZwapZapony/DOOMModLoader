using DOOMExtract;
using System;
using System.IO;
using System.Security;

// This file handles loading and saving the settings file

namespace DOOMModLoader;
partial class Program
{
	static bool configLaunchGame = true; // Whether to launch the game after installing mods
	static bool configPatchGame = true; // Whether to patch the game executables
	static bool configShowConflicts = true; // Display mod file conflicts after installing mods
	static bool configSnapMap = false; // Target "snap_gameresources" instead of "gameresources", and use "+com_gameType 1" as a launch parameter

	static bool askToLaunchGame = true; // If the settings file is missing or doesn't have "launchGame", ask whether to launch the game
	static bool shouldSaveConfig = true; // Only write the settings file if necessary



	// Loads the settings file, and sets variables accordingly
	static void LoadConfig()
	{
		string config;

		if (!File.Exists("./DOOMModLoader Settings.txt"))
			return;
		else try
			{config = File.ReadAllText("./DOOMModLoader Settings.txt");}
		catch (Exception e) when (e is IOException or SecurityException or UnauthorizedAccessException)
			{return;}

		askToLaunchGame = false; // Assume that "launchGame" is present, until proven otherwise
		shouldSaveConfig = false; // Don't rewrite the settings file, unless something is missing

		config = config.Replace('\r', '\n'); // Don't differentiate between \r and \n newlines

		// Naive single-line comment-stripper, in case someone has "snapMap=true;" in a comment
		// Doesn't understand quoted strings, backslashes before newlines,
		// nor that it should remove "// /* // */" but not "/* // */"
		for (int start = config.IndexOf("//"); start != -1; start = config.IndexOf("//"))
		{
			int end = config.IndexOf('\n', start+2);
			if (end == -1)
				config = config.Remove(start);
			else
				config = config.Remove(start, end - start);
		}
		// Naive multi-line comment-stripper next
		for (int start = config.IndexOf("/*"); start != -1; start = config.IndexOf("/*"))
		{
			int end = config.IndexOf("*/", start+2); // The "+2" is to not stop right after "/*/"
			if (end == -1)
				config = config.Remove(start);
			else
				config = config.Remove(start, end - start);
		}
		// Naive whitespace-stripper
		config = config.Replace(" ", "").Replace("\t", "").Replace("\n", "");

		// Naive value-checker
		if (!LoadConfigBool(config, "launchGame", ref configLaunchGame))
		{
			askToLaunchGame = true;
			shouldSaveConfig = true;
		}
		if (!LoadConfigBool(config, "patchGame", ref configPatchGame))
			shouldSaveConfig = true;
		if (!LoadConfigBool(config, "showConflicts", ref configShowConflicts))
			shouldSaveConfig = true;
		if (LoadConfigBool(config, "snapMap", ref configSnapMap) != File.Exists("./base/snap_gameresources.resources"))
			shouldSaveConfig = true; // "snapMap" should be present for DOOM (2016), and NOT present for DOOM VFR
	}

	// If "name" is set in "config", then sets "variable" accordingly and returns true
	// If "name" is not set in "config", then returns false without modifying "variable"
	static bool LoadConfigBool(string config, string name, ref bool variable)
	{
		if (config.Contains(name + "=true;"))
		{
			variable = true;
			return true;
		}
		else if (config.Contains(name + "=false;"))
		{
			variable = false;
			return true;
		}
		else
			return false;
	}

	// Writes the settings file
	static void SaveConfig()
	{
		if (!shouldSaveConfig)
			return;

		string config = "{";
		config += "\n\tlaunchGame = "    + (configLaunchGame ? "true" : "false")    + "; //Launch the game after installing mods";
		config += "\n\tpatchGame = "     + (configPatchGame ? "true" : "false")     + "; //Patch the game to not require developer mode, if possible";
		config += "\n\tshowConflicts = " + (configShowConflicts ? "true" : "false") + "; //Show mod file conflicts";
		if (File.Exists("./base/snap_gameresources.resources")) // Don't write the SnapMap line for DOOM VFR
			config += "\n\tsnapMap = "       + (configSnapMap ? "true" : "false")       + "; //Install mods for SnapMap instead of Campaign/Multiplayer";
		config += "\n}";

		try
			{File.WriteAllText("./DOOMModLoader Settings.txt", config);}
		catch (Exception e) when (e is IOException or SecurityException or UnauthorizedAccessException)
			{Utility.WriteWarning("Failed to write \"DOOMModLoader Settings.txt\"");}
	}
}
