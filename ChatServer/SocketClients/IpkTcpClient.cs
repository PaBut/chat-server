using System.Net;
using System.Net.Sockets;
using ChatClient.SocketClients;
using ChatServer.Enums;
using ChatServer.Logging;
using ChatServer.Models;
using ChatServer.SocketClients.Utilities.Tcp;
using ChatServer.Utilities.Tcp;
using SocketType = ChatServer.Enums.SocketType;

namespace ChatServer.SocketClients;

public class IpkTcpClient : IIpkClient
{
    private const string ServerName = "Server";
    
    private readonly ILogger logger;
    private readonly TcpClient client;
    private readonly ITcpNetworkWriterProxy clientStream;
    private readonly TcpMessageCoder messageCoder;
    private readonly TcpMessageQueue messageQueue;
    
    public IpkTcpClient(TcpClient client, ILogger logger)
    {
        this.client = client;
        this.logger = logger;
        this.clientStream = new TcpNetworkStreamProxy(client.GetStream());
        messageCoder = new TcpMessageCoder();
        messageQueue = new TcpMessageQueue(messageCoder);
    }
    
    public IpkTcpClient(TcpClient client, ITcpNetworkWriterProxy clientStream, ILogger logger)
    {
        this.client = client;
        this.clientStream = clientStream;
        this.logger = logger;
        messageCoder = new TcpMessageCoder();
        messageQueue = new TcpMessageQueue(messageCoder);
    }

    public SocketType SocketType { get; init; } = SocketType.Tcp;

    public async Task SendMessage(string messageContent, string senderUsername, CancellationToken cancellationToken = default)
    {
        var message = new Message()
        {
            MessageType = MessageType.Msg,
            Arguments = new Dictionary<MessageArguments, object>()
            {
                { MessageArguments.DisplayName, senderUsername },
                { MessageArguments.MessageContent, messageContent }
            }
        };
        await Send(message, cancellationToken);
    }

    public async Task SendReply(bool isSuccess, string messageContent, Message originalMessage, CancellationToken cancellationToken = default)
    {
        var message = new Message()
        {
            MessageType = MessageType.Reply,
            Arguments = new Dictionary<MessageArguments, object>()
            {
                { MessageArguments.ReplyStatus, isSuccess },
                { MessageArguments.MessageContent, messageContent }
            }
        };
        await Send(message, cancellationToken);
    }

    public async Task SendError(string errorContent, CancellationToken cancellationToken = default)
    {
        var message = new Message()
        {
            MessageType = MessageType.Err,
            Arguments = new Dictionary<MessageArguments, object>()
            {
                { MessageArguments.DisplayName, ServerName },
                { MessageArguments.MessageContent, errorContent }
            }
        };
        
        await Send(message, cancellationToken);
    }

    public async Task Leave()
    {
        var message = new Message()
        {
            MessageType = MessageType.Bye,
            Arguments = new Dictionary<MessageArguments, object>()
        };
        
        await Send(message);
    }

    public async Task<ResponseResult?> Listen(CancellationToken cancellationToken = default)
    {
        var message = messageQueue.Dequeue();
        if (message == null)
        {
            Memory<byte> buffer = new byte[2000];
            var byteCount = await clientStream.ReadAsync(buffer, cancellationToken);
            messageQueue.Enqueue(buffer.ToArray()[..byteCount]);
            message = messageQueue.Dequeue();
        }

        if (message == null)
        {
            return null;
        }
        
        logger.LogReceivedMessage(message, (IPEndPoint)client.Client.RemoteEndPoint!);
        
        var processingResult = ResponseProcessingResult.Ok;
        if (message!.MessageType == MessageType.Unknown)
        {
            processingResult = ResponseProcessingResult.ParsingError;
        }
        return new ResponseResult(message, processingResult);
    }

    public void Dispose()
    {
        clientStream.Dispose();
        client.Dispose();
    }
    
    public async Task Send(Message message, CancellationToken cancellationToken = default)
    {
        var byteMessage = messageCoder.GetByteMessage(message);
        
        logger.LogSentMessage(message, (IPEndPoint)client.Client.RemoteEndPoint!);
        await clientStream.WriteAsync(byteMessage, cancellationToken);
    }
}