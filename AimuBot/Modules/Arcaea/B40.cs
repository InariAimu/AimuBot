using AimuBot.Core.Message;
using AimuBot.Core.Message.Model;
using AimuBot.Core.ModuleMgr;
using AimuBot.Core.Utils;

#pragma warning disable CA1416
#pragma warning disable CS8602

namespace AimuBot.Modules.Arcaea;

public partial class Arcaea : ModuleBase
{
    [Command("ac b40",
        Name = "查询b40",
        Description = "查询b40（b30的基础上增加9个地板以下的overflow）",
        Tip = "/ac b40",
        Example = "/ac b40",
        Category = "Arcaea",
        CooldownType = CooldownType.User,
        CooldownSecond = 30,
        Matching = Matching.Full,
        SendType = SendType.Reply)]
    public async Task<MessageChain> OnB40(BotMessage msg)
    {
        var bindInfo = GetArcId(msg.SenderId);

        if (bindInfo == null)
            return "未绑定或id错误\n请使用/ac bind [arcaea数字id] 进行绑定";

        var arcIdOrName = bindInfo.BindType == 1 ? bindInfo.Name : bindInfo.ArcId;

        var response = await GetB40ResponseFromAua(arcIdOrName);

        bindInfo.ArcId = response.Content.AccountInfo.Code;
        bindInfo.Name = response.Content.AccountInfo.Name;
        _db.SaveObject(bindInfo);

        var accountInfo = response.Content.AccountInfo;

        foreach (var playRecord in response.Content.Best30List) UpdatePlayerScoreRecord(accountInfo, playRecord);
        foreach (var playRecord in response.Content.Best30Overflow) UpdatePlayerScoreRecord(accountInfo, playRecord);

        PttHistoryDesc pttHistoryDesc = new()
        {
            ArcId = accountInfo.Code,
            Ptt = accountInfo.RealRating,
            B30 = response.Content.Best30Avg,
            R10 = response.Content.Recent10Avg,
            Time = (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds,
            Type = 0
        };
        _db.SaveObject(pttHistoryDesc);

        try
        {
            var succ = GetB30ImageFile(response, BotUtil.CombinePath("Arcaea/tmp_b40.jpg"), bindInfo.B30Type, 9);
            if (succ) return new MessageBuilder(ImageChain.Create("Arcaea/tmp_b40.jpg")).Build();
        }
        catch (Exception ex)
        {
            return $"error: {ex.Message}";
        }

        return "";
    }
}