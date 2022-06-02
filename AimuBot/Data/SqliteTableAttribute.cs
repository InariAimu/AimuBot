namespace AimuBot.Data;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class SqliteTableAttribute : Attribute
{
    public string Name { get; }
    public string ColumnConstraint = "";

    public SqliteTableAttribute(string name)
    {
        Name = name;
    }
}