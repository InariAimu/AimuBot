using System.Net;
using System.Net.Sockets;
using System.Text;

// ReSharper disable IdentifierTypo
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ArrangeObjectCreationWhenTypeNotEvident
// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable UnusedType.Global

// From: https://github.com/NekoRushX/NekoRush.FuturedSocket under MIT license

namespace AimuBot.Adapters.Connection;

public class FuturedSocket : IDisposable
{
    /// <summary>
    /// Inner socket
    /// </summary>
    public Socket InnerSocket { get; }

    /// <summary>
    /// Is connected
    /// </summary>
    public bool Connected
        => InnerSocket.Connected;

    /// <summary>
    /// Creates a new instance of <see cref="FuturedSocket"/>
    /// </summary>
    /// <param name="family">Address family</param>
    /// <param name="type">Socket type</param>
    /// <param name="protocol">Protocol type</param>
    public FuturedSocket(AddressFamily family, SocketType type, ProtocolType protocol)
        => InnerSocket = new(family, type, protocol);

    /// <summary>
    /// Create a new instance from legacy <see cref="Socket"/>
    /// </summary>
    /// <param name="socket">Socket</param>
    public FuturedSocket(Socket socket)
        => InnerSocket = socket;

    public void Dispose()
        => InnerSocket?.Dispose();

    /// <summary>
    /// Connect to server
    /// </summary>
    /// <param name="ep">Destination endpoint</param>
    /// <param name="timeout">Connect timeout, -1 for wait infinity</param>
    /// <returns>If connected successfully, return True. Otherwise False.</returns>
    public Task<bool> Connect(IPEndPoint ep, int timeout = -1)
    {
        PrepareTask<bool>(timeout, out var tk, out var args);
        {
            args.UserToken = tk;
            args.RemoteEndPoint = ep;
            args.Completed += (_, o) =>
            {
                tk.TrySetResult(InnerSocket.Connected);
                o.Dispose();
            };

            // Connect async
            return InnerSocket.ConnectAsync(args)
                ? tk.Task
                : Task.FromResult(InnerSocket.Connected);
        }
    }

    /// <summary>
    /// Turn socket into listen mode and waiting for a client connection
    /// </summary>
    /// <param name="ep">Listening endpoint</param>
    /// <param name="maxconn">Max connections</param>
    /// <param name="timeout">Accept timeout, -1 for wait infinity</param>
    /// <returns>If accepts a client, return a connected <see cref="FuturedSocket"/> instance.</returns>
    public Task<FuturedSocket> Accept(IPEndPoint ep, int maxconn, int timeout = -1)
    {
        if (!InnerSocket.IsBound)
        {
            InnerSocket.Bind(ep);
            InnerSocket.Listen(maxconn);
        }

        PrepareTask<FuturedSocket>(timeout, out var tk, out var args);
        {
            args.UserToken = tk;
            args.RemoteEndPoint = ep;
            args.Completed += (_, o) =>
            {
                tk.TrySetResult(o.AcceptSocket is not { Connected: true }
                    ? null
                    : new FuturedSocket(o.AcceptSocket));
                o.Dispose();
            };
        }

        // Accept async
        return InnerSocket.AcceptAsync(args)
            ? tk.Task
            : Task.FromResult(
                args.AcceptSocket is not { Connected: true }
                    ? null
                    : new FuturedSocket(args.AcceptSocket)
            );
    }

    /// <summary>
    /// Disconnect from server
    /// </summary>
    /// <param name="timeout">Disconnect timeout, -1 for wait infinity</param>
    /// <returns>Return socket currently connect status after invoked disconnect function. If disconnected return True.</returns>
    public Task<bool> Disconnect(int timeout = -1)
    {
        PrepareTask<bool>(timeout, out var tk, out var args);
        {
            args.UserToken = tk;
            args.Completed += (_, o) =>
            {
                tk.TrySetResult(InnerSocket.Connected == false);
                o.Dispose();
            };
        }

        // Disconnect async
        return InnerSocket.DisconnectAsync(args)
            ? tk.Task
            : Task.FromResult(InnerSocket.Connected == false);
    }

