namespace HyperManager.Agent.HyperV;

public class HyperVException : Exception
{
    public HyperVException()
    {
    }

    public HyperVException(string message)
        : base(message)
    {
    }

    public HyperVException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
