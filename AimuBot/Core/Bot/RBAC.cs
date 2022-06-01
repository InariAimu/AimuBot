using AimuBot.Core.Message;
using AimuBot.Core.Utils;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AimuBot.Core.Bot;

public enum RBACLevel : int
{
    Super, Owner, Admin, Normal, RestrictedGroup, RestrictedUser
}

[JsonObject]
public class RBACInfo
{
    public long Owner { get; set; }
    public List<long> RestrictedGroups { get; set; } = new List<long>();
    public List<long> RestrictedUsers { get; set; } = new List<long>();
}

public class RBAC
{
    private RBACInfo? conf;

    public void Init()
    {
        DefaultContractResolver contractResolver = new DefaultContractResolver()
        {
            NamingStrategy = new KebabCaseNamingStrategy(),
        };
        string? json = File.ReadAllText(BotUtil.CombinePath(@"RBAC.json"));
        conf = JsonConvert.DeserializeObject<RBACInfo>(json, new JsonSerializerSettings()
        {
            ContractResolver = contractResolver,
            Formatting = Formatting.None
        });
    }

    public void SaveConf()
    {
        DefaultContractResolver contractResolver = new DefaultContractResolver()
        {
            NamingStrategy = new KebabCaseNamingStrategy(),
        };
        string? json = JsonConvert.SerializeObject(conf, new JsonSerializerSettings()
        {
            ContractResolver = contractResolver,
            Formatting = Formatting.Indented
        });
        File.WriteAllText(BotUtil.CombinePath("RBAC.json"), json);
    }

    public RBACLevel GetGroupMessageLevel(BotMessage desc)
    {
        if (desc.SenderId == conf.Owner)
            return RBACLevel.Super;

        if (conf.RestrictedGroups.Contains(desc.SubjectId))
            return RBACLevel.RestrictedGroup;

        if (conf.RestrictedUsers.Contains(desc.SenderId))
            return RBACLevel.RestrictedUser;

        return desc.Level switch
        {
            GroupLevel.Owner => RBACLevel.Owner,
            GroupLevel.Admin => RBACLevel.Admin,
            GroupLevel.Member => RBACLevel.Normal,
            _ => RBACLevel.Normal
        };
    }
}
