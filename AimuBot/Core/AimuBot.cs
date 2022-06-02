using AimuBot.Adapters;
using AimuBot.Core.Bot;
using AimuBot.Core.Events;
using AimuBot.Core.Utils;

using Newtonsoft.Json;

namespace AimuBot.Core;

public class AimuBot
{
    public static readonly BotConfig Config
        = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText(BotConfig.ConfigFileName))!;

    private readonly List<BotAdapter> _bots = new();

    private readonly EventDispatcher _dispatcher = new();

    public ModuleMgr.ModuleMgr ModuleMgr { get; } = new();

    public static Random Random { get; } = new();

    public async Task Start()
    {
        Config.AccessLevelControl.Init();

        _dispatcher.InitializeHandlers();

        _dispatcher.OnLog += Dispatcher_OnLog;
        _dispatcher.OnMessage += Dispatcher_OnMessage;

        ModuleMgr.Bot = this;
        ModuleMgr.Init();

        var miraiBot = new Mirai();
        miraiBot.OnMessageReceived += Bot_OnMessageReceived;
        _bots.Add(miraiBot);

        miraiBot.WaitForConnection();

        await Task.Delay(-1);
    }

    private void Bot_OnMessageReceived(BotAdapter sender, MessageEvent args)
    {
        BotLogger.LogV("Bot", args.Message);
        _dispatcher.RaiseEvent(sender, args);
    }

    private void Dispatcher_OnMessage(BotAdapter sender, MessageEvent args) =>
        ModuleMgr.DispatchGroupMessage(args.Message);

    private void Dispatcher_OnLog(BotAdapter sender, LogEvent args) => BotLogger.LogV(args.Tag, args.EventMessage);

    public void Test() => BotLogger.LogW("Test", "botTest");
}