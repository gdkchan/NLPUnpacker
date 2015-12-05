using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;

using Ionic.Zlib;

namespace NLPUnpacker
{
    public class Program
    {
        const uint BlockAlign = 0x800;
        const uint BlockAlignMask = BlockAlign - 1;

        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[NLPUnpacker] New Love Plus for 3DS \"img.bin\" unpacker");
            Console.WriteLine("Made by gdkchan");
            Console.WriteLine("Version 0.1.4");
            Console.Write(Environment.NewLine);
            Console.ResetColor();

            if (args.Length > 0)
            {
                string FileName = args[0];
                if (File.Exists(FileName))
                {
                    UnpackImage(FileName);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Done!");
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Drag and drop the \"img.bin\" file on this executable!");
            }

            Console.ResetColor();
        }

        static void UnpackImage(string FileName)
        {
            using (FileStream Input = new FileStream(FileName, FileMode.Open))
            {
                BinaryReader Reader = new BinaryReader(Input);

                int Index = 0;
                string OutFolder = Path.Combine(Path.GetDirectoryName(FileName), "Image");
                Directory.CreateDirectory(OutFolder);
                while (Input.Position < Input.Length)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[Package " + Index + " at offset 0x" + Input.Position.ToString("X8") + "]");
                    Console.ResetColor();

                    string PackageFolder = Path.Combine(OutFolder, "Package_" + Index.ToString("D5"));
                    Directory.CreateDirectory(PackageFolder);
                    if (ExtractPackage(Reader, PackageFolder) > 0)
                        Index++;
                    else
                        Directory.Delete(PackageFolder, true);

                    if ((Input.Position & BlockAlignMask) != 0)
                    {
                        long Position = (Input.Position & ~BlockAlignMask) + BlockAlign;
                        if (Position >= Input.Length) break;
                        Input.Position = Position;
                    }

                    Console.Write(Environment.NewLine);
                }
            }
        }

        static int ExtractPackage(BinaryReader Reader, string OutputFolder)
        {
            uint BaseOffset = (uint)Reader.BaseStream.Position;

            string PackSignature = ReadString(Reader, 4);
            if (PackSignature != "PACK") return -1;

            uint FileCount = Reader.ReadUInt32() >> 16;
            uint StringPointersOffset = Reader.ReadUInt32() + BaseOffset;
            uint StringTableOffset = Reader.ReadUInt32() + BaseOffset;
            uint DataOffset = Reader.ReadUInt32() + BaseOffset;
            uint DecompressedSectionLength = Reader.ReadUInt32();
            uint CompressedSectionLength = Reader.ReadUInt32();
            uint Padding = Reader.ReadUInt32();

            for (int Index = 0; Index < FileCount; Index++)
            {
                Reader.BaseStream.Seek(BaseOffset + 0x20 + Index * 0x20, SeekOrigin.Begin);
                string FileSignature = ReadString(Reader, 4);
                uint Unknown0 = Reader.ReadUInt32();
                uint DecompressedLength = Reader.ReadUInt32();
                uint DecompressedOffset = Reader.ReadUInt32() + BaseOffset;
                uint Unknown1 = Reader.ReadUInt32();
                uint Flags = Reader.ReadUInt32();
                uint CompressedLength = Reader.ReadUInt32();
                uint CompressedOffset = Reader.ReadUInt32() + BaseOffset;
                bool IsCompressed = (Flags & 1) != 0;

                Reader.BaseStream.Seek(StringPointersOffset + Index * 4, SeekOrigin.Begin);
                string FileName = ReadNullTerminatedString(Reader, StringTableOffset + Reader.ReadUInt32());

                if (IsCompressed && CompressedLength > 0)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("Extracting compressed file \"" + FileName + "\"...");
                    Console.ResetColor();

                    Reader.BaseStream.Seek(CompressedOffset, SeekOrigin.Begin);
                    byte[] Buffer = new byte[CompressedLength];
                    Reader.Read(Buffer, 0, Buffer.Length);
                    File.WriteAllBytes(Path.Combine(OutputFolder, FileName), ZlibStream.UncompressBuffer(Buffer));
                }
                else
                {
                    Reader.BaseStream.Seek(DecompressedOffset, SeekOrigin.Begin);

                    if (ReadString(Reader, 4) == "SERI")
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("Extracting SERI file \"" + FileName + "\" as XML...");
                        Console.ResetColor();

                        ExtractSERI(Reader,
                                Path.Combine(OutputFolder, FileName),
                                StringTableOffset,
                                StringPointersOffset,
                                DecompressedOffset);
                    }
                    else
                    {
                        Console.WriteLine("Extracting file \"" + FileName + "\"...");

                        Reader.BaseStream.Seek(-4, SeekOrigin.Current);
                        byte[] Buffer = new byte[DecompressedLength];
                        Reader.Read(Buffer, 0, Buffer.Length);
                        File.WriteAllBytes(Path.Combine(OutputFolder, FileName), Buffer);
                    }
                }
            }

            Reader.BaseStream.Seek(BaseOffset + CompressedSectionLength, SeekOrigin.Begin);
            return (int)FileCount;
        }

        static void ExtractSERI(BinaryReader Reader,
            string OutputFile,
            uint StringTableOffset, 
            uint StringPointersOffset,
            uint SERIOffset)
        {
            SERI Output = new SERI();
            Reader.BaseStream.Seek(SERIOffset, SeekOrigin.Begin);

            string SERISignature = ReadString(Reader, 4);
            uint ValuesOffset = SERIOffset + Reader.ReadUInt32() + 4;
            ushort ParametersCount = Reader.ReadUInt16();
            uint TypesTableOffset = ValuesOffset - ParametersCount;

            for (int Index = 0; Index < ParametersCount; Index++)
            {
                Reader.BaseStream.Seek(SERIOffset + 0xa + Index * 4, SeekOrigin.Begin);
                ushort NameOffset = Reader.ReadUInt16();
                ushort ValueOffset = Reader.ReadUInt16();
                string Name = ReadNullTerminatedString(Reader, StringTableOffset + NameOffset);

                Reader.BaseStream.Seek(TypesTableOffset + Index, SeekOrigin.Begin);
                char ValueType = Reader.ReadChar();

                Output.Add(ParseSERI(Reader,
                    StringTableOffset,
                    StringPointersOffset,
                    ValuesOffset,
                    ValueOffset,
                    ValueType,
                    Name));
            }

            XmlSerializer Serializer = new XmlSerializer(typeof(SERI));
            using (FileStream OutFile = new FileStream(OutputFile + ".xml", FileMode.Create))
            {
                Serializer.Serialize(OutFile, Output);
            }
        }

        static SERIParameter ParseSERI(BinaryReader Reader,
            uint StringTableOffset,
            uint StringPointersOffset,
            uint ValuesOffset,
            uint ValueOffset,
            char ValueType,
            string Name)
        {
            switch (ValueType)
            {
                case 's': //String
                    string NameValue = ReadNullTerminatedString(Reader, StringTableOffset + ValueOffset);
                    return new String(Name, NameValue);
                case 'i': //Integer
                    Reader.BaseStream.Seek(ValuesOffset + ValueOffset, SeekOrigin.Begin);
                    int IntValue = Reader.ReadInt32();
                    switch (Name)
                    {
                        case "bone":
                        case "smes":
                        case "smat":
                        case "tex":
                        case "hair_length":
                            Reader.BaseStream.Seek(StringPointersOffset + (IntValue - 1) * 4, SeekOrigin.Begin);
                            return new String(Name, ReadNullTerminatedString(Reader, StringTableOffset + Reader.ReadUInt32()));
                        default: return new Integer(Name, IntValue);
                    }
                case 'b': //Boolean
                    Reader.BaseStream.Seek(ValuesOffset + ValueOffset, SeekOrigin.Begin);
                    bool BooleanValue = Reader.ReadByte() == 1;
                    return new Boolean(Name, BooleanValue);
                case 'f': //Float
                    Reader.BaseStream.Seek(ValuesOffset + ValueOffset, SeekOrigin.Begin);
                    float FloatValue = Reader.ReadSingle();
                    return new Float(Name, FloatValue);
                case 'a': //Array
                    Reader.BaseStream.Seek(ValuesOffset + ValueOffset, SeekOrigin.Begin);
                    return ParseSERIArray(Reader,
                        StringTableOffset,
                        StringPointersOffset,
                        ValuesOffset,
                        Name);
            }

            return null;
        }

        static SERIParameter ParseSERIArray(BinaryReader Reader, 
            uint StringTableOffset,
            uint StringPointersOffset, 
            uint ValuesOffset,
            string Name)
        {
            char ArrayDataType = Reader.ReadChar();
            byte Padding = Reader.ReadByte();
            ushort ArrayLength = Reader.ReadUInt16();
            ushort[] Pointers = new ushort[ArrayLength];
            for (int i = 0; i < Pointers.Length; i++) Pointers[i] = Reader.ReadUInt16();

            switch (ArrayDataType)
            {
                case 's':  //Of String
                    string[] StringArray = new string[ArrayLength];

                    for (int i = 0; i < Pointers.Length; i++)
                    {
                        string ItemName = ReadNullTerminatedString(Reader, StringTableOffset + Pointers[i]);
                        StringArray[i] = ItemName;
                    }

                    return new StringArray(Name, StringArray);
                case 'i': //Of Integer
                    switch (Name)
                    {
                        case "texi":
                        case "model":
                        case "cloth":
                        case "list":
                            string[] NameArray = new string[ArrayLength];

                            for (int i = 0; i < Pointers.Length; i++)
                            {
                                Reader.BaseStream.Seek(ValuesOffset + Pointers[i], SeekOrigin.Begin);
                                Reader.BaseStream.Seek(StringPointersOffset + (Reader.ReadUInt32() - 1) * 4, SeekOrigin.Begin);
                                string ItemName = ReadNullTerminatedString(Reader, StringTableOffset + Reader.ReadUInt32());
                                NameArray[i] = ItemName;
                            }

                            return new StringArray(Name, NameArray);
                        default:
                            int[] IntArray = new int[ArrayLength];

                            for (int i = 0; i < Pointers.Length; i++)
                            {
                                Reader.BaseStream.Seek(ValuesOffset + Pointers[i], SeekOrigin.Begin);
                                IntArray[i] = Reader.ReadInt32();
                            }

                            return new IntegerArray(Name, IntArray);
                    }
                case 'b': //Of Boolean
                    bool[] BooleanArray = new bool[ArrayLength];

                    for (int i = 0; i < Pointers.Length; i++)
                    {
                        Reader.BaseStream.Seek(ValuesOffset + Pointers[i], SeekOrigin.Begin);
                        BooleanArray[i] = Reader.ReadByte() == 1;
                    }

                    return new BooleanArray(Name, BooleanArray);
                case 'f': //Of Float
                    float[] FloatArray = new float[ArrayLength];

                    for (int i = 0; i < Pointers.Length; i++)
                    {
                        Reader.BaseStream.Seek(ValuesOffset + Pointers[i], SeekOrigin.Begin);
                        FloatArray[i] = Reader.ReadSingle();
                    }

                    return new FloatArray(Name, FloatArray);
                case 'h': //Of Array
                    SERIParameter[] NestedArray = new SERIParameter[ArrayLength];

                    for (int i = 0; i < Pointers.Length; i++)
                    {
                        Reader.BaseStream.Seek(ValuesOffset + Pointers[i], SeekOrigin.Begin);
                        ushort Count = Reader.ReadUInt16();
                        long TypesTableOffset = ValuesOffset + Pointers[i] + 2 + Count * 4;
                        SERIParameter[] Arrays = new SERIParameter[Count];
                        for (int Index = 0; Index < Count; Index++)
                        {
                            Reader.BaseStream.Seek(ValuesOffset + Pointers[i] + 2 + Index * 4, SeekOrigin.Begin);
                            ushort NameOffset = Reader.ReadUInt16();
                            ushort ValueOffset = Reader.ReadUInt16();
                            string ArrayName = ReadNullTerminatedString(Reader, StringTableOffset + NameOffset);

                            Reader.BaseStream.Seek(TypesTableOffset + Index, SeekOrigin.Begin);
                            char ValueType = Reader.ReadChar();

                            Arrays[Index] = ParseSERI(Reader,
                                StringTableOffset,
                                StringPointersOffset,
                                ValuesOffset,
                                ValueOffset,
                                ValueType,
                                ArrayName);
                        }

                        NestedArray[i] = new NestedArray(null, Arrays);
                    }

                    return new NestedArray(Name, NestedArray);
            }

            return null;
        }
        
        static string ReadString(BinaryReader Reader, int Length)
        {
            byte[] Buffer = new byte[Length];
            Reader.Read(Buffer, 0, Length);
            return Encoding.ASCII.GetString(Buffer);
        }

        static string ReadNullTerminatedString(BinaryReader Reader, uint Offset)
        {
            Reader.BaseStream.Seek(Offset, SeekOrigin.Begin);
            MemoryStream Buffer = new MemoryStream();
            for (;;)
            {
                byte Character = Reader.ReadByte();
                if (Character == 0) break;
                Buffer.WriteByte(Character);
            }
            string Output = Encoding.ASCII.GetString(Buffer.ToArray());
            Buffer.Dispose();
            return Output;
        }
    }
}
