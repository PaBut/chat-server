using System.Net;

namespace ChatServer.Models;

public class User
{
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public Channel? Channel { get; set; }
    public UserClient Client { get; set; }

    public void SendMessage(Message message)
    {
        Client.SendMessage(message);
    }
}