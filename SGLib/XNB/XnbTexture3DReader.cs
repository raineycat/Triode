using System.Runtime.InteropServices;
using BCnEncoder.Decoder;
using BCnEncoder.ImageSharp;
using BCnEncoder.Shared;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Squish;

namespace SGLib.XNB;

public static class XnbTexture3DReader
{
    public static XnbTexture3D LoadImage3D(BinaryReader r)
    {
        var surfaceFormat = (XnbSurfaceFormat)r.ReadInt32();
        var img = new XnbTexture3D
        {
            Width = r.ReadInt32(),
            Height = r.ReadInt32(),
            Depth = r.ReadInt32()
        };
        var dataLength = r.ReadInt32();

        if (surfaceFormat != XnbSurfaceFormat.ColorBGRA)
        {
            throw new XnbException("Texture3D decompression not implemented!");
        }

        var buffer = r.ReadBytes(dataLength);
        var sliceSize = img.Width * img.Height * 4;
        for (var z = 0; z < img.Depth; z++)
        {
            var sliceOffset = z * sliceSize;
            var slice = Image.LoadPixelData<Bgra32>(buffer.AsSpan(sliceOffset, sliceSize), img.Width, img.Height);
            img.Slices.Add(slice);
        }
        
        return img;
    }
    
    public static XnbTexture3D LoadTexture3D(this XnbFile xnb)
    {
        using var ms = new MemoryStream(xnb.Contents, false);
        using var r = new BinaryReader(ms);

        if (xnb.Version is XnbVersion.XnaFramework31 or XnbVersion.GameStudio4)
        {
            var poly = r.ReadByte();
            if (poly > 0)
            {
                return LoadImage3D(r);
            }
            else
            {
                throw new XnbException("Invalid poly byte");
            }
        }

        return LoadImage3D(r);
    }
}