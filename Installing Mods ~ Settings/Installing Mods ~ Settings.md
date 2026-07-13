After running DOOMModLoader once, a "*DOOMModLoaderSettings.txt*" file will be created, which you can edit with a raw text editor to configure settings. By default, the file will look similar to this:
```cpp
{
	checkForUpdates = 30; //Check for updates before installing mods (1-30: Days between checks / 0: Always / -1: Never)
	lastUpdateCheck = "2016-05-13"; //The date of the last update check
	launchGame = true; //Launch the game after installing mods
	patchGame = true; //Patch the game to not require developer mode, if possible
	showConflicts = true; //Show mod file conflicts
	showZipWarnings = false; //Show mod development warnings for zips, not just loose files
	snapMap = false; //Install mods for SnapMap instead of Campaign/Multiplayer
	uncapCutscenes = false; //Experimental: Let cutscenes run at more than 60 FPS
	verbose = false; //Display more information while installing mods
}
```
When editing this file, it's important to maintain the file's syntax. Each setting must have an equals sign between the setting's name and value, and a semicolon after the value, and all settings must be between a pair of curly brackets ( `{}` ).\
A value must be either `true`, `false`, a number, or a quoted string.\
`//` comments are supported, ignoring the rest of the line.

> [!NOTE]
> When DOOMModLoader saves its settings, any custom comments and unrecognised settings will be erased.

### `checkForUpdates`
*Default: Ask for `30` or `-1`*\
If `-1`, never checks for updates.\
If `0`, always checks for updates before installing mods.\
If `1` through `30`, this is the amount of days that must pass between successful update checks.

DOOMModLoader only checks for updates when you run it; it doesn't schedule update checks as a "service" on your system.\
No personal information will be collected. This only retrieves data from GitHub's REST API, via [**[this URL]**](https://api.github.com/repos/ZwipZwapZapony/DOOMModLoader/releases/latest).

### `lastUpdateCheck`
*Default: The date of the last update check*\
If `checkForUpdates` is `1` or higher, then `lastUpdateCheck` must be at least that many days ago for DOOMModLoader to check for updates. Otherwise, the update check will be skipped.\
If `checkForUpdates` is `-1` or `0`, then `lastUpdateCheck` will not be saved to the settings file.

### `launchGame`
*Default: Ask for `true` or `false`*\
If `true`, automatically launches DOOM (2016) or DOOM VFR after installing mods, and closes the mod loader log after a short delay if you don't pause it (on Windows).\
If `false`, doesn't launch the game.

### `patchGame`
*Default: `true`*\
If `true`, automatically patches DOOM (2016) to not require developer mode when mods are installed.\
If `false`, doesn't do that.

Without this patch, if mods are installed, then the game will crash if developer mode is disabled, or mark save slots as developer-mode-only, lock achievements/Multiplayer, and fail to save certain settings correctly (including game difficulty) if developer mode is enabled.

Before patching the game, "*-/steamapps/common/DOOM/DOOMx64(vk).exe*" will be backed up to "*-/steamapps/common/DOOM/base/DOOMx64(vk) (Pre-DOOMModLoader backup).exe*". If backups already exist, they'll be overwritten.

Setting this to `false` will not automatically unpatch the game executable; you must manually replace the game executables with the backups, or right-click DOOM (2016) in your Steam library and choose "*Properties...*" > "*Installed Files*" > "*Verify integrity of game files*".

*Special thanks to PowerBall253 ([**@brunoanc**](https://github.com/brunoanc)) for creating this patch!*

> [!NOTE]
> DOOMModLoader only patches the 2024 Steam and 2025 GOG versions of DOOM (2016) to not require developer mode. Older versions are unsupported.\
> DOOM VFR doesn't require developer mode, even without patching the game.

### `showConflicts`
*Default: `true`*\
If `true`, displays a list of conflicting files that are present in multiple mods.\
If `false`, doesn't do that.

If multiple mods contain the same file path/name, then only one version will be used, overriding all other versions. (Alphabetically-later mods will override alphabetically-earlier mods, and loose files will always override zips. E.g. "*My Super Mod.zip*" will override files in "*My Awesome Mod.zip*".)

When `showConflicts` is `true`, these files will be listed. If you're installing multiple mods and one of them doesn't work, this can make it easier to see whether that might be due to another mod.

Special files like [**[text strings]**](Creating-Mods-~-Text-Strings) or [**["*mod.decl*"]**](Creating-Mods-~-Mod.Decl) won't be listed, as they're processed individually for each mod.

### `showZipWarnings`
*Default: `false`*\
If `true`, displays all mod development warnings for all mods.\
If `false`, displays all warnings for loose mods, and less warnings for zipped mods.

This is mostly intended for mod creators.

### `snapMap`
*Default: `false`*\
If `true`, installs mods for SnapMap.\
If `false`, installs mods for Campaign/Multiplayer.

SnapMap uses a different resource container than Campaign/Multiplayer. This determines which container to install mods for, and changes the "*Installing mods...*" message to "*Installing SnapMods...*".

### `uncapCutscenes`
*Default: `false`*\
If `true`, changes cutscenes that are usually capped to 60 FPS to run at an uncapped frame rate.\
If `false`, doesn't do that.

> [!WARNING]
> This is experimental, and will likely cause problems.

### `verbose`
*Default: `false`*\
If `true`, displays more information while installing mods, such as whether a resource was added or replaced.\
If `false`, doesn't do that.

If your mod is supposed to replace a resource, but this says "*added*" instead, then you might have a typo in the file path. This is mostly intended for mod creators.

---

## Command-line arguments
All settings can also be overridden with command-line arguments, without having to edit "*DOOMModLoaderSettings.txt*" permanently. For more information, run `.\DOOMModLoader.exe -help` in a terminal/command prompt.
