using AimuBot.Core.Message;
using AimuBot.Core.Utils;

using Newtonsoft.Json;

namespace AimuBot.Core.Config;

public enum RbacLevel
{
    Super,
    Owner,
    Admin,
    Normal,
    RestrictedGroup,
    RestrictedUser
}

[JsonObject]
public class RbacInfo
{
    public long Owner { get; set; }
    public List<long> RestrictedGroups { get; set; } = new();
    public List<long> RestrictedUsers { get; set; } = new();
}

public class AccessLevelControl
{
    private RbacInfo? _conf;

    public void Init()
    {
        var json = File.ReadAllText(BotUtil.CombinePath(@"RBAC.json"));
        _conf = JsonConvert.DeserializeObject<RbacInfo>(json, new JsonSerializerSettings
        {
            Formatting = Formatting.None
        });
    }

    public void SaveConf()
    {
        var json = JsonConvert.SerializeObject(_conf, new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        });
        File.WriteAllText(BotUtil.CombinePath("RBAC.json"), json);
    }

    public RbacLevel GetGroupMessageLevel(BotMessage desc)
    {
        if (_conf is null)
            return RbacLevel.Normal;

        if (desc.SenderId == _conf.Owner)
            return RbacLevel.Super;

        if (_conf.RestrictedGroups.Contains(desc.SubjectId))
            return RbacLevel.RestrictedGroup;

        if (_conf.RestrictedUsers.Contains(desc.SenderId))
            return RbacLevel.RestrictedUser;

        return desc.Level switch
        {
            GroupLevel.Owner  => RbacLevel.Owner,
            GroupLevel.Admin  => RbacLevel.Admin,
            GroupLevel.Member => RbacLevel.Normal,
            _                 => RbacLevel.Normal
        };
    }
}