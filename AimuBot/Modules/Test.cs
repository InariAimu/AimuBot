using AimuBot.Core.Config;
using AimuBot.Core.Extensions;
using AimuBot.Core.Message;
using AimuBot.Core.ModuleMgr;
using AimuBot.Core.Utils;

namespace AimuBot.Modules;

[Module("Test",
    Version = "1.0.0",
    Description = "测试用")]
internal class Test : ModuleBase
{
    [Command("ping",
        Name = "Ping",
        Matching = Matching.Full,
        SendType = SendType.Send)]
    public async Task<MessageChain> OnPing(BotMessage msg)
    {
        await Task.Delay(BotUtil.Random.Next(100, 1000));
        var names = new[] { "Konata", "Kagami", "Tsukasa", "Miyuki" };
        return "Hello, I'm " + names.Random();
    }

    [Command("test-net",
        Name = "Test network connectivity",
        Matching = Matching.Full,
        Level = RbacLevel.Super,
        SendType = SendType.Send)]
    public MessageChain OnTestNet(BotMessage msg)
    {
        var urls = new[] { "https://www.google.com.hk", "https://github.com/InariAimu/AimuBot.git" };

        var tasks = new List<Task>();
        foreach (var url in urls)
        {
            tasks.Add(url.UrlDownload(false));
            tasks.Add(url.UrlDownload(true));
        }

        var ret = "";

        try
        {
            Task.WaitAll(tasks.ToArray());
        }
        catch (AggregateException ex)
        {
            foreach (var e in ex.InnerExceptions)
            {
                BotLogger.LogW(nameof(OnTestNet), ex.Message);
            }
        }
        finally
        {
            ret = string.Join('\n', tasks.Select(t => $"t{t.Id}: {t.Status}"));
        }

        return ret;
    }

    public override bool OnGroupMessage(BotMessage msg)
    {
        var content = msg.Body;

        switch (content)
        {
            case "/cs test":
                msg.Bot.SendGroupMessageSimple(msg.SubjectId, "测试：猫");
                return true;
            case "/cs reply":
                msg.Bot.ReplyGroupMessageText(msg.SubjectId, msg.Id, "测试：猫");
                return true;
            case "/cs image":
                msg.Bot.ReplyGroupMessageImage(msg.SubjectId, msg.Id, "表情包/Arcaea/37.jpg");
                return true;
            default:
            {
                if (content.StartsWith("/cs format"))
                {
                    var cmd = content[11..].UnEscapeMiraiCode();
                    msg.Bot.SendRawMessage(cmd);
                    return true;
                }

                break;
            }
        }

        return false;
    }
}