    /// <summary>
    /// Send data
    /// </summary>
    /// <param name="data">The data to send</param>
    /// <param name="timeout">Send timeout, -1 for wait infinity</param>
    /// <returns>Bytes transferred in <see cref="int"/> value</returns>
    public Task<int> Send(byte[] data, int timeout = -1)
    {
        PrepareTask<int>(timeout, out var tk, out var args);
        {
            args.UserToken = tk;
            args.SetBuffer(data, 0, data.Length);
            args.Completed += (_, o) =>
            {
                tk.TrySetResult(o.BytesTransferred);
                o.Dispose();
            };
        }

        // Send async
        return InnerSocket.SendAsync(args)
            ? tk.Task
            : Task.FromResult(args.BytesTransferred);
    }

    /// <summary>
    /// Receive data
    /// </summary>
    /// <param name="buffer">Receive buffer</param>
    /// <param name="timeout">Receive timeout, -1 for wait infinity</param>
    /// <returns>Bytes received in <see cref="int"/></returns>
    public Task<int> Receive(byte[] buffer, int timeout = -1)
    {
        PrepareTask<int>(timeout, out var tk, out var args);
        {
            args.UserToken = tk;
            args.SetBuffer(buffer, 0, buffer.Length);
            args.Completed += (_, o) =>
            {
                tk.TrySetResult(o.BytesTransferred);
                o.Dispose();
            };
        }

        // Receive async
        return InnerSocket.ReceiveAsync(args)
            ? tk.Task
            : Task.FromResult(args.BytesTransferred);
    }

    #region Overload methods

    /// <summary>
    /// Connect to server
    /// </summary>
    /// <param name="host">Destination string</param>
    /// <param name="port">Destination port</param>
    /// <param name="timeout">Connect timeout, -1 for wait infinity</param>
    /// <returns>If connected successfully, return True. Otherwise False.</returns>
    /// <exception cref="EntryPointNotFoundException"></exception>
    public async Task<bool> Connect(string host, ushort port, int timeout = -1)
    {
        // Try parse ipaddress
        if (IPAddress.TryParse(host, out var ipaddr))
            return await Connect(ipaddr, port, timeout);

        // Get ipaddress through Dns
        var ipList = await Dns.GetHostEntryAsync(host!);
        if (ipList.AddressList.Length <= 0)
            throw new EntryPointNotFoundException("Dns probe returns no ip address.");

        // Connect it
        return await Connect(ipList.AddressList[0], port, timeout);
    }

    /// <summary>
    /// Connect to server
    /// </summary>
    /// <param name="addr">Destination address</param>
    /// <param name="port">Destination port</param>
    /// <param name="timeout">Connect timeout, -1 for wait infinity</param>
    /// <returns>If connected successfully, return True. Otherwise False.</returns>
    public Task<bool> Connect(IPAddress addr, ushort port, int timeout = -1)
        => Connect(new(addr, port), timeout);

    /// <summary>
    /// Turn socket into listen mode and waiting for a client connection
    /// </summary>
    /// <param name="ip">Listening address, "0.0.0.0" for all ports, "127.0.0.1" for local loopback</param>
    /// <param name="port">Listening port</param>
    /// <param name="maxconn">Max connections</param>
    /// <param name="timeout">Accept timeout, -1 for wait infinity</param>
    /// <returns>If accepts a client, return a connected <see cref="FuturedSocket"/> instance.</returns>
    public Task<FuturedSocket> Accept(string ip, ushort port, int maxconn, int timeout = -1)
        => Accept(new(IPAddress.Parse(ip), port), maxconn, timeout);

    /// <summary>
    /// Send data
    /// </summary>
    /// <param name="str">The string data to send</param>
    /// <param name="timeout">Send timeout</param>
    /// <returns>Bytes transferred in <see cref="int"/> value</returns>
    public Task<int> Send(string str, int timeout = -1)
        => Send(Encoding.UTF8.GetBytes(str), timeout);

    private static void PrepareTask<TType>(int timeout,
        out TaskCompletionSource<TType> tk, out SocketAsyncEventArgs args)
    {
        tk = new TaskCompletionSource<TType>(new CancellationTokenSource(timeout).Token);
        args = new SocketAsyncEventArgs();
    }

    #endregion
}