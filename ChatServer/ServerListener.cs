using System.Net;
using System.Net.Sockets;
using ChatServer.Models;
using ChatServer.SocketClients;

namespace ChatServer;

public class ServerListener
{
    private readonly IPAddress ListeningAddress;
    private readonly ushort ListeningPort;
    private readonly byte udpConfirmationAttempts;
    private readonly ushort udpConfirmationTimeout;
    private readonly IChannelManager channelManager = new ChannelManager();

    private readonly IList<Task> tasks = new List<Task>();
    private readonly IList<UserClient> userClients = new List<UserClient>();

    private readonly IAuthenticationService authenticationService = new AuthenticationService();
    private readonly ILogger logger = new Logger(new StdoutWriter());

    public ServerListener(IPAddress listeningAddress, ushort listeningPort, byte udpConfirmationAttempts,
        ushort udpConfirmationTimeout)
    {
        ListeningAddress = listeningAddress;
        ListeningPort = listeningPort;
        this.udpConfirmationAttempts = udpConfirmationAttempts;
        this.udpConfirmationTimeout = udpConfirmationTimeout;
    }

    public async Task StartListeningAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Server is listening on {ListeningAddress}:{ListeningPort}");

        var tcpListener = Task.Run(() => TcpListen(cancellationToken));
        var udpListener = Task.Run(() => UdpListen(cancellationToken));

        var serverTasks = tasks.Union(new[] { tcpListener, udpListener });
        
        bool leftToDispose = true;
        while (leftToDispose)
        {
            leftToDispose = false;
            try
            {
                await Task.WhenAll(serverTasks.Where(t => !t.IsCompleted));
            }
            catch (OperationCanceledException)
            {
                leftToDispose = true;
            }   
        }
    }

    private async Task TcpListen(CancellationToken cancellationToken = default)
    {
        using TcpListener listener = new TcpListener(ListeningAddress, ListeningPort);
        listener.Start();
        
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
            var ipkUdpClient = IpkUdpClient.Create(ListeningAddress, ListeningPort, logger, udpConfirmationAttempts,
                udpConfirmationTimeout);
            if (ipkUdpClient == null)
            {
                // TODO: Rewrite this logic
                throw new NullReferenceException();
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
            await socketClient.SendError("Invalid message format", cancellationToken);
            await socketClient.Leave();
            socketClient.Dispose();
            return;
        }

        UserClient client = new UserClient(socketClient, channelManager, authenticationService, cancellationToken);
        tasks.Add(Task.Run(async () =>
        {
            await client.Start();
            client.Dispose();
        }));
        await client.ProcessReceivedMessage(response.Message);
        userClients.Add(client);
    }
}