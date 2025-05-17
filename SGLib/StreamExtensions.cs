using System.Text;

namespace SGLib;

internal static class StreamExtensions
{
    private static byte[] ReverseBytes(byte[] inArray)
    {
        var end = inArray.Length - 1;
        for (var begin = 0; begin < inArray.Length / 2; begin++)
        {
            (inArray[begin], inArray[end]) = (inArray[end], inArray[begin]);
            end--;
        }
        return inArray;
    }

    public static void SkipString(this Stream stream)
    {
        var length = stream.ReadByte();
        stream.Position += length;
    }

    public static string ReadString(this Stream stream)
    {
        var length = stream.ReadByte();
        var buf = new byte[length];
        stream.Read(buf);
        return Encoding.ASCII.GetString(buf);
    }

    public static void WriteString(this Stream stream, string str)
    {
        var buf = Encoding.ASCII.GetBytes(str);
        stream.WriteByte((byte)buf.Length);
        stream.Write(buf);
    }
    
    public static int ReadInt32(this Stream stream)
    {
        var buffer = new byte[sizeof(int)];
        stream.Read(buffer);
        if (BitConverter.IsLittleEndian)
        {
            buffer = ReverseBytes(buffer);
        }
        return BitConverter.ToInt32(buffer);
    }

    public static void WriteInt32(this Stream stream, int i)
    {
        var buffer = BitConverter.GetBytes(i);
        if (BitConverter.IsLittleEndian)
        {
            buffer = ReverseBytes(buffer);
        }
        stream.Write(buffer);
    }

    public static float ReadFloat(this Stream stream)
    {
        var buffer = new byte[sizeof(float)];
        stream.Read(buffer);
        if (BitConverter.IsLittleEndian)
        {
            buffer = ReverseBytes(buffer);
        }
        return BitConverter.ToSingle(buffer);
    }
    
    public static void WriteFloat(this Stream stream, float f)
    {
        var buffer = BitConverter.GetBytes(f);
        if (BitConverter.IsLittleEndian)
        {
            buffer = ReverseBytes(buffer);
        }
        stream.Write(buffer);
    }
}