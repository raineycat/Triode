namespace SGLib.XNB;

[Flags]
public enum XnbFlags
{
    None = 0x00,
    HiDefProfile = 0x01,
    CompressedWithLZ4 = 0x40,
    CompressedWithLZX = 0x80
}