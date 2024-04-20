using System.Net;
using ChatServer.Models;

namespace ChatServer;

public interface ILogger
{
    void LogReceivedMessage(Message message, IPEndPoint senderEndPoint);
    void LogSentMessage(Message message, IPEndPoint receiverEndPoint);
}   