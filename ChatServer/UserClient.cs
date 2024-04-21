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
    private readonly BlockingCollection<ResponseResult> internalResponseProcessQueue = new();
    private readonly CancellationTokenSource cancellationTokenSource;
    private readonly CancellationToken userCancellationToken;

    private CancellationTokenSource byeSent = new();

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
                catch (NotReceivedConfirmException ex)
                {
                    await SendBye();
                    await cancellationTokenSource.CancelAsync();
                }
            }
        });

        var processingTask = Task.Run(async () =>
        {
            while (!userCancellationToken.IsCancellationRequested)
            {
                try
                {
                    var responseResult = internalResponseProcessQueue.Take(userCancellationToken);
                    await messageProcessor.ProcessMessage(responseResult, user, userCancellationToken);

                    if (messageProcessor.IsEndState)
                    {
                        await cancellationTokenSource.CancelAsync();
                    }
                }
                catch (NotReceivedConfirmException)
                {
                    await SendBye();
                    await cancellationTokenSource.CancelAsync();
                }
                catch(OperationCanceledException){}
            }

            if (!messageProcessor.IsEndState)
            {
                await SendBye();
                await byeSent.CancelAsync();
            }
        });

        var receiverTask = Task.Run(async () =>
        {
            while (!userCancellationToken.IsCancellationRequested)
            {
                try
                {
                    var message = await socketClient.Listen(userCancellationToken);

                    internalResponseProcessQueue.Add(message!, userCancellationToken);    
                }
                catch(OperationCanceledException){}
            }


            while (!byeSent.IsCancellationRequested)
            {
                await socketClient.Listen(byeSent.Token);
            }
            
        });

        await TaskExtensions.WaitForAllWithCancellationSupport([senderTask, processingTask, receiverTask]);
        Console.WriteLine("All done here");
    }

    public async Task ProcessReceivedMessage(Message message)
    {
        await messageProcessor.ProcessMessage(new ResponseResult(message), user, userCancellationToken);
    }

    public void Dispose()
    {
        socketClient.Dispose();
        externalMessageQueue.Dispose();
        internalResponseProcessQueue.Dispose();
        cancellationTokenSource.Dispose();
        byeSent.Dispose();
        isDisposed = true;
        Console.WriteLine($"Client {user.Username} is disposed");
    }

    private async Task SendBye()
    {
        try
        {
            await socketClient.Leave();
        }
        catch (NotReceivedConfirmException)
        {
        }
    }
}