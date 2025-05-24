using SixLabors.ImageSharp;

namespace SGLib.XNB;

public class XnbTexture3D
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int Depth { get; set; }

    public List<Image> Slices = [];
}