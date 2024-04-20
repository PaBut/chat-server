using ChatServer.Models;

namespace ChatServer;

public interface IChannelManager
{
    void AddChannel(string channelId);
    void RemoveChannel(string channelId);
    Channel? GetChannel(string channelName);
    void AddUserToChannel(User user, string channelId);
    void RemoveUserFromChannel(User user, string channelId);
    Task SendToChannel(string channelId, string? senderUsername, Message message);
}