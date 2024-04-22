using System.Text;
using ChatServer.Enums;
using ChatServer.Models;

namespace ChatServer.SocketClients.Utilities.Tcp;

public class TcpMessageCoder
{
    private const string CLRF = "\r\n";

    public byte[] GetByteMessage(Message message)
    {
        string messageString = TcpMessageTypeCoder.GetMessageString(message.MessageType);

        if (message.MessageType == MessageType.Bye)
        {
            return Encoding.UTF8.GetBytes(messageString + CLRF);
        }

        messageString += " ";

        if (message.MessageType == MessageType.Join)
        {
            messageString += (string)message.Arguments[MessageArguments.ChannelId] + " AS " +
                             (string)message.Arguments[MessageArguments.DisplayName];
        }
        else if (message.MessageType == MessageType.Auth)
        {
            messageString += (string)message.Arguments[MessageArguments.UserName] + " AS " +
                             (string)message.Arguments[MessageArguments.DisplayName] + " USING " +
                             (string)message.Arguments[MessageArguments.Secret];
        }
        else if (message.MessageType == MessageType.Msg || message.MessageType == MessageType.Err)
        {
            messageString += (string)message.Arguments[MessageArguments.DisplayName] + " IS " +
                             (string)message.Arguments[MessageArguments.MessageContent];
        }
        else if (message.MessageType == MessageType.Reply)
        {
           messageString += ((bool)message.Arguments[MessageArguments.ReplyStatus] ? "OK" : "NOK") + " IS " +
                            (string)message.Arguments[MessageArguments.MessageContent];
        }
        else
        {
            throw new Exception("Not supported");
        }
        
        return Encoding.UTF8.GetBytes(messageString + CLRF);
    }

    public Message DecodeMessage(string messageString)
    {
        string[] messageParts = messageString.Split(" ");

        var stringMessageType = messageParts[0];

        if (messageParts.Length > 1 && messageParts[1] == "FROM")
        {
            stringMessageType = string.Join(' ', messageParts[..2]);
        }

        var messageType = TcpMessageTypeCoder.GetMessageType(stringMessageType);

        var messageArguments = new Dictionary<MessageArguments, object>();

        if (messageType == MessageType.Msg || messageType == MessageType.Err)
        {
            if (messageParts.Length < 5)
            {
                return Message.UnknownMessage;
            }
            messageArguments.Add(MessageArguments.DisplayName, messageParts[2]);

            if (messageParts[3] != "IS")
            {
                return Message.UnknownMessage;
            }

            messageArguments.Add(MessageArguments.MessageContent, string.Join(' ', messageParts[4..]));
        }
        else if (messageType == MessageType.Auth)
        {
            if (messageParts.Length != 6)
            {
                return Message.UnknownMessage;
            }
            messageArguments.Add(MessageArguments.UserName, messageParts[1]);
            if (messageParts[2] != "AS")
            {
                return Message.UnknownMessage;
            }
            messageArguments.Add(MessageArguments.DisplayName, messageParts[3]);
            if (messageParts[4] != "USING")
            {
                return Message.UnknownMessage;
            }
            messageArguments.Add(MessageArguments.Secret, messageParts[5]);
        }
        else if (messageType == MessageType.Join)
        {
            if (messageParts.Length != 4)
            {
                return Message.UnknownMessage;
            }
            messageArguments.Add(MessageArguments.ChannelId, messageParts[1]);
            if(messageParts[2] != "AS")
            {
                return Message.UnknownMessage;
            }
            messageArguments.Add(MessageArguments.DisplayName, messageParts[3]);
        }
        else if (messageType == MessageType.Reply)
        {
            if (messageParts.Length < 4)
            {
                return Message.UnknownMessage;
            }
            
            if (messageParts[1] != "OK" && messageParts[1] != "NOK")
            {
                return Message.UnknownMessage;
            }

            messageArguments.Add(MessageArguments.ReplyStatus, messageParts[1] == "OK");

            if (messageParts[2] != "IS")
            {
                return Message.UnknownMessage;
            }

            messageArguments.Add(MessageArguments.MessageContent, string.Join(' ', messageParts[3..]));
        }
        else if (messageType != MessageType.Bye)
        {
            messageType = MessageType.Unknown;
        }

        return new Message
        {
            MessageType = messageType.Value,
            Arguments = messageArguments
        };
    }
}