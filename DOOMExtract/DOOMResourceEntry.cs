using System;
using System.IO;
using System.IO.Compression;

namespace DOOMExtract;
public class DOOMResourceEntry
{
	private DOOMResourceIndex _index;

	public int ID;
	public string FileType; // Resource type
	public string FileName2; // Resource name - How other resources refer to this resource, possibly unused?
	public string FileName3; // Full file name - Blank for resources with 0 Size

	public long Offset;
	public int Size;
	public int CompressedSize;
	public long Zero; // Usually 0, but 16 for string files, "submission/orbis/save_data_icon(_profile).png", and all BSWF files and BSWF bimages
	public byte PatchFileNumber;

	public bool IsCompressed { get { return Size != CompressedSize; } }
	public DOOMResourceEntry(DOOMResourceIndex index)
	{
		_index = index;
	}

	public override string ToString()
	{
		return GetFullName();
	}

	public string GetFullName()
	{
		if (!String.IsNullOrEmpty(FileName3))
			return FileName3;

		if (!String.IsNullOrEmpty(FileName2))
			return FileName2;

		return FileType;

	}
	public void Read(EndianIO io)
	{
		io.BigEndian = true;
		ID = io.Reader.ReadInt32();

		io.BigEndian = false;
		// fname1
		int size = io.Reader.ReadInt32();
		FileType = io.Reader.ReadAsciiString(size);
		// fname2
		size = io.Reader.ReadInt32();
		FileName2 = io.Reader.ReadAsciiString(size);
		// fname3
		size = io.Reader.ReadInt32();
		FileName3 = io.Reader.ReadAsciiString(size);

		io.BigEndian = true;

		Offset = io.Reader.ReadInt64();
		Size = io.Reader.ReadInt32();
		CompressedSize = io.Reader.ReadInt32();
		if (_index.Header_Version <= 4)
			Zero = io.Reader.ReadInt64();
		else
			Zero = io.Reader.ReadInt32(); // Zero field is 4 bytes instead of 8 in version 5+

		PatchFileNumber = io.Reader.ReadByte();
	}

	public void Write(EndianIO io)
	{
		io.BigEndian = true;
		io.Writer.Write(ID);

		io.BigEndian = false;
		io.Writer.Write(FileType.Length);
		io.Writer.WriteAsciiString(FileType, FileType.Length);
		io.Writer.Write(FileName2.Length);
		io.Writer.WriteAsciiString(FileName2, FileName2.Length);
		io.Writer.Write(FileName3.Length);
		io.Writer.WriteAsciiString(FileName3, FileName3.Length);

		io.BigEndian = true;

		io.Writer.Write(Offset);
		io.Writer.Write(Size);
		io.Writer.Write(CompressedSize);
		if (_index.Header_Version <= 4)
			io.Writer.Write(Zero);
		else
			io.Writer.Write((int)Zero); // Zero field is 4 bytes instead of 8 in version 5+
		io.Writer.Write(PatchFileNumber);
	}

	public Stream GetDataStream(bool decompress)
	{
		if (Size == 0 && CompressedSize == 0)
			return null;

		var io = _index.GetResourceIO(PatchFileNumber);
		if (io == null)
			return null;

		io.Stream.Position = Offset;
		Stream dataStream = io.Stream;

		if (IsCompressed && decompress) // Copy the compressed data into a new DeflateStream
		{
			byte[] bytes = new byte[CompressedSize];
			dataStream.ReadExactly(bytes, 0, CompressedSize);
			MemoryStream memory = new MemoryStream(bytes);
			dataStream = new DeflateStream(memory, CompressionMode.Decompress);
		}

		return dataStream;
	}
}
