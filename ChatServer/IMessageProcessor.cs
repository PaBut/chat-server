using System.Collections.Concurrent;
using ChatServer.Models;

namespace ChatServer;

public interface IMessageProcessor
{
    Task ProcessMessage(ResponseResult request, User user, CancellationToken cancellationToken = default);
    bool IsEndState { get; }
}