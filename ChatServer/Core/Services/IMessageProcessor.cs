using ChatServer.Models;

namespace ChatServer.Core.Services;

public interface IMessageProcessor
{
    Task ProcessMessage(ResponseResult request, User user, CancellationToken cancellationToken = default);
    bool IsEndState { get; }
}