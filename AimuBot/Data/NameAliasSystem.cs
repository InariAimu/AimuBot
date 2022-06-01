namespace AimuBot.Data;

[SqliteTable("", ColumnConstraint = "unique ([name], [alias]) on conflict ignore")]
class NameAliasRecord
{
    [SqliteColumn("name")] public string Name = "";
    [SqliteColumn("alias")] public string Alias = "";
}

internal class NameAliasSystem
{
    private readonly SqliteDatabase database;
    private readonly string table_name;

    public NameAliasSystem(string tableName = "Common")
    {
        database = new SqliteDatabase("NameAlias.db");
        table_name = tableName;
        CreateTable();
    }

    public void CreateTable()
    {
        string? sql = database.GetCreateTableSql(typeof(NameAliasRecord), table_name);

        database.ExecuteNoneQuery(sql);
    }

    public bool IsNameExist(string name)
    {
        var (succ, _) = database.GetObject<NameAliasRecord>(
            "name = $name",
            new Dictionary<string, object>() { { "$name", name } },
            table_name);
        return succ;
    }

    public int SaveNameAlias(string name, string alias)
    {
        NameAliasRecord r = new NameAliasRecord()
        {
            Name = name,
            Alias = alias,
        };
        return database.SaveObject(r, table_name);
    }

    public string TryGetNameByAlias(string alias)
    {
        string? name = GetNameByAlias(alias);
        return name ?? alias;
    }

    public string? GetNameByAlias(string alias)
    {
        var (succ, na) = database.GetObject<NameAliasRecord>(
            "alias = $alias",
            new Dictionary<string, object>() { { "$alias", alias } },
            table_name);

        if (succ)
            return na.Name;

        return null;
    }

    public string[]? GetAlias(string name)
    {
        var objs = database.GetObjects<NameAliasRecord>(
            "name = $name",
            new Dictionary<string, object>() { { "$name", name } },
            table_name);

        if (objs is null)
            return null;

        return objs.Select(x => x.Alias).ToArray();
    }
}
