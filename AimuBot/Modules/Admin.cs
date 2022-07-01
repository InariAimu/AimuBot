using AimuBot.Core.Config;
using AimuBot.Core.Message;
using AimuBot.Core.ModuleMgr;

namespace AimuBot.Modules;

[Module(nameof(Admin), Command = "admin")]
internal class Admin : ModuleBase
{
    [Command("msg-switch",
        Matching = Matching.StartsWith,
        Level = RbacLevel.Super)]
    private MessageChain OnMsgSwitch(BotMessage msg)
    {
        Core.Bot.Config.EnableSendMessage = msg.Content == "on";
        return $"[Admin] msg-switch {Core.Bot.Config.EnableSendMessage}";
    }
}