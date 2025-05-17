using System.Drawing;
using System.Numerics;
using Serilog;

namespace SGLib;

public class PackageManifest
{
    private static readonly byte[] _buffer = new byte[0x800000];
    private int _nextAtlasId = 1;

    public List<ManifestItem> Items = [];
    public List<ManifestAtlasEntry> AtlasEntries = [];

    public static PackageManifest ReadFrom(Stream s, string rootPackageName)
    {
        var versionFlags = s.ReadInt32();
        if ((versionFlags & 0x40000000) != 0)
            throw new InvalidPackageException("Compressed manifest files are not supported!");

        return (PackageVersion)versionFlags switch
        {
            PackageVersion.GSGE_TRANSISTOR => ReadManifestV5(s, rootPackageName),
            _ => throw new InvalidPackageException("Unknown package version!")
        };
    }

    private static PackageManifest ReadManifestV5(Stream s, string rootPackageName)
    {
        var manifest = new PackageManifest();
        var contentLength = (int)(s.Length - 4);
        var readStatus = ReadingStatus.Successful;

        var included = new Queue<string>();
        included.Enqueue(rootPackageName);

        do
        {
            var bytesRead = s.Read(_buffer, 0, contentLength);
            var chunk = new MemoryStream(_buffer, 0, bytesRead, false);

            do
            {
                readStatus = ReadItemV5(chunk, manifest, included, out var entry);
                if (entry.Name != null) manifest.Items.Add(entry);
            } while (readStatus != ReadingStatus.ReachedChunkEnd && readStatus != ReadingStatus.ReachedFileEnd);
        } while (readStatus == ReadingStatus.ReachedChunkEnd);

        Log.Debug("Finished loading manifest! Final includes: {Included}", included);
        return manifest;
    }

    private static ReadingStatus ReadItemV5(Stream chunk, PackageManifest manifest, Queue<string> included,
        out ManifestItem item)
    {
        item = new ManifestItem();
        var type = chunk.ReadByte();
        return type switch
        {
            -1 => ReadingStatus.ReachedFileEnd,
            0xBE => ReadingStatus.ReachedChunkEnd,
            0xCC => ReadIncludePackageV5(chunk, included),
            0xDE => ReadManifestAtlasV5(chunk, manifest, out item),
            0xEE => ReadManifestBinkAtlasV5(chunk, manifest, out item),
            0xFF => ReadingStatus.ReachedFileEnd,
            _ => ReadingStatus.Errored
        };
    }

    private static ReadingStatus ReadIncludePackageV5(Stream chunk, Queue<string> included)
    {
        var packageName = chunk.ReadString();
        Log.Debug("Including: {PackageName}", packageName);
        included.Enqueue(packageName);
        return ReadingStatus.Successful;
    }

    private static ReadingStatus ReadManifestAtlasV5(Stream chunk, PackageManifest manifest, out ManifestItem item)
    {
        item = new ManifestItem();
        var size = chunk.ReadInt32();

        if (size < 0)
        {
            Log.Error("Error reading atlas, size ({Size}) < 0", size);
            return ReadingStatus.Errored;
        }

        var startPosition = chunk.Position;
        var header = chunk.ReadInt32();
        var flagsVersion = 0;
        int entryCount;

        if (header == 0x7FB1776B)
        {
            flagsVersion = chunk.ReadInt32();
            entryCount = chunk.ReadInt32();
        }
        else
        {
            entryCount = header;
        }

        item.Start = manifest.AtlasEntries.Count;
        item.Count = entryCount;

        for (var i = 0; i < entryCount; i++)
        {
            var textureName = PathUtils.CleanPath(chunk.ReadString());
            textureName = string.Intern(textureName);

            var rect = new Rectangle(
                chunk.ReadInt32(), // x
                chunk.ReadInt32(), // y
                chunk.ReadInt32(), // width
                chunk.ReadInt32() // height
            );

            var offset = new Point(chunk.ReadInt32(), chunk.ReadInt32());
            var sizeInAtlas = new Point(chunk.ReadInt32(), chunk.ReadInt32());
            var scale = new Vector2(chunk.ReadFloat(), chunk.ReadFloat());

            var rotated = false;
            var trimmed = false;

            if (flagsVersion > 0)
            {
                var flags = chunk.ReadByte();
                if (flagsVersion > 1)
                {
                    rotated = (flags & 1) != 0;
                    trimmed = (flags & 2) != 0;
                }
                else
                {
                    rotated = flags != 0;
                }
            }

            manifest.AtlasEntries.Add(new ManifestAtlasEntry
            {
                Name = textureName,
                AtlasMap = new AtlasMap(
                    rect, offset, sizeInAtlas, scale,
                    manifest._nextAtlasId++,
                    rotated, trimmed, false,
                    GetSiblingTextureType(textureName)
                )
            });
        }

        chunk.ReadByte(); // Discard separator byte
        item.Name = chunk.ReadString(); // ReadTextureReference(chunk);

        return item.Name == null
            ? ReadingStatus.Errored
            : ReadingStatus.Successful;
    }

    private static ReadingStatus ReadManifestBinkAtlasV5(Stream chunk, PackageManifest manifest, out ManifestItem item)
    {
        item = new ManifestItem();

        var size = chunk.ReadInt32();
        if (size < 0)
        {
            Log.Error("Error reading bink atlas, size ({Size}) < 0 ", size);
            return ReadingStatus.Errored;
        }

        var version = chunk.ReadInt32();
        if (version != 1)
        {
            Log.Error("Error incorrect bink atlas version {ActualVersion}, excepting {ExpectedVersion} ", version, 1);
            return ReadingStatus.Errored;
        }

        var name = chunk.ReadString();
        var width = chunk.ReadInt32();
        var height = chunk.ReadInt32();

        item.Name = name;
        item.Start = manifest.AtlasEntries.Count;
        item.Count = 1;

        manifest.AtlasEntries.Add(new ManifestAtlasEntry
        {
            Name = name,
            AtlasMap = new AtlasMap(
                new Rectangle(0, 0, width, height), 
                new Point(0, 0), 
                new Point(width, height),
                Vector2.One, 
                -1, 
                false, 
                false, 
                true, 
                SiblingTextureType.None)
        });

        return ReadingStatus.Successful;
    }

    private static SiblingTextureType GetSiblingTextureType(string textureName)
    {
        if (textureName.EndsWith("_normal"))
        {
            return SiblingTextureType.NormalMap;
        }

        if (textureName.EndsWith("_mask"))
        {
            return SiblingTextureType.Mask;
        }

        if (textureName.EndsWith("_bink"))
        {
            return SiblingTextureType.Bink;
        }

        return SiblingTextureType.None;
    }
}