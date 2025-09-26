using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BlazinglyFastHttp;

public class SocketReceiver : SocketAsyncEventArgs
{

}

public class BlazinglyFastHttp(int port = 8080)
{
    private Socket? _listenerSocket;
    private bool _isRunning;
    private Memory<byte> _buffer = new byte[4096];

    public async Task Start()
    {
        _listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _listenerSocket.Bind(new IPEndPoint(IPAddress.Any, port));
        _listenerSocket.Listen(1000);
        _isRunning = true;
        Console.WriteLine($"BlazinglyFastHttp listening on port {port}");

        for (var i = 0; i < 10; i++)
        {
            _ = Task.Run(() => AcceptLoop(_listenerSocket));
        }
    }

    private async Task AcceptLoop(Socket listener)
    {
        while (_isRunning)
        {
            try
            {
                Socket clientSocket = await listener.AcceptAsync();
                _ = Task.Run(() => HandleClient(clientSocket));
            }
            catch (ObjectDisposedException)
            {
                break; // Listener stopped
            }
            catch
            {
                // Optionally log error
            }
        }
    }

    private static async Task HandleClient(Socket clientSocket)
    {
        byte[] readBuffer = ArrayPool<byte>.Shared.Rent(4096);
        byte[] writeBuffer = ArrayPool<byte>.Shared.Rent(4096);
        try
        {
            await using var stream = new NetworkStream(clientSocket, ownsSocket: true);
            int bytesRead = await stream.ReadAsync(readBuffer);
            if (bytesRead == 0) return;

            const string response =
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/plain; charset=UTF-8\r\n" +
                "Content-Length: 11\r\n" +
                "Connection: close\r\n" +
                "\r\n" +
                "Hello World";

            Encoding.UTF8.GetBytes(
                response,
                0,
                response.Length,
                writeBuffer,
                0
            );

            await stream.WriteAsync(writeBuffer, 0, bytesRead);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(readBuffer);
            ArrayPool<byte>.Shared.Return(writeBuffer);
            clientSocket.Close();
        }
    }
}
