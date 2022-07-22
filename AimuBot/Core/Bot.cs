using System.Net.Sockets;

using AimuBot.Adapters;
using AimuBot.Adapters.Connection;
using AimuBot.Core.Config;
using AimuBot.Core.Events;
using AimuBot.Core.Extensions;
using AimuBot.Core.ModuleMgr;
using AimuBot.Core.Utils;

using Newtonsoft.Json;

namespace AimuBot.Core;

public class Bot
{
    public static Bot Instance = new ();
    
    public static readonly BotConfig Config
        = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText(BotConfig.ConfigFileName))!;

    private readonly List<BotAdapter> _botAdapters = new();

    public List<BotAdapter> BotAdapters => _botAdapters;

    private readonly EventDispatcher _dispatcher = new();

    public ModuleMgr.ModuleMgr ModuleMgr { get; } = new();

    public static Random Random { get; } = new();

    public async Task Start()
    {
        Config.AccessLevelControl.Init();

        _dispatcher.InitializeHandlers();

        _dispatcher.OnLog += Dispatcher_OnLog;
        _dispatcher.OnMessage += Dispatcher_OnMessage;

        CommandInvokeLogger.Instance.Init();
        
        ModuleMgr.Init();

        FuturedSocket futuredSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        var index = 0;
        while (true)
        {
            Console.WriteLine("Begin Accept");
            var fs = await futuredSocket.Accept("127.0.0.1", 10616, 4);
            Console.WriteLine($"Accepted {fs.InnerSocket.RemoteEndPoint}");

            var sc = new StringOverSocket(fs);

            var pc = await sc.Receive();
            if (pc.IsNullOrEmpty())
            {
                sc.WorkSocket.Dispose();
                continue;
            }

            CleanAdapters();
            
            Console.WriteLine(pc);
            var initMsg = JsonConvert.DeserializeObject<SocketMessage>(pc);

            Console.WriteLine($"Adapter: {index} is {initMsg?.BotAdapter}");

            switch (initMsg?.BotAdapter.ToLower())
            {
                case "mirai":
                {
                    var adapter = new Mirai()
                    {
                        StringOverSocket = sc,
                        Name = $"mirai-{index}",
                        Status = BotAdapterStatus.Connected,
                    };
                    adapter.OnMessageReceived += Bot_OnMessageReceived;
                    _botAdapters.Add(adapter);

                    Task.Run(adapter.StartReceiveMessage);

                    index++;
                    break;
                }
            }
        }
    }
    
    private void CleanAdapters()
    {
        _botAdapters.RemoveAll(x => x.Status == BotAdapterStatus.Disposed);
    }
    
    private void Bot_OnMessageReceived(BotAdapter sender, MessageEvent args)
    {
        BotLogger.LogV($"Bot [{sender.Name}]", args.Message);
        _dispatcher.RaiseEvent(sender, args);
    }

    private void Dispatcher_OnMessage(BotAdapter sender, MessageEvent args) =>
        ModuleMgr.DispatchGroupMessage(args.Message);

    private void Dispatcher_OnLog(BotAdapter sender, LogEvent args) => BotLogger.LogV(args.Tag, args.EventMessage);

    public void Test() => BotLogger.LogW("Test", "botTest");
}