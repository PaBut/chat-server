using System.Net;
using System.Net.Sockets;

namespace ChatClient.SocketClients.Proxies.Udp;

public interface IUdpClientProxy : IDisposable
{
    ValueTask<UdpReceiveResult> ReceiveAsync(CancellationToken cancellationToken = default);
    Task<int> SendAsync(byte[] bytes, int length, IPEndPoint? endPoint);
    void UpdatePort(IPEndPoint endPoint);
}