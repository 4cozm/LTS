using System.Net.Sockets;

namespace LTS.Services;

public interface ITcpConnectionService
{
    NetworkStream? GetStream();
    bool IsConnected { get; }
    void SetStream(NetworkStream stream);
}

public class TcpConnectionService : ITcpConnectionService
{
    private NetworkStream? _stream;

    public void SetStream(NetworkStream stream)
    {
        _stream = stream;
    }

    public NetworkStream? GetStream()
    {
        return _stream;
    }

    public bool IsConnected => _stream != null && _stream.CanWrite;
}
