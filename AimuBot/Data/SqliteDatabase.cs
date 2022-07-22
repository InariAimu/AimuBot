using System.Reflection;
using System.Text;

using AimuBot.Core.Extensions;
using AimuBot.Core.Utils;

using Microsoft.Data.Sqlite;

namespace AimuBot.Data;

public class SqliteDatabase
{
    private static readonly Dictionary<Type, string> TypeMapper = new();
    protected readonly string ConnectionString;

    static SqliteDatabase()
    {
        TypeMapper.Add(typeof(string), "GetString");
        TypeMapper.Add(typeof(int), "GetInt32");
        TypeMapper.Add(typeof(long), "GetInt64");
        TypeMapper.Add(typeof(double), "GetDouble");
        TypeMapper.Add(typeof(float), "GetFloat");
    }

    public SqliteDatabase(string relativeDbFilePath)
    {
        var dbFile = BotUtil.CombinePath(relativeDbFilePath);
        FileInfo file = new(dbFile);
        if (!file.Exists)
            file.Directory?.Create();

        var baseConnectionString = "Data Source=" + BotUtil.CombinePath(dbFile);
        ConnectionString = new SqliteConnectionStringBuilder(baseConnectionString)
        {
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();
    }

    public int SaveObject(object obj, string tableNameOverride = "")
    {
        var t = obj.GetType();
        var tn = t.GetCustomAttribute<SqliteTableAttribute>();
        if (tn == null)
            return -1;

        var tableName = tableNameOverride.IsNullOrEmpty() ? tn.Name : tableNameOverride;

        var members = t.GetFields().Where(y =>
        {
            var attr = y.GetCustomAttribute<SqliteColumnAttribute>();
            return attr is { AutoField: false };
        }).ToList();
        StringBuilder sb = new($"insert into {tableName} (");

        sb.Append(string.Join(',', members.Select(x =>
        {
            var attr = x.GetCustomAttribute<SqliteColumnAttribute>();
            return attr != null && !attr.NameOverride.IsNullOrEmpty() ? attr.NameOverride : x.Name;
        })));

        sb.Append(") values (");
        for (var i = 0; i < members.Count - 1; i++)
        {
            sb.Append('$');
            sb.Append(members[i].Name);
            sb.Append(',');
        }

        sb.Append('$');
        sb.Append(members[^1].Name);
        sb.Append(");");

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = sb.ToString();
        
        //BotLogger.LogI(MethodInfo.GetCurrentMethod().Name, command.CommandText);

        foreach (var t1 in members)
            command.Parameters.AddWithValue($"${t1.Name}", t1.GetValue(obj));

        return command.ExecuteNonQuery();
    }

    public (bool, T?) GetObject<T>(
        string whereClause,
        Dictionary<string, object> paramList,
        string tableNameOverride = "") where T : notnull, new()
    {
        var objects = GetObjects<T>(whereClause, paramList, tableNameOverride);
        return objects.Length == 0 ? (false, default) : (true, objects[0]);
    }

    public T[] GetObjects<T>(
        string whereClause,
        Dictionary<string, object>? paramList = null,
        string tableNameOverride = "") where T : notnull, new()
    {
        List<T> objects = new();

        var template = typeof(T);
        var fields = template.GetFields();
        var tn = template.GetCustomAttribute<SqliteTableAttribute>();
        if (tn == null)
            return objects.ToArray();

        var tableName = tableNameOverride.IsNullOrEmpty() ? tn.Name : tableNameOverride;

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var command = connection.CreateCommand();
        if (whereClause.IsNullOrEmpty())
            command.CommandText = $"select * from {tableName};";
        else if (paramList == null)
            command.CommandText = $"select * from {tableName} {whereClause};";
        else
            command.CommandText = $"select * from {tableName} where {whereClause};";

        if (paramList != null)
            foreach (var (k, v) in paramList)
                command.Parameters.AddWithValue(k, v);
        
        //BotLogger.LogI(MethodInfo.GetCurrentMethod().Name, command.CommandText);

        using var reader = command.ExecuteReader();
        var methods = typeof(SqliteDataReader).GetMethods().ToList();

        while (reader.Read())
        {
            T obj = new();
            for (var i = 0; i < fields.Length; i++)
            {
                var fi = fields[i];
                var fieldType = fi.FieldType;

                if (!TypeMapper.ContainsKey(fieldType)) continue;

                var mi = methods.Find(x => x.Name == TypeMapper[fieldType]);

                if (mi == null) continue;

                var fv = mi.Invoke(reader, new object[] { i });
                object boxedT = obj;
                fi.SetValue(boxedT, fv);
                obj = (T)boxedT;
            }

            objects.Add(obj);
        }

        return objects.ToArray();
    }

    public int ExecuteNoneQuery(string sql)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteNonQuery();
    }

    public void CreateTables(params Type[] templates)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        foreach (var template in templates)
        {
            var command = connection.CreateCommand();
            command.CommandText = GetCreateTableSql(template);
            command.ExecuteNonQuery();
        }
    }

    public string GetCreateTableSql(Type template, string tableNameOverride = "")
    {
        var tn = template.GetCustomAttribute<SqliteTableAttribute>();
        if (tn == null)
            return "";

        var table = tableNameOverride.IsEmpty() ? tn.Name : tableNameOverride;
        var members = template.GetFields();
        StringBuilder sb = new($"create table if not exists {table} (");
        for (var i = 0; i < members.Length; i++)
        {
            var cfa = members[i].GetCustomAttribute<SqliteColumnAttribute>();
            if (cfa != null && !cfa.NameOverride.IsNullOrEmpty())
                sb.Append(cfa.NameOverride);
            else
                sb.Append(members[i].Name);

            var fieldType = members[i].FieldType;
            var sqlType = "";

            if (cfa != null && cfa.SqliteType != "")
                sqlType = cfa.SqliteType;
            else if (fieldType == typeof(string))
                sqlType = "TEXT";
            else if (fieldType == typeof(double) || fieldType == typeof(float))
                sqlType = "REAL";
            else if (fieldType == typeof(int) || fieldType == typeof(long) || fieldType == typeof(ulong) ||
                     fieldType == typeof(uint))
                sqlType = "INTEGER";

            var constraint = "";

            if (cfa != null && cfa.Constraint != "")
                constraint = cfa.Constraint;

            sb.Append(' ');
            sb.Append(sqlType);

            if (sqlType == "REAL" || sqlType == "INTEGER")
            {
                if (constraint == "")
                {
                    sb.Append(' ');
                    if (cfa != null && cfa.DefaultValue != "")
                    {
                        sb.Append("default ");
                        sb.Append(cfa.DefaultValue);
                    }
                    else
                    {
                        sb.Append("default 0");
                    }
                }
                else
                {
                    sb.Append(' ');
                    sb.Append(constraint);
                }
            }
            else
            {
                if (constraint != "")
                {
                    sb.Append(' ');
                    sb.Append(constraint);
                }
            }

            if (i < members.Length - 1)
                sb.Append(',');
        }

        if (!tn.ColumnConstraint.IsNullOrEmpty())
        {
            sb.Append(',');
            sb.Append(tn.ColumnConstraint);
        }

        sb.Append(");");
        return sb.ToString();
    }
}