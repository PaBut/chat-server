using ChatServer.Enums;
using ChatServer.Models;

namespace ChatServer.SocketClients;

public interface IIpkClient : IDisposable
{
    SocketType SocketType { get; init; }
    Task SendMessage(string messageContent, string senderUsername, CancellationToken cancellationToken = default);
    Task SendReply(bool isSuccess, string messageContent, Message originalMessage, CancellationToken cancellationToken = default);
    Task SendError(string errorContent, CancellationToken cancellationToken = default);
    Task Send(Message message, CancellationToken cancellationToken = default);
    Task Leave();
    Task<ResponseResult?> Listen(CancellationToken cancellationToken = default);
}