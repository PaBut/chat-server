using ChatServer.Models;

namespace ChatServer.Core.Services;

class ChannelManager : IChannelManager
{
    private readonly IList<Channel> channels;
    
    public ChannelManager()
    {
        channels = new List<Channel>();
    }

    private bool ChannelExists(string channelId) => channels.Any(c => c.Name == channelId);
    
    public void AddChannel(string channelId)
    {
        channels.Add(new(channelId));
    }

    public void RemoveChannel(string channelId)
    {
        if (ChannelExists(channelId))
        {
            channels.Remove(channels.First(c => c.Name == channelId));
        }
    }

    public Channel? GetChannel(string channelName)
    {
        return channels.FirstOrDefault(c => c.Name == channelName);
    }

    public void AddUserToChannel(User user, string channelId)
    {
        
        if (!ChannelExists(channelId))
        {
            AddChannel(channelId);
        }
        
        GetChannel(channelId)!.AddUser(user);
    }

    public void RemoveUserFromChannel(User user, string channelId)
    {
        var channel = GetChannel(channelId);
        channel?.RemoveUser(user);
    }

    public IEnumerable<User> GetAllUsers()
    {
        return channels.SelectMany(c => c.Users);
    }

    public async Task SendToChannel(string channelId, string? senderUsername, Message message)
    {
        var channel = GetChannel(channelId);

        if (channel != null)
        {
            foreach (var user in channel.Users.Where(user => user.Username != senderUsername))
            {
                user.SendMessage(message.Clone());
            }
        }
    }
}