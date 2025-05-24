using System.Runtime.InteropServices;
using BCnEncoder.Decoder;
using BCnEncoder.ImageSharp;
using BCnEncoder.Shared;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Squish;

namespace SGLib.XNB;

public static class XnbTexture2DReader
{
    public static XnbTexture2D LoadImage(BinaryReader r, XnbVersion version)
    {
        var tex = new XnbTexture2D();
        var formatCode = r.ReadInt32();
        
        // why did this change??
        // surely there's no need for this to change????
        var format = version switch
        {
            XnbVersion.SuperGiantCustom => (XnbSurfaceFormat)formatCode,
            XnbVersion.GameStudio4 => (XnbSurfaceFormat)formatCode,
            XnbVersion.XnaFramework31 => formatCode switch
            {
                1 => XnbSurfaceFormat.ColorBGRA,
                28 => XnbSurfaceFormat.Dxt1,
                30 => XnbSurfaceFormat.Dxt3,
                32 => XnbSurfaceFormat.Dxt5,
                _ => throw new XnbException("Unsupported (legacy) surface format")
            },
            _ => throw new XnbException("Unknown XNB version")
        };
        
        var width = r.ReadUInt32();
        var height = r.ReadUInt32();
        var mipCount = r.ReadUInt32();

        for (var mip = 0; mip < mipCount; mip++)
        {
            var dataSize = r.ReadUInt32();
            var buffer = new byte[dataSize];
            r.BaseStream.Read(buffer);
            Image img;

            switch (format)
            {
                case XnbSurfaceFormat.ColorBGRA:
                    img = Image.LoadPixelData<Bgra32>(buffer, (int)width, (int)height);
                    break;

                case XnbSurfaceFormat.Dxt1:
                case XnbSurfaceFormat.Dxt3:
                case XnbSurfaceFormat.Dxt5:
                {
                    // guess the size based on an RGBA image
                    var outBuffer = new byte[4 * width * height];
                    var flags = format switch
                    {
                        XnbSurfaceFormat.Dxt1 => SquishFlags.kDxt1,
                        XnbSurfaceFormat.Dxt3 => SquishFlags.kDxt3,
                        XnbSurfaceFormat.Dxt5 => SquishFlags.kDxt5
                    };

                    Log.Debug("Using LibSquish: {Flags}", flags);
                    Squish.Squish.DecompressImage(outBuffer, (int)width, (int)height, buffer, flags);
                    img = Image.LoadPixelData<Rgba32>(outBuffer, (int)width, (int)height);
                    break;
                }
                
                case XnbSurfaceFormat.Rgba1010102:
                    img = Image.LoadPixelData<Rgba1010102>(buffer, (int)width, (int)height);
                    break;
                
                case XnbSurfaceFormat.Rg32:
                    img = Image.LoadPixelData<Rg32>(buffer, (int)width, (int)height);
                    break;
                
                case XnbSurfaceFormat.Rgba64:
                    img = Image.LoadPixelData<Rgba64>(buffer, (int)width, (int)height);
                    break;
                
                case XnbSurfaceFormat.Alpha8:
                    img = Image.LoadPixelData<A8>(buffer, (int)width, (int)height);
                    break;
                
                case XnbSurfaceFormat.Luminance:
                    img = Image.LoadPixelData<L8>(buffer, (int)width, (int)height);
                    break;

                case XnbSurfaceFormat.LuminanceAlpha:
                    img = Image.LoadPixelData<La16>(buffer, (int)width, (int)height);
                    break;

                case XnbSurfaceFormat.Bc7:
                {
                    var dec = new BcDecoder();
                    img = dec.DecodeRawToImageRgba32(buffer, (int)width, (int)height, CompressionFormat.Bc7);
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

        if (xnb.Version is XnbVersion.XnaFramework31 or XnbVersion.GameStudio4)
        {
            var poly = r.ReadByte();
            if (poly > 0)
            {
                return LoadImage(r, xnb.Version);
            }
            else
            {
                throw new XnbException("Invalid poly byte");
            }
        }

        return LoadImage(r, xnb.Version);
    }
}