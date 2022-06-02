namespace AimuBot.Data;

[SqliteTable("", ColumnConstraint = "unique ([name], [alias]) on conflict ignore")]
internal class NameAliasRecord
{
    [SqliteColumn("name")] public string Name = "";
    [SqliteColumn("alias")] public string Alias = "";
}

internal class NameAliasSystem
{
    private readonly SqliteDatabase _database;
    private readonly string _tableName;

    public NameAliasSystem(string tableName = "Common")
    {
        _database = new SqliteDatabase("NameAlias.db");
        _tableName = tableName;
        CreateTable();
    }

    public void CreateTable()
    {
        var sql = _database.GetCreateTableSql(typeof(NameAliasRecord), _tableName);

        _database.ExecuteNoneQuery(sql);
    }

    public bool IsNameExist(string name)
    {
        var (succ, _) = _database.GetObject<NameAliasRecord>(
            "name = $name",
            new Dictionary<string, object> { { "$name", name } },
            _tableName);
        return succ;
    }

    public int SaveNameAlias(string name, string alias)
    {
        var r = new NameAliasRecord
        {
            Name = name,
            Alias = alias
        };
        return _database.SaveObject(r, _tableName);
    }

    public string TryGetNameByAlias(string alias)
    {
        var name = GetNameByAlias(alias);
        return name ?? alias;
    }

    public string? GetNameByAlias(string alias)
    {
        var (succ, na) = _database.GetObject<NameAliasRecord>(
            "alias = $alias",
            new Dictionary<string, object> { { "$alias", alias } },
            _tableName);

        return succ ? na.Name : null;
    }

    public string[]? GetAlias(string name)
    {
        var objs = _database.GetObjects<NameAliasRecord>(
            "name = $name",
            new Dictionary<string, object> { { "$name", name } },
            _tableName);

        return objs is null ? null : objs.Select(x => x.Alias).ToArray();
    }
}