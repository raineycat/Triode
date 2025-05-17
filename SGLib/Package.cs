using Serilog;
using SGLib.XNB;

namespace SGLib;

public class Package
{
    private static byte[] _buffer = new byte[0x800000];
    private static readonly byte[] _compressionBuffer = new byte[0x800000];
    private static readonly LZF _lzf = new();

    public Dictionary<string, XnbTexture2D> LoadedTextures = [];
    
    public static Package ReadFrom(Stream s)
    {
        var versionFlags = s.ReadInt32();
        var isCompressed = false;
        
        if ((versionFlags & 0x40000000) != 0)
        {
            versionFlags &= -0x40000001;
            isCompressed = true;
        }
        
        return (PackageVersion)versionFlags switch
        {
            PackageVersion.GSGE_TRANSISTOR => ReadPackageV5(s, isCompressed),
            _ => throw new InvalidPackageException("Unknown package version!")
        };
    }

    private static Package ReadPackageV5(Stream s, bool isCompressed)
    {
        var pkg = new Package();
        var readStatus = ReadingStatus.Successful;
        var assetList = new List<string>();
        var includePackages = new Queue<string>();

        do
        {
            int readBytes;
            if (isCompressed && s.ReadByte() != 0)
            {
                var compressedSize = s.ReadInt32();
                s.Read(_compressionBuffer, 0, compressedSize);
                readBytes = _lzf.Decompress(_compressionBuffer, compressedSize, _buffer, _buffer.Length);
                Log.Debug("Decompressed package: {CompressedSize} > {ActualSize}", compressedSize, readBytes);
            }
            else
            {
                readBytes = s.Read(_buffer);
            }

            var chunk = new MemoryStream(_buffer, 0, readBytes, false);
            do
            {
                readStatus = ReadAssetV5(chunk, pkg, assetList, includePackages);
            } while (readStatus != ReadingStatus.ReachedChunkEnd && readStatus != ReadingStatus.ReachedFileEnd);
            ProcessMultiTextureAtlasMaps();
        } while (readStatus == ReadingStatus.ReachedChunkEnd);

        Log.Debug("Finished loading package! Final asset list: {AssetList}; Final load queue: {Queue}", assetList, includePackages);
        return pkg;
    }

    private static ReadingStatus ReadAssetV5(Stream chunk, Package pkg, List<string> assetList, Queue<string> includePackages)
    {
        var chunkType = chunk.ReadByte();
        return chunkType switch
        {
            -1 => ReadingStatus.ReachedFileEnd,
            0xAD => ReadTextureV5(chunk, pkg, assetList),
            0xBB => ReadBinkV5(chunk, assetList),
            0xBE => ReadingStatus.ReachedChunkEnd,
            0xCC => ReadIncludePackageV5(chunk, includePackages),
            0xDE => ReadAtlasV5(chunk, assetList),
            0xEE => ReadBinkAtlasV5(chunk, assetList),
            0xFF => ReadingStatus.ReachedFileEnd,
            _ => ReadingStatus.Errored
        };
    }

    private static ReadingStatus ReadIncludePackageV5(Stream chunk, Queue<string> includePackages)
    {
        var packageName = chunk.ReadString();
        Log.Debug("Include package: {Name}", packageName);
        includePackages.Enqueue(packageName);
        return ReadingStatus.Successful;
    }

    private static ReadingStatus ReadTextureV5(Stream chunk, Package pkg, List<string> assetList)
    {
        var textureName = chunk.ReadString();
        var size = chunk.ReadInt32();

        if (size < 0)
        {
            Log.Error("Failed to load texture {Name}: Size ({Size}) is less than 0", textureName, size);
            return ReadingStatus.Errored;
        }

        try
        {
            var buffer = new byte[size];
            chunk.Read(buffer);
            using var ms = new MemoryStream(buffer);
            var xnb = XnbFile.LoadFrom(ms);
            assetList.Add(textureName);
            pkg.LoadedTextures.Add(textureName, xnb.LoadTexture2D());
            return ReadingStatus.Successful;
        }
        catch (Exception e)
        {
            Log.Error("Failed to read texture {Name}: {Error}", textureName, e);
            return ReadingStatus.Errored;
        }
    }
    
    private static ReadingStatus ReadBinkV5(Stream chunk, List<string> assetList)
    {
        var hasAlpha = chunk.ReadByte() == 1;
        var name = chunk.ReadString();
        Log.Debug("Load Bink video: GAME\\Content\\Movies\\{Name}.bik (alpha: {HasAlpha})", name, hasAlpha);
        return ReadingStatus.Successful;
    }
    
    private static ReadingStatus ReadAtlasV5(Stream chunk, List<string> assetList)
    {
        Log.Warning("ReadAtlasV5: Function is not implemented!");
        return ReadingStatus.Errored;
    }
    
    private static ReadingStatus ReadBinkAtlasV5(Stream chunk, List<string> assetList)
    {
        Log.Warning("ReadBinkAtlasV5: Function is not implemented!");
        return ReadingStatus.Errored;
    }

    private static void ProcessMultiTextureAtlasMaps()
    {
        Log.Warning("ProcessMultiTextureAtlasMaps: Not implemented!");
    }
}