namespace ChatServer.Infrastructure;

public interface IAuthenticationService
{
    public bool Authenticate(string username, string password);
}