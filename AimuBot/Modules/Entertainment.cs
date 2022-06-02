using AimuBot.Core.Message;
using AimuBot.Core.ModuleMgr;

namespace AimuBot.Modules;

[Module("Entertain",
    Version = "0.1.0",
    Description = "群娱乐")]
internal class Entertainment : ModuleBase
{
    [Command("pixel",
        Name = "像素绘",
        Description = "所有群进行的像素画",
        Tip = "/pixel <x> <y> <color_hex | color_name>",
        Example = "/pixel 10 10 #1f1e33\n/pixel 0 0 red",
        CooldownType = CooldownType.User,
        CooldownSecond = 3600,
        State = State.Developing,
        SendType = SendType.Send)]
    public MessageChain OnPixel(BotMessage msg) => "";
}