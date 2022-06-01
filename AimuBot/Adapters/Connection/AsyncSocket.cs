using System.Net.Sockets;
using System.Text;

using AimuBot.Adapters.Connection.Event;

namespace AimuBot.Adapters.Connection;

public class AsyncSocket : IDisposable
{
    public delegate void SocketEvent<in TArgs>(FuturedSocket sender, TArgs args);

    protected FuturedSocket _listen_socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    protected FuturedSocket _work_socket = null!;

    public ByteBuffer buff = ByteBuffer.Allocate(1024);

    public SocketEvent<SocketConnetedEvent>? OnConnected;
    public SocketEvent<StringReceivedEvent>? OnStringReceived;
    public SocketEvent<SocketDisconnetedEvent>? OnDisconnected;

    public bool Connected => _work_socket.Connected;

    public void Dispose()
        => _work_socket?.Dispose();

    public async Task WaitConnection()
    {
        Console.WriteLine("Start listen");
        _work_socket = await _listen_socket.Accept("127.0.0.1", 10616);

        Console.WriteLine("Connected");
        OnConnected?.Invoke(_work_socket, new());
    }

    public async Task SendString(string str)
    {
        byte[] sendBytes = Encoding.UTF8.GetBytes(str);
        ByteBuffer buff = ByteBuffer.Allocate(4 + sendBytes.Length);
        buff.WriteInt(sendBytes.Length);
        buff.WriteBytes(sendBytes, 0, sendBytes.Length);
        int len = await _work_socket.Send(buff.ToArray());

        //await _work_socket.Send(BitConverter.GetBytes(str.Length));
        //await _work_socket.Send(Encoding.UTF8.GetBytes(str));
    }

    public async Task Receive()
    {
        byte[]? buffer = new byte[1024];
        while (true)
        {
            try
            {
                int bytesRead = await _work_socket.Receive(buffer);
                if (bytesRead > 0)
                {
                    buff.WriteBytes(buffer, 0, bytesRead);
                    buff.MarkReaderIndex();
                    int headLength = buff.ReadInt();
                    int msgLength = headLength;
                    int readByteLength = buff.ReadableBytes();

                    if (msgLength > readByteLength)
                    {
                        buff.ResetReaderIndex();
                    }
                    else
                    {

                        byte[] filthyBytes = buff.ToArray();
                        string? s = Encoding.UTF8.GetString(filthyBytes, 4, filthyBytes.Length - 4);
                        //Console.WriteLine("Msg: " + s);
                        OnStringReceived?.Invoke(_work_socket, new(s));

                        buff.Clear();
                        int useLength = filthyBytes.Length;
                        int lastOffSetLength = filthyBytes.Length - useLength;
                        if (lastOffSetLength > 0)
                            buff.WriteBytes(filthyBytes, lastOffSetLength, filthyBytes.Length);
                    }
                }
                else
                {
                    Console.WriteLine("Disconnected");
                    OnDisconnected?.Invoke(_work_socket, new());
                    _work_socket.InnerSocket.Shutdown(SocketShutdown.Both);
                    _work_socket.InnerSocket.Close();
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.StackTrace}\n{ex.Message}");
                _work_socket.InnerSocket.Shutdown(SocketShutdown.Both);
                _work_socket.InnerSocket.Close();
                return;
            }
        }
    }
}
