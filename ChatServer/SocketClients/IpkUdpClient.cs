using System.Collections.Concurrent;
using System.Net;
using ChatClient.SocketClients.Proxies.Udp;
using ChatServer.Models;
using ChatServer.Utilities.Udp;
using UdpClient = System.Net.Sockets.UdpClient;

namespace ChatServer.SocketClients;

public class IpkUdpClient : IIpkClient
{
    private const string ServerName = "Server";

    private readonly ILogger logger;
    private readonly IUdpClientProxy client;
    private readonly UdpMessageCoder messageCoder = new();
    private readonly ushort timeout;
    private readonly byte retrials;

    private IPEndPoint? remoteEndPoint;
    private readonly IPAddress listeningAddress;
    private ushort currentMessageId = 0;
    private List<ushort> seenMessages = new();
    private List<ushort> confirmedMessages = new();
    private ManualResetEvent confirmEvent = new(false);

    private readonly object messageIdLocker = new object();

    public IpkUdpClient(IUdpClientProxy client, IPEndPoint? endpoint, IPAddress listeningAddress, byte retrials,
        ushort timeout, ILogger logger)
    {
        this.client = client;
        this.retrials = retrials;
        this.timeout = timeout;
        this.logger = logger;
        this.remoteEndPoint = endpoint;
        this.listeningAddress = listeningAddress;
    }

    public async Task SendMessage(string messageContent, string senderUsername,
        CancellationToken cancellationToken = default)
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

    public async Task SendReply(bool isSuccess, string messageContent, Message originalMessage,
        CancellationToken cancellationToken = default)
    {
        var message = new Message()
        {
            MessageType = MessageType.Reply,
            Arguments = new Dictionary<MessageArguments, object>()
            {
                { MessageArguments.ReplyStatus, isSuccess },
                { MessageArguments.MessageContent, messageContent },
                { MessageArguments.ReferenceMessageId, originalMessage.Arguments[MessageArguments.MessageId] }
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
        Console.WriteLine("Start receiving message");

        var response = await client.ReceiveAsync(cancellationToken);

        Console.WriteLine("Received message");

        var message = messageCoder.DecodeMessage(response.Buffer);

        if (message.MessageType == MessageType.Unknown)
        {
            return new ResponseResult(message, ResponseProcessingResult.ParsingError);
        }

        remoteEndPoint ??= response.RemoteEndPoint;

        logger.LogReceivedMessage(message, remoteEndPoint);

        if (message.MessageType == MessageType.Confirm)
        {
            var messageId = (ushort)message.Arguments[MessageArguments.ReferenceMessageId];

            if (!confirmedMessages.Contains(messageId))
            {
                confirmedMessages.Add(messageId);
                confirmEvent.Set();
            }
        }
        else
        {
            var messageId = (ushort)message.Arguments[MessageArguments.MessageId];
            await SendConfirmation(messageId, cancellationToken);
            if (seenMessages.Contains(messageId))
            {
                return new ResponseResult(message, ResponseProcessingResult.AlreadyProcessed);
            }

            seenMessages.Add(messageId);
            confirmEvent.Set();
        }

        return new ResponseResult(message);
    }

    public async Task Send(Message message, CancellationToken cancellationToken = default)
    {
        lock (messageIdLocker)
        {
            var messageId = currentMessageId++;

            message.Arguments[MessageArguments.MessageId] = messageId;   
        }
        
        await SendWithRetrial(message, cancellationToken);
    }

    public void RandomizePort()
    {
        client.UpdatePort(new IPEndPoint(listeningAddress, 0));
    }

    private async Task SendWithRetrial(Message message, CancellationToken cancellationToken = default)
    {
        var byteMessage = messageCoder.GetByteMessage(message);
        var messageId = (ushort)message.Arguments[MessageArguments.MessageId];
        for (int i = 0; i < retrials + 1 && !confirmedMessages.Contains(messageId); i++)
        {
            logger.LogSentMessage(message, remoteEndPoint!);
            await client.SendAsync(byteMessage, byteMessage.Length, remoteEndPoint);

            // using CancellationTokenSource cts = new(timeout);
            // using CancellationTokenSource tokenSource =
            //     CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);

            try
            {
                var mustBeDisposed = false;
                var task = Task.Run(() =>
                {
                    while (!confirmedMessages.Contains(messageId) && !mustBeDisposed)
                    {
                        confirmEvent.WaitOne();
                        confirmEvent.Reset();
                    }
                }, cancellationToken);

                await Task.WhenAny(task, Task.Delay(timeout, cancellationToken));
                confirmEvent.Set();
                mustBeDisposed = true;
            }
            catch (Exception)
            {
                Console.WriteLine("Cancelled");
            }
        }

        if (!confirmedMessages.Contains(messageId))
        {
            Console.WriteLine($"Not confirmed message in client {remoteEndPoint.Port}");
            throw new NotReceivedConfirmException();
        }
    }

    private async Task SendConfirmation(ushort messageId, CancellationToken cancellationToken = default)
    {
        var message = new Message()
        {
            MessageType = MessageType.Confirm,
            Arguments = new Dictionary<MessageArguments, object>()
            {
                { MessageArguments.ReferenceMessageId, messageId },
            }
        };

        logger.LogSentMessage(message, remoteEndPoint!);

        var byteMessage = messageCoder.GetByteMessage(message);

        await client.SendAsync(byteMessage, byteMessage.Length, remoteEndPoint);
    }

    public void Dispose()
    {
        client.Dispose();
    }

    public static IpkUdpClient? Create(IPAddress listeningAddress, ushort port, ILogger logger, byte retrials,
        ushort timeout)
    {
        try
        {
            var client = new UdpClient();
            client.Client.Bind(new IPEndPoint(listeningAddress, port));

            return new IpkUdpClient(new UdpClientProxy(client), null, listeningAddress, retrials, timeout, logger);
        }
        catch (Exception)
        {
            return null;
        }
    }
}