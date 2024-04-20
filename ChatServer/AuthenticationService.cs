namespace ChatServer;

public class AuthenticationService : IAuthenticationService
{
    public bool Authenticate(string username, string password)
    {
        return true;
    }
}