using System;
using System.IO;
using System.Linq;

namespace DOOMExtract;
public static class Utility
{
	// Copies a specified amount of bytes from "sourceStream" to "destStream"
	public static long StreamCopy(Stream destStream, Stream sourceStream, int bufferSize, long length)
	{
		long read = 0;
		while (read < length)
		{
			int toRead = bufferSize;
			if (toRead > length - read)
				toRead = (int)(length - read);

			byte[] buf = new byte[toRead];
			int buf_read = sourceStream.Read(buf, 0, toRead);
			destStream.Write(buf, 0, buf_read);
			read += buf_read;
			if (buf_read == 0)
				break; // no more to be read..
		}
		return read;
	}

	// Checks for a given byte sequence at a set point in a stream
	public static bool StreamCheckBytes(Stream stream, long position, byte[] sequence)
	{
		byte[] bytes = new byte[sequence.Length];
		stream.Position = position;
		stream.ReadExactly(bytes);
		return bytes.SequenceEqual(sequence);
	}

	// Displays a warning with yellow text
	public static void WriteWarning(string text, bool noReset = false)
	{
		if (Console.BackgroundColor != ConsoleColor.Yellow) // This check isn't reliable on Linux, but what can you do?
			Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine("Warning: " + text);
		if (!noReset)
			Console.ResetColor();
	}
}
