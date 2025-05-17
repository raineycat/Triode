using SixLabors.ImageSharp;

namespace SGLib.XNB;

public class XnbTexture2D
{
    public uint Width { get; set; }
    public uint Height { get; set; }
    public List<Image> Mips { get; set; } = [];
}