namespace ChatServer.Models;

public class Channel
{
    public Channel(string name)
    {
        Name = name;
        Users = new List<User>();
    }

    public string Name { get; set; }
    public IList<User> Users { get; set; }
    
    public void AddUser(User user)
    {
        Users.Add(user);
    }
    
    public void RemoveUser(User user)
    {
        if (Users.Contains(user))
        {
            Users.Remove(user);
        }
    }
}