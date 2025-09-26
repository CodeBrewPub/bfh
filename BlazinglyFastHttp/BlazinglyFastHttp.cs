using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BlazinglyFastHttp;

public class BlazinglyFastHttp(int port = 8080)
{
    private Socket? _listenerSocket;
    private bool _isRunning;

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
        byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);
        try
        {
            await using var stream = new NetworkStream(clientSocket, ownsSocket: true);
            int bytesRead = await stream.ReadAsync(buffer);
            if (bytesRead == 0) return;
            // Simple response

            byte[] data = Encoding.UTF8.GetBytes(
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/plain; charset=UTF-8\r\n" +
                "Content-Length: 11\r\n" +
                "Connection: close\r\n" +
                "\r\n" +
                "Hello World"
            );

            await stream.WriteAsync(data);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            clientSocket.Close();
        }
    }
}
