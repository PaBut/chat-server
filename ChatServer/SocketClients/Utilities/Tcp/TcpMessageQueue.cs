using System.Text;
using ChatServer.Models;

namespace ChatServer.SocketClients.Utilities.Tcp;

public class TcpMessageQueue
{
    private const string CLRF = "\r\n";
    private Queue<(string message, bool hasEnd)> messageQueue = new();
    private readonly TcpMessageCoder messageCoder;

    public TcpMessageQueue(TcpMessageCoder messageCoder)
    {
        this.messageCoder = messageCoder;
    }

    public void Enqueue(byte[] encodedMessage)
    {
        var messageString = Encoding.UTF8.GetString(encodedMessage);
        var messages = ParseMessages(messageString);

        if (messageQueue.Any() && !messageQueue.Last().hasEnd)
        {
            var lastMessage = messageQueue.Last();
            var firstMessage = messages.First();
            
            messageQueue = new Queue<(string message, bool hasEnd)>(messageQueue.Take(messageQueue.Count - 1));
            
            if (firstMessage.hasEnd)
            {
                firstMessage.message += CLRF;
            }

            var combinedMessage = lastMessage.message + firstMessage.message;
            var combineMessagesParsingResult = ParseMessages(combinedMessage);
            foreach (var part in combineMessagesParsingResult)
            {
                messageQueue.Enqueue(part);
            }

            messages = messages.Skip(1);
        }

        foreach (var part in messages)
        {
            messageQueue.Enqueue(part);
        }
    }

    public Message? Dequeue()
    {
        if (messageQueue.Count == 0 || !messageQueue.Peek().Item2)
        {
            return null;
        }

        return messageCoder.DecodeMessage(messageQueue.Dequeue().message);
    }

    private static IEnumerable<(string message, bool hasEnd)> ParseMessages(string messagesString)
    {
        var messageString = string.Empty;

        for (int i = 0; i < messagesString.Length; i++)
        {
            if (messagesString[i] == '\r' && i + 1 < messagesString.Length && messagesString[i + 1] == '\n')
            {
                yield return (messageString, true);
                messageString = string.Empty;
                i++;
            }
            else
            {
                messageString += messagesString[i];
            }
        }

        if (!string.IsNullOrEmpty(messageString))
        {
            yield return (messageString, false);
        }
    }
}