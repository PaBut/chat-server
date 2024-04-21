using System.Net;
using System.Net.Sockets;
using ChatServer.Core.Client;
using ChatServer.Core.Services;
using ChatServer.Enums;
using ChatServer.Exceptions;
using ChatServer.Infrastructure;
using ChatServer.Logging;
using ChatServer.Logging.Utilities;
using ChatServer.Models;
using ChatServer.SocketClients;
using TaskExtensions = ChatServer.Extensions.TaskExtensions;

namespace ChatServer.Core;

public class ServerListener : IDisposable
{
    private readonly IPAddress listeningAddress;
    private readonly ushort listeningPort;
    private readonly byte udpConfirmationAttempts;
    private readonly ushort udpConfirmationTimeout;
    private readonly IChannelManager channelManager = new ChannelManager();
    private readonly CancellationTokenSource errorCancellationTokenSource = new();

    private readonly IList<Task> tasks = new List<Task>();

    private readonly IAuthenticationService authenticationService = new AuthenticationService();
    private readonly ILogger logger = new Logger(new StdoutWriter());

    public bool IsInternalError { get; set; } = false;
    
    public ServerListener(IPAddress listeningAddress, ushort listeningPort, byte udpConfirmationAttempts,
        ushort udpConfirmationTimeout)
    {
        this.listeningAddress = listeningAddress;
        this.listeningPort = listeningPort;
        this.udpConfirmationAttempts = udpConfirmationAttempts;
        this.udpConfirmationTimeout = udpConfirmationTimeout;
    }

    public async Task StartListeningAsync(CancellationToken cancellationToken = default)
    {
        var cancellationTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, errorCancellationTokenSource.Token);
        var tcpListener = Task.Run(() => TcpListen(cancellationTokenSource.Token));
        var udpListener = Task.Run(() => UdpListen(cancellationTokenSource.Token));

        var serverTasks = tasks.Union(new[] { tcpListener, udpListener });

        await TaskExtensions.WaitForAllWithCancellationSupport(serverTasks);
    }

    private async Task TcpListen(CancellationToken cancellationToken = default)
    {
        using TcpListener listener = new TcpListener(listeningAddress, listeningPort);
        try
        {
            listener.Start();
        }
        catch (SocketException)
        {
            IsInternalError = true;
            await errorCancellationTokenSource.CancelAsync();
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client = await listener.AcceptTcpClientAsync(cancellationToken);
            var ipkTcpClient = new IpkTcpClient(client, logger);
            var result = await ipkTcpClient.Listen(cancellationToken);
            if (result == null)
            {
                continue;
            }

            await ProcessRequest(ipkTcpClient, result, cancellationToken);
        }
    }

    private async Task UdpListen(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            IpkUdpClient ipkUdpClient;

            try
            {
                ipkUdpClient = IpkUdpClient.Create(listeningAddress, listeningPort, logger, udpConfirmationAttempts,
                    udpConfirmationTimeout);
            }
            catch (SocketException)
            {
                IsInternalError = true;
                await errorCancellationTokenSource.CancelAsync();
                return;
            }
            
            var result = await ipkUdpClient.Listen(cancellationToken);

            ipkUdpClient.RandomizePort();
            await ProcessRequest(ipkUdpClient, result!, cancellationToken);
        }
    }

    private async Task ProcessRequest(IIpkClient socketClient, ResponseResult response,
        CancellationToken cancellationToken = default)
    {
        if ((response.ProcessingResult != ResponseProcessingResult.Ok
             && response.ProcessingResult != ResponseProcessingResult.AlreadyProcessed)
            || response.Message.MessageType != MessageType.Auth)
        {
            bool socketException = false;
            try
            {
                await socketClient.SendError("Invalid message format", cancellationToken);
            }
            catch (NotReceivedConfirmException)
            {
            }
            catch (SocketException)
            {
                socketException = true;
            }
            finally
            {
                if (!socketException)
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

                socketClient.Dispose();
            }

            return;
        }

        UserClient client = new UserClient(socketClient, channelManager, authenticationService, cancellationToken);
        tasks.Add(Task.Run(async () =>
        {
            await client.Start();
            client.Dispose();
        }));
        client.ProcessReceivedMessage(response.Message);
    }

    public void Dispose()
    {
        errorCancellationTokenSource.Dispose();
    }
}