namespace ChatServer.SocketClients.Proxies.Tcp;

public interface ITcpNetworkWriterProxy : IDisposable, IAsyncDisposable
{
    ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default);
    ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default);
}