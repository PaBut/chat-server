using ChatServer.Enums;

namespace ChatServer.SocketClients.Utilities.Udp;

public static class UdpMessageTypeCoder
{
    private static readonly IDictionary<byte, MessageType> MessageTypeMap = new Dictionary<byte, MessageType>
    {
        { 0x00, MessageType.Confirm },
        { 0x01, MessageType.Reply },
        { 0x02, MessageType.Auth },
        { 0x03, MessageType.Join },
        { 0x04, MessageType.Msg },
        { 0xFE, MessageType.Err },
        { 0xFF, MessageType.Bye },
    };

    public static MessageType GetMessageType(byte code) => MessageTypeMap[code];

    public static byte GetMessageTypeCode(MessageType type)
        => MessageTypeMap.First(t => t.Value == type).Key;
}