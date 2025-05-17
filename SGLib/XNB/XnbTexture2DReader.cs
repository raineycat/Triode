using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Squish;

namespace SGLib.XNB;

public static class XnbTexture2DReader
{
    public static XnbTexture2D LoadImage(BinaryReader r)
    {
        var tex = new XnbTexture2D();
        var format = (XnbSurfaceFormat)r.ReadInt32();
        var width = r.ReadUInt32();
        var height = r.ReadUInt32();
        var mipCount = r.ReadUInt32();

        for (var mip = 0; mip < mipCount; mip++)
        {
            var dataSize = r.ReadUInt32();
            var buffer = new byte[dataSize];
            r.BaseStream.Read(buffer);
            byte[] outBuffer;

            bool swapColours = false;
            switch (format)
            {
                case XnbSurfaceFormat.Color:
                    outBuffer = buffer;
                    swapColours = true;
                    break;

                case XnbSurfaceFormat.Dxt1:
                case XnbSurfaceFormat.Dxt3:
                case XnbSurfaceFormat.Dxt5:
                {
                    // guess the size based on an RGBA image
                    outBuffer = new byte[4 * width * height];
                    var flags = format switch
                    {
                        XnbSurfaceFormat.Dxt1 => SquishFlags.kDxt1,
                        XnbSurfaceFormat.Dxt3 => SquishFlags.kDxt3,
                        XnbSurfaceFormat.Dxt5 => SquishFlags.kDxt5
                    };

                    Log.Debug("Using LibSquish: {Flags}", flags);
                    Squish.Squish.DecompressImage(outBuffer, (int)width, (int)height, buffer, flags);
                    break;
                }

                default:
                    throw new XnbException("Unsupported surface format! " + format);
            }

#if false
            var dumpPath = Path.GetTempFileName();
            File.WriteAllBytes(dumpPath, outBuffer);
            Log.Debug("Dumped texture blob to: {Path}", dumpPath);
#endif

            Image img;
            if (swapColours)
            {
                img = Image.LoadPixelData<Bgra32>(outBuffer, (int)width, (int)height);
            }
            else
            {
                img = Image.LoadPixelData<Rgba32>(outBuffer, (int)width, (int)height);
            }
            tex.Mips.Add(img);
        }

        tex.Width = width;
        tex.Height = height;
        return tex;
    }

    public static XnbTexture2D LoadTexture2D(this XnbFile xnb)
    {
        using var ms = new MemoryStream(xnb.Contents, false);
        using var r = new BinaryReader(ms);
        var poly = r.ReadByte();
        if (poly > 0)
        {
            return LoadImage(r);
        }
        else
        {
            throw new XnbException("Invalid poly byte");
        }
    }
}