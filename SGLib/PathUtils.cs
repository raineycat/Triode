using System.Text;

namespace SGLib;

internal static class PathUtils
{
    public static string CleanPath(string assetName)
    {
        if (string.IsNullOrEmpty(assetName))
            return assetName;

        // normalise separators
        var path = assetName.Replace(Path.AltDirectorySeparatorChar, '\\');

        // remove any consecutive backslashes
        var sb = new StringBuilder(path.Length);
        var justSeenSlash = false;

        foreach (var c in path)
        {
            if (c == '\\')
            {
                if (justSeenSlash) continue;
                sb.Append(c);
                justSeenSlash = true;
            }
            else
            {
                sb.Append(c);
                justSeenSlash = false;
            }
        }

        return sb.ToString();
    }
}