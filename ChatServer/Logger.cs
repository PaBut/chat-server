using System.Net;
using ChatServer.Models;

namespace ChatServer;

public class Logger : ILogger
{
    private readonly IStdoutWriter stdoutWriter;

    public Logger(IStdoutWriter stdoutWriter)
    {
        this.stdoutWriter = stdoutWriter;
    }

    public void LogReceivedMessage(Message message, IPEndPoint senderEndPoint)
    {
        stdoutWriter.Write($"RECV {senderEndPoint.Address}:{senderEndPoint.Port} | {message.MessageType}");
    }

    public void LogSentMessage(Message message, IPEndPoint receiverEndPoint)
    {
        stdoutWriter.Write($"SENT {receiverEndPoint.Address}:{receiverEndPoint.Port} | {message.MessageType}");
    }
}