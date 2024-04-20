using System.Collections.Concurrent;
using ChatServer.Models;
using ChatServer.SocketClients;
using ChatServer.Utilities;

namespace ChatServer;

public class UserClient : IDisposable
{
    private readonly IIpkClient socketClient;
    private readonly IMessageProcessor messageProcessor;

    private readonly User user;
    private readonly BlockingCollection<Message> externalMessageQueue;
    private readonly CancellationTokenSource cancellationTokenSource;
    private readonly CancellationToken userCancellationToken;

    private bool isDisposed;

    public UserClient(IIpkClient socketClient, IChannelManager channelManager,
        IAuthenticationService authenticationService, CancellationToken serverCancellationToken)
    {
        this.externalMessageQueue = new BlockingCollection<Message>();
        this.cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(serverCancellationToken);
        this.userCancellationToken = cancellationTokenSource.Token;
        this.socketClient = socketClient;
        this.user = new User() { Client = this };
        this.messageProcessor = new MessageProcessor(authenticationService, channelManager, socketClient, user);
    }

    public void SendMessage(Message message)
    {
        externalMessageQueue.Add(message, userCancellationToken);
    }

    public async Task Start()
    {
        var senderTask = Task.Run(async () =>
        {
            while (!userCancellationToken.IsCancellationRequested)
            {
                try
                {
                    var message = externalMessageQueue.Take(userCancellationToken);
                    await socketClient.Send(message, userCancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }                
            }
        });

        var receiverTask = Task.Run(async () =>
        {
            while (!userCancellationToken.IsCancellationRequested)
            {
                var message = await socketClient.Listen(userCancellationToken);
                await messageProcessor.ProcessMessage(message!, user, userCancellationToken);
                if (messageProcessor.IsEndState)
                {
                    await cancellationTokenSource.CancelAsync();
                }
            }
        });

        try
        {
            await Task.WhenAll(senderTask, receiverTask);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (!messageProcessor.IsEndState)
            {
                Console.WriteLine("Bye sent 1");
                await socketClient.Leave();
                Console.WriteLine("Bye sent 2");
            }
        }
    }

    public async Task ProcessReceivedMessage(Message message)
    {
        await messageProcessor.ProcessMessage(new ResponseResult(message), user, userCancellationToken);
    }

    public void Dispose()
    {
        socketClient.Dispose();
        externalMessageQueue.Dispose();
        cancellationTokenSource.Dispose();
        isDisposed = true;
    }
}