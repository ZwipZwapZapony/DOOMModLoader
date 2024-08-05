using System;
using System.Diagnostics;

// This file handles all "press a key" prompts

namespace DOOMModLoader;
partial class Program
{
	// Don't wait for keypresses if DOOMModLoader was opened within an existing terminal (batch file, command prompt, et cetera)
	static bool skipPrompts = (Process.GetCurrentProcess().MainWindowHandle == IntPtr.Zero);
	static bool ctrlCHandlerUsed = false; // Set to true when a custom Ctrl+C handler is executed



	// Waits for any keypress (with a "to exit" message), unless "skipPrompts" is true
	static void PressKeyPrompt()
	{
		if (skipPrompts)
			return;
		Console.WriteLine("Press any key to exit...");
		WaitForKeypress();
	}

	// Waits for any keypress, used when pressing Ctrl+C during the exit countdown
	static void ExitCtrlCHandler(object sender, ConsoleCancelEventArgs e)
	{
		Console.CancelKeyPress -= ExitCtrlCHandler; // Don't allow "stackable" Ctrl+C events
		ctrlCHandlerUsed = true;
		Console.WriteLine("Ctrl+C was pressed. Press any key to exit...");
		WaitForKeypress();
		Environment.Exit(0);
	}

	// Returns true if Y was pressed, or false if N was pressed, regardless of "skipPrompts"
	static bool YesNoPrompt()
	{
		while (true)
		{
			ConsoleKey key;
			try
				{key = Console.ReadKey(true).Key;}
			catch (InvalidOperationException)
			{
				FatalError("Failed to detect keystroke", 0); // Mods were installed at this point, so it's okay to exit with 0
				return false;
			}

			if (key == ConsoleKey.Y)
				return true;
			else if (key == ConsoleKey.N)
				return false;
			else if ((key >= ConsoleKey.A && key <= ConsoleKey.Z) // Beep at unrecognised letters and numbers only
			|| (key >= ConsoleKey.D0 && key <= ConsoleKey.D9))
				Console.Beep(); // Todo: Beep asynchronously?
		}
	}

	// Waits for a "reasonable" keypress - Console.ReadKey is too generous, triggering even for the Windows key
	static void WaitForKeypress()
	{
		while (true)
		{
			ConsoleKey key;
			try
				{key = Console.ReadKey(true).Key;}
			catch (InvalidOperationException)
				{return;} // This is the best that we can do here

			if ((key >= ConsoleKey.A && key <= ConsoleKey.Z)
			|| (key >= ConsoleKey.D0 && key <= ConsoleKey.D9)
			|| (key >= ConsoleKey.F1 && key <= ConsoleKey.F24)
			|| (key >= ConsoleKey.NumPad0 && key <= ConsoleKey.NumPad9)
			|| (key >= ConsoleKey.Backspace && key <= ConsoleKey.Help)
			|| (key >= ConsoleKey.Multiply && key <= ConsoleKey.Divide)
			|| (key >= ConsoleKey.Oem1 && key <= ConsoleKey.Oem102)) // This doesn't catch OemClear, but I don't even know what that is
				return;
		}
	}

	// Displays "Error: [message]", waits for keyboard input, and terminates DOOMModLoader
	static void FatalError(string message, int exitCode = 1)
	{
		Console.BackgroundColor = ConsoleColor.DarkRed;
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine("Error: " + message);
		Console.ResetColor();

		PressKeyPrompt();
		Environment.Exit(exitCode);
	}
}
