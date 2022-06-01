namespace AimuBot.Data;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class SqliteColumnAttribute : Attribute
{
    public string SqliteType = "";
    public string Constraint = "";
    public string DefaultValue = "";
    public string NameOverride = "";

    public SqliteColumnAttribute(string name_override = "") => NameOverride = name_override;
}
