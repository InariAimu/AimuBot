using AimuBot.Core.Config;
using AimuBot.Core.Message;
using AimuBot.Core.ModuleMgr;

namespace AimuBot.Modules;

[Module(nameof(Admin))]
internal class Admin : ModuleBase
{
    [Command("msg-switch",
        Name = "消息开关",
        Template = "/msg-switch <on|off>",
        Description = "Bot 总消息发送开关",
        Matching = Matching.StartsWith,
        Level = RbacLevel.Super)]
    public MessageChain OnMsgSwitch(BotMessage msg)
    {
        Core.Bot.Config.EnableSendMessage = msg.Content == "on";
        return $"[Admin] msg-switch {Core.Bot.Config.EnableSendMessage}";
    }
}