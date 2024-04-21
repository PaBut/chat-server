namespace ChatServer.Logging.Utilities;

public class StdoutWriter : IStdoutWriter
{
    public void Write(string text)
    {
        Console.WriteLine(text);
    }
}