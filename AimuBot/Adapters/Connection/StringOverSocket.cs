using System.Net.Sockets;
using System.Text;

using AimuBot.Adapters.Connection.Event;

namespace AimuBot.Adapters.Connection;

public class StringOverSocket
{
    private readonly RingBuffer<byte> _buff = new RingBuffer<byte>(8192);
    public FuturedSocket _workSocket { get; set; }

    public StringOverSocket(FuturedSocket socket)
    {
        _workSocket = socket;
    }

    public async Task Send(string str)
    {
        var sendBytes = Encoding.UTF8.GetBytes(str);
        var buff = ByteBuffer.Allocate(4 + sendBytes.Length);
        buff.WriteInt(sendBytes.Length);
        buff.WriteBytes(sendBytes, 0, sendBytes.Length);
        var len = await _workSocket.Send(buff.ToArray());
    }

    public async Task<string> Receive()
    {
        var buffer = new byte[4096];
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

                var bytesRead = await _workSocket.Receive(buffer);
                if (bytesRead == 0)
                {
                    _workSocket.InnerSocket.Shutdown(SocketShutdown.Both);
                    _workSocket.InnerSocket.Close();
                    return "";
                }
                else if (bytesRead > 0)
                {
                    _buff.AddMany(buffer, bytesRead);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{_workSocket.InnerSocket.RemoteEndPoint}] {ex.Message}\n{ex.StackTrace}");
            _workSocket.InnerSocket.Shutdown(SocketShutdown.Both);
            _workSocket.InnerSocket.Close();
            return "";
        }
    }
}