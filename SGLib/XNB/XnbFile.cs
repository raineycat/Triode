using System.Runtime.InteropServices;
using System.Text;
using Serilog;
using SGLib.XNB.Compression;

namespace SGLib.XNB;

public class XnbFile
{
    public XnbPlatform Platform { get; set; }
    public XnbFlags Flags { get; set; }
    public XnbVersion Version { get; set; }
    public Dictionary<string, int> TypeReaders { get; set; } = [];
    public int SharedResourceCount { get; set; }
    public byte[] Contents { get; set; }
    
    public bool IsHiDefProfile
    {
        get => Flags.HasFlag(XnbFlags.HiDefProfile);
        set
        {
            if (value)
            {
                Flags |= XnbFlags.HiDefProfile;
            }
            else
            {
                Flags &= ~(XnbFlags.HiDefProfile);
            }
        }
    }

    public bool IsCompressed
    {
        get => Flags.HasFlag(XnbFlags.Compressed);
        set
        {
            if (value)
            {
                Flags |= XnbFlags.Compressed;
            }
            else
            {
                Flags &= ~(XnbFlags.Compressed);
            }
        }
    }
    
    public static XnbFile LoadFrom(Stream s)
    {
#if false
        var prevPos = s.Position;
        var dumpPath = Path.GetTempFileName();
        using (var dumpStream = File.OpenWrite(dumpPath))
        {
            s.CopyTo(dumpStream);   
        }
        s.Position = prevPos;
        Log.Debug("Dumped XNB to: {Path}", dumpPath);
#endif
        
        var xnb = new XnbFile();
        
        if (((char[])['X', 'N', 'B']).Any(c => s.ReadByte() != (byte)c))
        {
            Log.Error("Invalid XNB magic number");
            throw new XnbException("Invalid magic number");
        }

        xnb.Platform = (XnbPlatform)s.ReadByte();
        switch (xnb.Platform)
        {
            case XnbPlatform.WindowsDesktop:
            case XnbPlatform.WindowsPhone:
            case XnbPlatform.Xbox360:
                break;
            
            default:
                Log.Error("Invalid XNB platform: {Platform}!", xnb.Platform);
                throw new XnbException("Invalid platform");
        }

        xnb.Version = (XnbVersion)s.ReadByte();
        switch (xnb.Version)
        {
            case XnbVersion.XnaFramework31:
            case XnbVersion.GameStudio4:
                break;
            
            default:
                Log.Error("Invalid XNB version: {Version}!", xnb.Version);
                throw new XnbException("Invalid version");
        }

        xnb.Flags = (XnbFlags)s.ReadByte();

        var reader = new BinaryReader(s, Encoding.ASCII, true);
        var compressedSize = reader.ReadUInt32();
        var bufferBeginPos = s.Position;
        byte[] buffer;

        if (xnb.IsCompressed)
        {
            var decompressedSize = reader.ReadUInt32();
            using var ms = new MemoryStream((int)decompressedSize);

            using var lzx = new LzxDecoderStream(s, (int)decompressedSize, (int)compressedSize);
            lzx.CopyTo(ms);
            buffer = ms.GetBuffer();
            
#if false
            File.WriteAllBytes("DECOMPRESSED.bin", buffer);
#endif
        }
        else
        {
            buffer = new byte[compressedSize];
            var readBytes = s.Read(buffer);
        }

        var actualStream = new MemoryStream(buffer);
        reader.Dispose();
        reader = new BinaryReader(actualStream, Encoding.ASCII, true);
        
        var numTypeReaders = reader.Read7BitEncodedInt();
        //xnb.TypeReaders = new Dictionary<string, int>();
        for (var i = 0; i < numTypeReaders; i++)
        {
            var name = reader.ReadString();
            var version = reader.ReadInt32();
            xnb.TypeReaders.Add(name, version);
        }
        
        xnb.SharedResourceCount = reader.Read7BitEncodedInt();
        var contentsStartPos = (int)reader.BaseStream.Position;
        // Log.Debug("XNB content starts at: {Pos}", contentsStartPos);
        
        xnb.Contents = buffer[contentsStartPos..];
        return xnb;
    }
}