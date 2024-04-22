using System.Net;
using System.Net.Sockets;

namespace ChatServer.SocketClients.Proxies.Udp;

public class UdpClientProxy : IUdpClientProxy
{
    private UdpClient client;

    public UdpClientProxy(UdpClient client)
    {
        this.client = client;
    }

    public ValueTask<UdpReceiveResult> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        return client.ReceiveAsync(cancellationToken);
    }

    public Task<int> SendAsync(byte[] bytes, int length, IPEndPoint? endPoint)
    {
        return client.SendAsync(bytes, length, endPoint);
    }

    public void UpdatePort(IPEndPoint endPoint)
    {
        client.Dispose();
        client = new UdpClient();
        client.Client.Bind(endPoint);
    }

    public void Dispose()
    {
        client.Dispose();
    }
}