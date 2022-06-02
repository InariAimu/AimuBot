using AimuBot.Core.Extensions;
using AimuBot.Core.Message;
using AimuBot.Core.ModuleMgr;

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
        await Task.Delay(1000);
        return "Hello, I'm Kagami";
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
