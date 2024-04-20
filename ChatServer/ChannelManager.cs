using ChatServer.Models;

namespace ChatServer;

class ChannelManager : IChannelManager
{
    private readonly IList<Channel> channels;
    
    public ChannelManager()
    {
        channels = new List<Channel>();
    }
    
    public void AddChannel(string channelId)
    {
        channels.Add(new(channelId));
    }

    public void RemoveChannel(string channelId)
    {
        if (channels.Any(c => c.Name == channelId))
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
        
        if (!channels.Any(channel => channel.Name == channelId))
        {
            AddChannel(channelId);
        }
        
        GetChannel(channelId)!.AddUser(user);
    }

    public void RemoveUserFromChannel(User user, string channelId)
    {
       var channel = GetChannel(channelId);

       if (channel != null)
       {
           channel.RemoveUser(user);
       }
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