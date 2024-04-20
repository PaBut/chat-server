using ChatServer.Models;

namespace ChatServer;

public interface ISocketClient
{
    Task SendMessage(Message message, CancellationToken cancellationToken = default);
    Task<Message> ReceiveMessage(CancellationToken cancellationToken = default);
}