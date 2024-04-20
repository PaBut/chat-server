namespace ChatServer.Models;

public enum MessageType
{
    Auth,
    Join,
    Reply, 
    Err,
    Bye,
    Msg,
    Confirm,
    Unknown,
}