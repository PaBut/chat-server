using System.Collections.Concurrent;
using System.Net.Sockets;
using ChatServer.Core.Services;
using ChatServer.Exceptions;
using ChatServer.Infrastructure;
using ChatServer.Models;
using ChatServer.SocketClients;
using SocketType = ChatServer.Enums.SocketType;
using TaskExtensions = ChatServer.Extensions.TaskExtensions;

namespace ChatServer.Core.Client;

public class UserClient : IDisposable
{
    private readonly IIpkClient socketClient;
    private readonly IMessageProcessor messageProcessor;

    private readonly User user;
    private readonly BlockingCollection<Message> externalMessageQueue;
    private readonly BlockingCollection<ResponseResult> internalResponseProcessQueue = new();
    private readonly CancellationTokenSource cancellationTokenSource;
    private readonly CancellationToken userCancellationToken;

    private readonly CancellationTokenSource byeSentTokenSource = new();
    private bool isSocketException = false;

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
                catch (SocketException)
                {
                    await cancellationTokenSource.CancelAsync();
                    isSocketException = true;
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
                catch (OperationCanceledException)
                {
                }
                catch (SocketException)
                {
                    await cancellationTokenSource.CancelAsync();
                    isSocketException = true;
                }
            }

            if (!messageProcessor.IsEndState && !isSocketException)
            {
                await SendBye();
                await byeSentTokenSource.CancelAsync();
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
                catch (OperationCanceledException)
                {
                }
                catch (SocketException)
                {
                    await cancellationTokenSource.CancelAsync();
                    isSocketException = true;
                }
            }

            while (!messageProcessor.IsEndState && !byeSentTokenSource.IsCancellationRequested && !isSocketException &&
                   socketClient.SocketType == SocketType.Udp)
            {
                try
                {
                    await socketClient.Listen(byeSentTokenSource.Token);
                }
                catch(SocketException){}
            }
        });

        await TaskExtensions.WaitForAllWithCancellationSupport([senderTask, processingTask, receiverTask]);
    }

    public void ProcessReceivedMessage(Message message)
    {
        internalResponseProcessQueue.Add(new ResponseResult(message));
    }

    public void Dispose()
    {
        socketClient.Dispose();
        externalMessageQueue.Dispose();
        internalResponseProcessQueue.Dispose();
        cancellationTokenSource.Dispose();
        byeSentTokenSource.Dispose();
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
        catch (SocketException)
        {
        }
    }
}