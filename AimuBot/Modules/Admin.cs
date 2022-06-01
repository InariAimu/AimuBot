
using AimuBot.Core.Bot;
using AimuBot.Core.Message;
using AimuBot.Core.ModuleMgr;

namespace AimuBot.Modules;

[Module(nameof(Admin), Command = "admin")]
internal class Admin : ModuleBase
{
    [Command("msg-switch",
        Matching = Matching.StartsWith,
        Level = RBACLevel.Super)]
    private MessageChain OnMsgSwitch(BotMessage msg)
    {
        Core.AimuBot.Config.EnableSendMessage = msg.Content == "on";
        return $"[Admin] msg-switch {Core.AimuBot.Config.EnableSendMessage}";
    }
}
