using System.Net.Sockets;

namespace ChatClient.SocketClients;

public class TcpNetworkStreamProxy : ITcpNetworkWriterProxy
{
    private readonly NetworkStream stream;

    public TcpNetworkStreamProxy(NetworkStream stream)
    {
        this.stream = stream;
    }

    public ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return stream.ReadAsync(buffer, cancellationToken);
    }

    public ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return stream.WriteAsync(buffer, cancellationToken);
    }

    public void Dispose()
    {
        stream.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await stream.DisposeAsync();
    }
}