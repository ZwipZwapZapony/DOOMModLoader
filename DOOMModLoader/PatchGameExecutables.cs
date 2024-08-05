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
		if (!File.Exists("./DOOMx64.exe") || !File.Exists("./DOOMx64vk.exe"))
			return GameExeStatus.VFR;

		GameExeStatus statusGl     = GameExeStatus.Unknown;
		GameExeStatus statusVulkan = GameExeStatus.Unknown;

		try
		{
			// Check the OpenGL executable
			using (FileStream stream = File.OpenRead("./DOOMx64.exe"))
			{
				if (stream.Length < 0x169C0E0 + 7)
					return GameExeStatus.Unknown; // Too short for the byte sequence check

				if (Utility.StreamCheckBytes(stream, 0x169C0E0, [0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3, 0x90]))
					statusGl = GameExeStatus.Patched_New; // 2024 version, already patched
				else if (Utility.StreamCheckBytes(stream, 0x169C0E0, [0x40, 0x55, 0x53, 0x56, 0x57, 0x41, 0x56]))
					statusGl = GameExeStatus.Vanilla_New; // 2024 version, not yet patched
				else if (stream.Length == 76_022_480)
					statusGl = GameExeStatus.Vanilla_Old; // 2018 version, unpatchable
			}
			// Check the Vulkan executable
			using (FileStream stream = File.OpenRead("./DOOMx64vk.exe"))
			{
				if (stream.Length < 0x169B260 + 7)
					return GameExeStatus.Unknown;

				if (Utility.StreamCheckBytes(stream, 0x169B260, [0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3, 0x90]))
					statusVulkan = GameExeStatus.Patched_New;
				else if (Utility.StreamCheckBytes(stream, 0x169B260, [0x40, 0x55, 0x53, 0x56, 0x57, 0x41, 0x56]))
					statusVulkan = GameExeStatus.Vanilla_New;
				else if (stream.Length == 100_128_464)
					statusVulkan = GameExeStatus.Vanilla_Old;
			}
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
			{return GameExeStatus.Unknown;}

		if (statusGl == statusVulkan)
			return statusGl;
		else
			return GameExeStatus.Unknown; // Version/Patch status mismatch?
	}

	// Patches the 2024 game executables to not require developer mode to load mods,
	// and to allow earning achievements and saving settings while mods are installed
	// Returns true if the executables are patched successfully
	static bool PatchGameExecutables()
	{
		if (!File.Exists("./DOOMx64.exe") || !File.Exists("./DOOMx64vk.exe"))
			return false;

		try
		{
			// Back up the original executables
			Console.WriteLine("Backing up game executables to the \"base\" folder...");
			File.Copy("./DOOMx64.exe",   "./base/DOOMx64 (Pre-DOOMModLoader backup).exe",   true);
			File.Copy("./DOOMx64vk.exe", "./base/DOOMx64vk (Pre-DOOMModLoader backup).exe", true);

			Console.WriteLine("Patching game executables to load mods...");

			// Patch the OpenGL executable
			using (FileStream stream = File.Open("./DOOMx64.exe", FileMode.Open))
			{
				if (stream.Length < 0x169C0E0 + 7)
					return false; // Too short for the byte sequence check

				if (Utility.StreamCheckBytes(stream, 0x169C0E0, [0x40, 0x55, 0x53, 0x56, 0x57, 0x41, 0x56]))
				{
					stream.Position = 0x169C0E0;
					stream.Write([0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3, 0x90]); // mov eax, 1; ret; nop;
				}
				else
					return false;
			}
			// Patch the Vulkan executable
			using (FileStream stream = File.Open("./DOOMx64vk.exe", FileMode.Open))
			{
				if (stream.Length < 0x169B260 + 7)
					return false;

				if (Utility.StreamCheckBytes(stream, 0x169B260, [0x40, 0x55, 0x53, 0x56, 0x57, 0x41, 0x56]))
				{
					stream.Position = 0x169B260;
					stream.Write([0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3, 0x90]);
				}
				else
					return false;
			}
		}
		catch (Exception e) when (e is ArgumentException or IOException or UnauthorizedAccessException)
			{return false;}

		return true;
	}
}
