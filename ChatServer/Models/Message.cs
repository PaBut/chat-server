using ChatServer.Enums;

namespace ChatServer.Models;

public class Message
{
    public MessageType MessageType { get; set; }
    public IDictionary<MessageArguments, object> Arguments { get; set; }


    public Message Clone()
    {
        var newMessage = new Message()
        {
            MessageType = MessageType,
            Arguments = new Dictionary<MessageArguments, object>()
        };

        foreach (var entry in Arguments)
        {
            newMessage.Arguments.Add(entry.Key, entry.Value);
        }

        return newMessage;
    }

    public static Message UnknownMessage => new()
    {
        MessageType = MessageType.Unknown,
        Arguments = new Dictionary<MessageArguments, object>()
    };
}