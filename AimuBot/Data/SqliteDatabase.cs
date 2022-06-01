using System.Reflection;
using System.Text;

using AimuBot.Core.Extensions;
using AimuBot.Core.Utils;

using Microsoft.Data.Sqlite;

namespace AimuBot.Data;

public class SqliteDatabase
{
    protected readonly string connectionString;

    private static Dictionary<Type, string> _typeMapper = new();

    static SqliteDatabase()
    {
        _typeMapper.Add(typeof(string), "GetString");
        _typeMapper.Add(typeof(int), "GetInt32");
        _typeMapper.Add(typeof(long), "GetInt64");
        _typeMapper.Add(typeof(double), "GetDouble");
        _typeMapper.Add(typeof(float), "GetFloat");
    }

    public SqliteDatabase(string relativeDbFilePath)
    {
        var _dbFile = BotUtil.CombinePath(relativeDbFilePath);
        FileInfo _file = new(_dbFile);
        if (!_file.Exists)
            _file.Directory?.Create();

        string baseConnectionString = $"Data Source=" + BotUtil.CombinePath(_dbFile);
        connectionString = new SqliteConnectionStringBuilder(baseConnectionString)
        {
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();
    }

    public int SaveObject(object obj, string table_name_override = "")
    {
        var t = obj.GetType();
        var tn = t.GetCustomAttribute<SqliteTableAttribute>();
        if (tn == null)
            return -1;

        string tableName = table_name_override.IsNullOrEmpty() ? tn.Name : table_name_override;

        var members = t.GetFields();
        StringBuilder sb = new($"insert into {tableName} (");

        sb.Append(string.Join(',', members.Select(x =>
        {
            var attr = x.GetCustomAttribute<SqliteColumnAttribute>();
            return attr != null && !attr.NameOverride.IsNullOrEmpty() ?
                attr.NameOverride :
                x.Name;
        })));

        sb.Append(") values (");
        for (int i = 0; i < members.Length - 1; i++)
        {
            sb.Append('$');
            sb.Append(members[i].Name);
            sb.Append(',');
        }
        sb.Append('$');
        sb.Append(members[^1].Name);
        sb.Append(");");

        using SqliteConnection? connection = new SqliteConnection(connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = sb.ToString();

        for (int i = 0; i < members.Length; i++)
        {
            command.Parameters.AddWithValue($"${members[i].Name}", members[i].GetValue(obj));
        }
        return command.ExecuteNonQuery();
    }

    public (bool, T?) GetObject<T>(
        string where_clause,
        Dictionary<string, object> param_list,
        string table_name_override = "") where T : notnull, new()
    {
        var objects = GetObjects<T>(where_clause, param_list, table_name_override);
        if (objects.Length == 0)
            return (false, default(T));

        return (true, objects[0]);
    }

    public T[] GetObjects<T>(
        string where_clause,
        Dictionary<string, object>? param_list = null,
        string table_name_override = "") where T : notnull, new()
    {
        List<T> objects = new();

        var template = typeof(T);
        var fields = template.GetFields();
        var tn = template.GetCustomAttribute<SqliteTableAttribute>();
        if (tn == null)
            return objects.ToArray();

        string tableName = table_name_override.IsNullOrEmpty() ? tn.Name : table_name_override;

        using SqliteConnection? connection = new SqliteConnection(connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        if (where_clause.IsNullOrEmpty())
            command.CommandText = $"select * from {tableName};";
        else if (param_list == null)
            command.CommandText = $"select * from {tableName} {where_clause};";
        else
            command.CommandText = $"select * from {tableName} where {where_clause};";

        if (param_list != null)
        {
            foreach (var (k, v) in param_list)
            {
                command.Parameters.AddWithValue(k, v);
            }
        }

        using var reader = command.ExecuteReader();
        List<MethodInfo>? methods = typeof(SqliteDataReader).GetMethods().ToList();

        while (reader.Read())
        {
            T obj = new();
            for (int i = 0; i < fields.Length; i++)
            {
                var fi = fields[i];
                var ftype = fi.FieldType;

                if (_typeMapper.ContainsKey(ftype))
                {
                    var mi = methods.Find(x => x.Name == _typeMapper[ftype]);
                    if (mi != null)
                    {
                        object? fv = mi.Invoke(reader, new object[] { i });
                        object boxedT = obj;
                        fi.SetValue(boxedT, fv);
                        obj = (T)boxedT;
                    }
                }
            }
            objects.Add(obj);
        }
        return objects.ToArray();
    }

    public int ExecuteNoneQuery(string sql)
    {
        using SqliteConnection? connection = new SqliteConnection(connectionString);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteNonQuery();
    }

    public void CreateTables(params Type[] templates)
    {
        using SqliteConnection? connection = new SqliteConnection(connectionString);
        connection.Open();

        int r = 0;

        foreach (var template in templates)
        {
            var command = connection.CreateCommand();
            command.CommandText = GetCreateTableSql(template);
            r = command.ExecuteNonQuery();
        }
    }

    public string GetCreateTableSql(Type template, string table_name_override = "")
    {
        var tn = template.GetCustomAttribute<SqliteTableAttribute>();
        if (tn == null)
            return "";

        string table = table_name_override.IsEmpty() ? tn.Name : table_name_override;
        var members = template.GetFields();
        StringBuilder sb = new($"create table if not exists {table} (");
        for (int i = 0; i < members.Length; i++)
        {
            var cfa = members[i].GetCustomAttribute<SqliteColumnAttribute>();
            if (cfa != null && !cfa.NameOverride.IsNullOrEmpty())
                sb.Append(cfa.NameOverride);
            else
                sb.Append(members[i].Name);

            var ftype = members[i].FieldType;
            string sql_type = "";

            if (cfa != null && cfa.SqliteType != "")
            {
                sql_type = cfa.SqliteType;
            }
            else if (ftype == typeof(string))
            {
                sql_type = "TEXT";
            }
            else if (ftype == typeof(double) || ftype == typeof(float))
            {
                sql_type = "REAL";
            }
            else if (ftype == typeof(int) || ftype == typeof(long) || ftype == typeof(ulong) || ftype == typeof(uint))
            {
                sql_type = "INTEGER";
            }

            string constraint = "";

            if (cfa != null && cfa.Constraint != "")
                constraint = cfa.Constraint;

            sb.Append(' ');
            sb.Append(sql_type);

            if (sql_type == "REAL" || sql_type == "INTEGER")
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

