using DOOMExtract;
using System;
using System.IO;

// This file handles checking and patching the game executables to not require developer mode
// Special thanks to PowerBall253 (https://github.com/brunoanc) for discovering the patch

namespace DOOMModLoader;
partial class Program
{
	enum GameExeStatus // Whether the game executables have been patched, and whether DOOMLauncher is useful
	{
		Patched_New, // Patched 2024 version, skips DOOMLauncher
		Vanilla_New, // Vanilla 2024 version, uses DOOMLauncher if you don't patch it
		Vanilla_Old, // Vanilla 2018 version, uses DOOMLauncher
		VFR,         // DOOM VFR, skips DOOMLauncher
		Unknown,     // Unrecognised version, skips DOOMLauncher
	}



	// Gets the current version and patch status of the game executables
	static GameExeStatus CheckGameExecutables()
	{
		return GameExeStatus.Unknown; // Todo: Finish this function
	}

	// Patches the 2024 game executables to not require developer mode to load mods,
	// and to allow earning achievements and saving settings while mods are installed
	// Returns true if the executables are patched successfully
	static bool PatchGameExecutables()
	{
		return false; // Todo: Finish this function
	}
}
