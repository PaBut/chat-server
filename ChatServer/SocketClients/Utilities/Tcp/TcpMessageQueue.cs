using System.Text;
using ChatServer.Models;

namespace ChatServer.SocketClients.Utilities.Tcp;

public class TcpMessageQueue
{
    private const string CLRF = "\r\n";
    private readonly Queue<Message> messageQueue = new();
    private readonly TcpMessageCoder _messageCoder;

    public TcpMessageQueue(TcpMessageCoder messageCoder)
    {
        this._messageCoder = messageCoder;
    }

    public void Enqueue(byte[] encodedMessage)
    {
        var messageParts = Encoding.UTF8.GetString(encodedMessage).Split(CLRF)
            .Where(part => !string.IsNullOrEmpty(part));
        foreach (var part in messageParts)
        {
            var message = _messageCoder.DecodeMessage(part);
            messageQueue.Enqueue(message);
        }
    }

    public Message? Dequeue()
    {
        if (messageQueue.Count == 0)
        {
            return null;
        }

        return messageQueue.Dequeue();
    }
}