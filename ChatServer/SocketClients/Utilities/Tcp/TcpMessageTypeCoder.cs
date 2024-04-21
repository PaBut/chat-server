using ChatServer.Enums;
using ChatServer.Models;

namespace ChatServer.Utilities.Tcp;

public static class TcpMessageTypeCoder
{
    private static readonly IDictionary<string, MessageType> MessageTypeMap = new Dictionary<string, MessageType>
    {
        { "REPLY", MessageType.Reply },
        { "AUTH", MessageType.Auth },
        { "JOIN", MessageType.Join },
        { "MSG FROM", MessageType.Msg },
        { "ERR FROM", MessageType.Err },
        { "BYE", MessageType.Bye },
    };

    public static MessageType? GetMessageType(string code) =>
        MessageTypeMap.ContainsKey(code) ? MessageTypeMap[code] : MessageType.Unknown;

    public static string GetMessageString(MessageType type)
        => MessageTypeMap.FirstOrDefault(t => t.Value == type).Key;
}