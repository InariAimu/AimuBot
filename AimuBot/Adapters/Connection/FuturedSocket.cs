
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AimuBot.Adapters.Connection;

// From https://github.com/KonataDev/Konata.Core under GNU GPLv3

public class FuturedSocket : IDisposable
{
    /// <summary>
    /// Inner socket
    /// </summary>
    public System.Net.Sockets.Socket InnerSocket { get; }

    /// <summary>
    /// Is Connected
    /// </summary>
    public bool Connected
        => InnerSocket.Connected;

    public FuturedSocket(Socket socket)
        => InnerSocket = socket;

    public FuturedSocket(AddressFamily family, SocketType type, ProtocolType protocol)
        => InnerSocket = new(family, type, protocol);

    public void Dispose()
        => InnerSocket?.Dispose();

    /// <summary>
    /// Turn socket into listen mode and accepts the connections from client
    /// </summary>
    /// <param name="ep"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    public async Task<FuturedSocket> Accept(IPEndPoint ep, int timeout = -1)
    {
        if (!InnerSocket.IsBound)
        {
            InnerSocket.Bind(ep);
            InnerSocket.Listen();
        }

        EnterAsync(out var tk, out var args);
        {
            args.UserToken = tk;
            args.RemoteEndPoint = ep;
            args.Completed += OnCompleted;

            // Accept async
            if (InnerSocket.AcceptAsync(args))
                await Task.Run(() => tk.WaitOne(timeout));
        }
        LeaveAsync(tk, args);

        if (args.AcceptSocket == null) return null;
        if (!args.AcceptSocket.Connected) return null;
        return new(args.AcceptSocket);
    }

    /// <summary>
    /// Connect to server
    /// </summary>
    /// <param name="ep"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    public async Task<bool> Connect(IPEndPoint ep, int timeout = -1)
    {
        EnterAsync(out var tk, out var args);
        {
            args.UserToken = tk;
            args.RemoteEndPoint = ep;
            args.Completed += OnCompleted;

            // Connect async
            if (InnerSocket.ConnectAsync(args))
                await Task.Run(() => tk.WaitOne(timeout));
        }
        LeaveAsync(tk, args);

        return InnerSocket.Connected;
    }

    /// <summary>
    /// Disconnect from server
    /// </summary>
    /// <param name="timeout"></param>
    /// <returns></returns>
    public async Task<bool> Disconnect(int timeout = -1)
    {
        EnterAsync(out var tk, out var args);
        {
            args.UserToken = tk;
            args.Completed += OnCompleted;

            // Disconnect async
            if (InnerSocket.DisconnectAsync(args))
                await Task.Run(() => tk.WaitOne(timeout));
        }
        LeaveAsync(tk, args);

        return InnerSocket.Connected == false;
    }

    /// <summary>
    /// Send data
    /// </summary>
    /// <param name="data"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    public async Task<int> Send(byte[] data, int timeout = -1)
    {
        EnterAsync(out var tk, out var args);
        {
            args.UserToken = tk;
            args.Completed += OnCompleted;
            args.SetBuffer(data);

            // Send async
            if (InnerSocket.SendAsync(args))
                await Task.Run(() => tk.WaitOne(timeout));
        }
        LeaveAsync(tk, args);

        return args.BytesTransferred;
    }

    /// <summary>
    /// Receive data
    /// </summary>
    /// <param name="data"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    public async Task<int> Receive(byte[] data, int timeout = -1)
    {
        EnterAsync(out var tk, out var args);
        {
            args.UserToken = tk;
            args.Completed += OnCompleted;
            args.SetBuffer(data);

            // Receive async
            if (InnerSocket.ReceiveAsync(args))
                await Task.Run(() => tk.WaitOne(timeout));
        }
        LeaveAsync(tk, args);

        return args.BytesTransferred;
    }

    #region Overload methods

    public Task<bool> Connect(string host, int port)
        => Connect(new(IPAddress.Parse(host), port));

    public Task<bool> Connect(IPAddress addr, int port)
        => Connect(new(addr, port));

    public Task<int> Send(string str)
        => Send(Encoding.UTF8.GetBytes(str));

    public Task<int> Send(ReadOnlyMemory<byte> bytes)
        => Send(bytes.ToArray());

    public Task<FuturedSocket> Accept(string ip, ushort port, int timeout = -1)
        => Accept(new(IPAddress.Parse(ip), port), timeout);

    private static void OnCompleted(object s, SocketAsyncEventArgs e)
        => ((AutoResetEvent)e.UserToken)?.Set();

    private static void EnterAsync(out AutoResetEvent tk, out SocketAsyncEventArgs args)
    {
        tk = new AutoResetEvent(false);
        args = new SocketAsyncEventArgs();
    }

    private static void LeaveAsync(AutoResetEvent token, SocketAsyncEventArgs args)
    {
        args?.Dispose();
        token?.Dispose();
    }

    #endregion
}
