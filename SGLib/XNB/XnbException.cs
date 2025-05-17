namespace SGLib.XNB;

public class XnbException : Exception
{
    public XnbException()
    {
    }

    public XnbException(string message) : base(message)
    {
    }

    public XnbException(string message, Exception inner) : base(message, inner)
    {
    }
}