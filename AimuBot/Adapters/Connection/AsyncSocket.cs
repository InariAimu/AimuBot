using System.Net.Sockets;
using System.Text;

using AimuBot.Adapters.Connection.Event;

namespace AimuBot.Adapters.Connection;

public class AsyncSocket : IDisposable
{
    public delegate void SocketEvent<in TArgs>(FuturedSocket sender, TArgs args);

    private readonly ByteBuffer _buff = ByteBuffer.Allocate(1024);

    private readonly FuturedSocket _listenSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private FuturedSocket _workSocket = null!;

    public SocketEvent<SocketConnectedEvent>? OnConnected;
    public SocketEvent<SocketDisconnectedEvent>? OnDisconnected;
    public SocketEvent<StringReceivedEvent>? OnStringReceived;

    public bool Connected => _workSocket.Connected;

    public void Dispose()
        => _workSocket?.Dispose();

    public async Task WaitConnection()
    {
        Console.WriteLine("Start listen");
        _workSocket = await _listenSocket.Accept("127.0.0.1", 10616);

        Console.WriteLine("Connected");
        OnConnected?.Invoke(_workSocket, new SocketConnectedEvent());
    }

    public async Task SendString(string str)
    {
        var sendBytes = Encoding.UTF8.GetBytes(str);
        var buff = ByteBuffer.Allocate(4 + sendBytes.Length);
        buff.WriteInt(sendBytes.Length);
        buff.WriteBytes(sendBytes, 0, sendBytes.Length);
        var len = await _workSocket.Send(buff.ToArray());

        //await _work_socket.Send(BitConverter.GetBytes(str.Length));
        //await _work_socket.Send(Encoding.UTF8.GetBytes(str));
    }

    public async Task Receive()
    {
        var buffer = new byte[1024];
        while (true)
            try
            {
                var bytesRead = await _workSocket.Receive(buffer);
                if (bytesRead > 0)
                {
                    _buff.WriteBytes(buffer, 0, bytesRead);
                    _buff.MarkReaderIndex();
                    var headLength = _buff.ReadInt();
                    var readByteLength = _buff.ReadableBytes();

                    if (headLength > readByteLength)
                    {
                        _buff.ResetReaderIndex();
                    }
                    else
                    {
                        var filthyBytes = _buff.ToArray();
                        var s = Encoding.UTF8.GetString(filthyBytes, 4, filthyBytes.Length - 4);

                        //Console.WriteLine("Msg: " + s);
                        OnStringReceived?.Invoke(_workSocket, new StringReceivedEvent(s));

                        _buff.Clear();
                        var useLength = filthyBytes.Length;
                        var lastOffSetLength = filthyBytes.Length - useLength;
                        if (lastOffSetLength > 0)
                            _buff.WriteBytes(filthyBytes, lastOffSetLength, filthyBytes.Length);
                    }
                }
                else
                {
                    Console.WriteLine("Disconnected");
                    OnDisconnected?.Invoke(_workSocket, new SocketDisconnectedEvent());
                    _workSocket.InnerSocket.Shutdown(SocketShutdown.Both);
                    _workSocket.InnerSocket.Close();
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.StackTrace}\n{ex.Message}");
                _workSocket.InnerSocket.Shutdown(SocketShutdown.Both);
                _workSocket.InnerSocket.Close();
                return;
            }
    }
}