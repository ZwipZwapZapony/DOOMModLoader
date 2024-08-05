## DOOMModLoader

A mod loader for DOOM (2016).

Installation: Right-click DOOM (2016) in your Steam library, and choose "*Manage*" > "*Browse local files*" to open a File Explorer window in your DOOM (2016) installation folder. Download "*DOOMModLoader.zip*" from [**[the Releases page]**](../../../releases/latest), and extract it into that folder.

Usage: Place unextracted mod zips into a "*Mods*" folder in your DOOM (2016) installation, and run DOOMModLoader to install them. To uninstall mods, move them out of the "*Mods*" folder and run DOOMModLoader again.  
After running DOOMModLoader once, a "*DOOMModLoader Settings.txt*" file will be created, which you can edit with a raw text editor to change certain settings.

*:penguin: Note: On Linux, you should right-click DOOMModLoader and choose "Run in Konsole", or otherwise run it in a terminal.*

## Other Tools

[**[The Releases page]**](../../../releases/latest) also has a download called "*Tools.zip*". This contains all tools in this repository, including...

\- **DOOMExtract**: A command-line tool for extracting DOOM (2016) resources, necessary if you want to create your own mods.  
\- **DOOMModLoader**: See above.  
\- **idCrypt**: A command-line tool for decrypting certain binary files into plain text, and vice-versa. *Only available for Windows. On Linux, use [**[brunoanc/idCryptRust]**](https://github.com/brunoanc/idCryptRust/releases/latest) instead.*

## Building

To compile your own versions of these tools locally, you can use .NET 8.0 SDK to build DOOMExtract and DOOMModLoader with `dotnet publish "./DOOMExtract.csproj" --no-self-contained` and likewise for DOOMModLoader, and use MinGW-w64 to build idCrypt with `x86_64-w64-mingw32-gcc "./idCrypt.c" -o "./idCrypt.exe" -lbcrypt -Os`.

Alternatively, simply fork this repository and push a commit to any branch. This will trigger [**[an automated build]**](../../../actions).
