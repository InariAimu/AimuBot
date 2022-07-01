using System.Net.Sockets;
using System.Text;

using AimuBot.Adapters.Connection.Event;

namespace AimuBot.Adapters.Connection;

public class StringOverSocket
{
    private readonly RingBuffer<byte> _buff = new (1024 * 64);
    public FuturedSocket WorkSocket { get; }

    public StringOverSocket(FuturedSocket socket)
    {
        WorkSocket = socket;
    }

    public async Task Send(string str)
    {
        var sendBytes = Encoding.UTF8.GetBytes(str);
        var buff = ByteBuffer.Allocate(4 + sendBytes.Length);
        buff.WriteInt(sendBytes.Length);
        buff.WriteBytes(sendBytes, 0, sendBytes.Length);
        var len = await WorkSocket.Send(buff.ToArray());
    }

    public async Task<string?> Receive()
    {
        var buffer = new byte[1024 * 32];
        try
        {
            while (true)
            {
                if (_buff.Size > 4)
                {
                    var head = _buff.Peek(4);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(head);
                    var packetLen = BitConverter.ToInt32(head);
                    if (packetLen <= _buff.Size)
                    {
                        _buff.Take(4);
                        var s = Encoding.UTF8.GetString(_buff.Take(packetLen));

                        //Console.WriteLine($"[StringOverSocket] {packetLen} {_buff.Size}");
                        return s;
                    }
                }

                var bytesRead = await WorkSocket.Receive(buffer);
                switch (bytesRead)
                {
                    case 0:
                        WorkSocket.InnerSocket.Shutdown(SocketShutdown.Both);
                        WorkSocket.InnerSocket.Close();
                        return null;
                    case > 0:
                        _buff.AddMany(buffer, bytesRead);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{WorkSocket.InnerSocket.RemoteEndPoint}] {ex.Message}\n{ex.StackTrace}");
            WorkSocket.InnerSocket.Shutdown(SocketShutdown.Both);
            WorkSocket.InnerSocket.Close();
            return null;
        }
    }
}