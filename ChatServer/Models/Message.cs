namespace ChatServer.Models;

public class Message
{
    public MessageType MessageType { get; set; }
    public IDictionary<MessageArguments, object> Arguments { get; set; }

    // public Message(MessageType messageType) : this(messageType, new Dictionary<MessageArguments, object>())
    // {
    // }
    //
    // public Message(MessageType messageType, IDictionary<MessageArguments, object> arguments)
    // {
    //     MessageType = messageType;
    //     Arguments = arguments;
    // }

    public static Message FromCommandLine(string line, out string? errorResponse)
    {
        errorResponse = null;

        if (line[0] != '/')
        {
            return new Message{
                MessageType = MessageType.Msg,
                Arguments = new Dictionary<MessageArguments, object>()
                {
                    { MessageArguments.MessageContent, line }
                }
            };
        }

        var parts = line.Split(' ');
        var command = parts[0];

        if (command == "/auth")
        {
            if (parts.Length != 4)
            {
                return Message.UnknownMessage;
            }

            return new Message()
            {
                MessageType = MessageType.Auth,
                Arguments = new Dictionary<MessageArguments, object>()
                {
                    { MessageArguments.UserName, parts[1] },
                    { MessageArguments.Secret, parts[2] },
                    { MessageArguments.DisplayName, parts[3] },
                }
            };
        }
        else if (command == "/join")
        {
            if (parts.Length != 2)
            {
                return Message.UnknownMessage;
            }

            return new Message()
            {
                MessageType = MessageType.Join,
                Arguments = new Dictionary<MessageArguments, object>()
                {
                    { MessageArguments.ChannelId, parts[1] },
                }
            };
        }

        return Message.UnknownMessage;
    }

    public override string? ToString()
    {
        if (MessageType == MessageType.Msg)
        {
            return $"{(string)Arguments[MessageArguments.DisplayName]}: " +
                   $"{(string)Arguments[MessageArguments.MessageContent]}";
        }

        if (MessageType == MessageType.Err)
        {
            return $"ERROR FROM {(string)Arguments[MessageArguments.DisplayName]}: " +
                   $"{(string)Arguments[MessageArguments.MessageContent]}";
        }

        if (MessageType == MessageType.Reply)
        {
            var success = (bool)Arguments[MessageArguments.ReplyStatus] ? "SUCCESS" : "ERROR";
            return $"{success}: {(string)Arguments[MessageArguments.MessageContent]}";
        }

        return null;
    }

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