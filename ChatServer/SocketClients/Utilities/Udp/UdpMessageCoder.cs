using System.Text;
using ChatServer.Enums;
using ChatServer.Models;

namespace ChatServer.Utilities.Udp;

public class UdpMessageCoder
{
    public Message DecodeMessage(byte[] message)
    {
        if (message.Length < 3)
        {
            return Message.UnknownMessage;
        }

        var messageType = UdpMessageTypeCoder.GetMessageType(message[0]);
        var arguments = new Dictionary<MessageArguments, object>
        {
            {
                messageType == MessageType.Confirm ? MessageArguments.ReferenceMessageId : MessageArguments.MessageId,
                BitConverter.ToUInt16(message[1..3])
            }
        };

        switch (messageType)
        {
            case MessageType.Reply:
                if (message.Length < 8)
                {
                    return Message.UnknownMessage;
                }

                arguments.Add(MessageArguments.ReplyStatus, message[3] == 1);

                arguments.Add(MessageArguments.ReferenceMessageId, BitConverter.ToUInt16(message[4..6]));

                var messageContentEnd1 = GetEndOfTheFloatingMessage(6, message);

                arguments.Add(MessageArguments.MessageContent, Encoding.UTF8.GetString(message[6..messageContentEnd1]));

                break;

            case MessageType.Auth:
                if (message.Length < 9)
                {
                    return Message.UnknownMessage;
                }

                var usernameEnd = GetEndOfTheFloatingMessage(3, message);

                arguments.Add(MessageArguments.UserName, Encoding.UTF8.GetString(message[3..usernameEnd]));

                var displayNameEnd1 = GetEndOfTheFloatingMessage(usernameEnd + 1, message);

                arguments.Add(MessageArguments.DisplayName,
                    Encoding.UTF8.GetString(message[(usernameEnd + 1)..displayNameEnd1]));

                var secretEnd = GetEndOfTheFloatingMessage(displayNameEnd1 + 1, message);

                arguments.Add(MessageArguments.Secret,
                    Encoding.UTF8.GetString(message[(displayNameEnd1 + 1)..secretEnd]));

                break;

            case MessageType.Join:
                if (message.Length < 7)
                {
                    return Message.UnknownMessage;
                }

                var channelIdEnd = GetEndOfTheFloatingMessage(3, message);

                arguments.Add(MessageArguments.ChannelId, Encoding.UTF8.GetString(message[3..channelIdEnd]));

                var displayNameEnd2 = GetEndOfTheFloatingMessage(channelIdEnd + 1, message);

                arguments.Add(MessageArguments.DisplayName,
                    Encoding.UTF8.GetString(message[(channelIdEnd + 1)..displayNameEnd2]));

                break;

            case MessageType.Err:
            case MessageType.Msg:
                if (message.Length < 7)
                {
                    return Message.UnknownMessage;
                }

                var displayNameEnd = GetEndOfTheFloatingMessage(3, message);

                arguments.Add(MessageArguments.DisplayName, Encoding.UTF8.GetString(message[3..displayNameEnd]));

                var messageContentEnd3 = GetEndOfTheFloatingMessage(displayNameEnd + 1, message);

                arguments.Add(MessageArguments.MessageContent,
                    Encoding.UTF8.GetString(message[(displayNameEnd + 1)..messageContentEnd3]));

                break;
        }

        return new Message
        {
            MessageType = messageType,
            Arguments = arguments
        };
    }

    public byte[] GetByteMessage(Message message)
    {
        List<byte> byteMessage = new();

        byteMessage.Add(UdpMessageTypeCoder.GetMessageTypeCode(message.MessageType));

        if (message.MessageType == MessageType.Confirm)
        {
            byteMessage.AddRange(BitConverter.GetBytes((ushort)message.Arguments[MessageArguments.ReferenceMessageId]));

            return byteMessage.ToArray();
        }

        byteMessage.AddRange(BitConverter.GetBytes((ushort)message.Arguments[MessageArguments.MessageId]));

        switch (message.MessageType)
        {
            case MessageType.Err:
            case MessageType.Msg:
                byteMessage.AddRange(Encoding.UTF8.GetBytes((string)message.Arguments[MessageArguments.DisplayName]));
                byteMessage.Add(0);
                byteMessage.AddRange(
                    Encoding.UTF8.GetBytes((string)message.Arguments[MessageArguments.MessageContent]));
                byteMessage.Add(0);
                break;
            case MessageType.Auth:
                byteMessage.AddRange(Encoding.UTF8.GetBytes((string)message.Arguments[MessageArguments.UserName]));
                byteMessage.Add(0);
                byteMessage.AddRange(
                    Encoding.UTF8.GetBytes((string)message.Arguments[MessageArguments.DisplayName]));
                byteMessage.Add(0);
                byteMessage.AddRange(
                    Encoding.UTF8.GetBytes((string)message.Arguments[MessageArguments.Secret]));
                byteMessage.Add(0);
                break;
            case MessageType.Join:
                byteMessage.AddRange(Encoding.UTF8.GetBytes((string)message.Arguments[MessageArguments.ChannelId]));
                byteMessage.Add(0);
                byteMessage.AddRange(
                    Encoding.UTF8.GetBytes((string)message.Arguments[MessageArguments.DisplayName]));
                byteMessage.Add(0);
                break;
            case MessageType.Reply:
                byteMessage.AddRange(BitConverter.GetBytes((bool)message.Arguments[MessageArguments.ReplyStatus]));
                byteMessage.AddRange(
                    BitConverter.GetBytes((ushort)message.Arguments[MessageArguments.ReferenceMessageId]));
                byteMessage.AddRange(
                    Encoding.UTF8.GetBytes((string)message.Arguments[MessageArguments.MessageContent]));
                byteMessage.Add(0);
                break;
            case MessageType.Bye:
                break;
        }

        return byteMessage.ToArray();
    }

    private int GetEndOfTheFloatingMessage(int start, byte[] message)
    {
        for (int i = start; i < message.Length; i++)
        {
            if (message[i] == 0x00)
            {
                return i;
            }
        }

        return -1;
    }
}