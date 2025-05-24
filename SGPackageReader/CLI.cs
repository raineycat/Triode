using System.Text.Json;
using Serilog;
using SGLib;
using SixLabors.ImageSharp;

namespace SGPackageReader;

internal static class CLI
{
    private static readonly JsonSerializerOptions PrettyJson = new JsonSerializerOptions
        { IndentSize = 2, WriteIndented = true };
    
    /// <summary>
    /// Prints information about a manifest (*.pkg_manifest) file.
    /// </summary>
    /// <param name="manifestPath">The manifest file to read</param>
    /// <param name="printItems">Whether to print out items</param>
    /// <param name="printAtlasEntries">Whether to print out atlas entries</param>
    public static void PrintManifestInfo(string manifestPath, bool printItems = true, bool printAtlasEntries = false)
    {
        Log.Information("Loading: {FilePath}", manifestPath);
        using var s = File.OpenRead(manifestPath);
        var manifest = PackageManifest.ReadFrom(s, Path.ChangeExtension(manifestPath, null));
        Log.Information("Done!");

        if (printItems)
        {
            Log.Information("There are {Count} ManifestItems:", manifest.Items.Count);
            foreach (var item in manifest.Items)
            {
                Log.Information("- {Name} ({Start})", item.Name, item.Start);
            }
        }

        if (printAtlasEntries)
        {
            Log.Information("There are {Count} ManifestAtlasEntries:", manifest.AtlasEntries.Count);
            foreach (var atlas in manifest.AtlasEntries)
            {
                Log.Information("- {Name} (ID {ID})", atlas.Name, atlas.AtlasMap.ID);
            }
        }
    }

    /// <summary>
    /// Dumps all the asset files from a package (*.pkg) file
    /// </summary>
    /// <param name="packagePath">The file to read from</param>
    /// <param name="outputFolder">The folder to output to</param>
    /// <param name="atlasJson">The folder to output to</param>
    public static void DumpAssets(string packagePath, string outputFolder = "package-dump", bool atlasJson = false)
    {
        Log.Information("Loading: {FilePath}", packagePath);
        using var s = File.OpenRead(packagePath);
        var pkg = Package.ReadFrom(s);
        Log.Information("Loaded {Count} textures", pkg.LoadedTextures.Count);

        if (Directory.Exists(outputFolder))
        {
            Directory.Delete(outputFolder, true);
        }
        
        foreach(var tex in pkg.LoadedTextures)
        {
            var texName = tex.Key;
            if (Path.IsPathRooted(texName))
            {
                texName = texName.Substring(Path.GetPathRoot(texName)?.Length ?? 0);
            }
            
            var path = Path.Combine(outputFolder, texName + ".png");
            if (tex.Value.Mips.Count > 0)
            {
                var dir = Path.GetDirectoryName(path);
                if (dir != null && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                
                tex.Value.Mips[0].Save(path);
                Log.Debug("Saved: {Path}", path);
            }
        }
        
        foreach(var tex in pkg.LoadedTextures3D)
        {
            var texName = tex.Key;
            if (Path.IsPathRooted(texName))
            {
                texName = texName.Substring(Path.GetPathRoot(texName)?.Length ?? 0);
            }
            
            var dir = Path.Combine(outputFolder, texName);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            
            var count = 0;
            foreach (var slice in tex.Value.Slices)
            {
                slice.Save(Path.Combine(dir, $"{++count}.png"));
            }
            
            Log.Debug("Saved: {Path}/*.png ({Count} slices)", dir, tex.Value.Depth);
        }

        if (atlasJson)
        {
            foreach (var kv in pkg.LoadedAtlasMaps)
            {
                var atlasName = kv.Key;
                var amap = kv.Value;
                
                if (Path.IsPathRooted(atlasName))
                {
                    atlasName = atlasName.Substring(Path.GetPathRoot(atlasName)?.Length ?? 0);
                }
                
                var path = Path.Combine(outputFolder, atlasName + ".json");
                var dir = Path.GetDirectoryName(path);
                if (dir != null && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                
                File.WriteAllText(path, JsonSerializer.Serialize(amap, PrettyJson));
                Log.Debug("Dumped atlas to JSON: {Name}", atlasName);
            }
        }
        
        Log.Information("Done!");
    }
}