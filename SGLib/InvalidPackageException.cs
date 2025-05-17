namespace SGLib;

public class InvalidPackageException : Exception
{
    public InvalidPackageException()
    {
    }

    public InvalidPackageException(string message) : base(message)
    {
    }

    public InvalidPackageException(string message, Exception inner) : base(message, inner)
    {
    }
}