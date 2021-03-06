using System.Text;

using AimuBot.Core.Config;
using AimuBot.Core.Extensions;
using AimuBot.Core.Message;
using AimuBot.Core.ModuleMgr;
using AimuBot.Core.Utils;

using Microsoft.Data.Sqlite;

namespace AimuBot.Modules;

[Module("BotMyAdmin",
    Version = "1.0.0",
    Description = "BotMyAdmin 数据库管理")]
public class BotMyAdmin : ModuleBase
{
    private string ConnectionString;

    [Command("bma open",
        Name = "Open",
        Template = "/bma open <db_name>",
        Description = "连接数据库",
        Level = RbacLevel.Super,
        Matching = Matching.StartsWith,
        SendType = SendType.Reply)]
    public MessageChain OnOpen(BotMessage msg)
    {
        var baseConnectionString = "Data Source=" + BotUtil.CombinePath(msg.Content);
        ConnectionString = new SqliteConnectionStringBuilder(baseConnectionString)
        {
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();
        return $"[BotMyAdmin] {ConnectionString}";
    }

    [Command("bma exec",
        Name = "Exec",
        Template = "/bma exec <sql>",
        Description = "执行无返回值 sql",
        Level = RbacLevel.Super,
        Matching = Matching.StartsWith,
        SendType = SendType.Reply)]
    public async Task<MessageChain> OnExec(BotMessage msg)
    {
        await using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = msg.Content;

        var result = await command.ExecuteNonQueryAsync();

        await connection.CloseAsync();
        return $"[BotMyAdmin Exec] {result}";
    }

    [Command("bma q",
        Name = "Query",
        Template = "/bma q <sql>",
        Description = "执行查询 sql",
        Level = RbacLevel.Super,
        Matching = Matching.StartsWith,
        SendType = SendType.Reply)]
    public async Task<MessageChain> OnQuery(BotMessage msg)
    {
        await using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = msg.Content;

        var result = await command.ExecuteReaderAsync();

        StringBuilder sb = new();

        //var fieldCount = result.FieldCount;
        //sb.AppendLine(string.Join(' ', result.GetSchemaTable().Columns));

        var rows = 0;
        while (result.Read())
        {
            if (rows > 10)
                continue;
            rows++;

            for (var i = 0; i < result.FieldCount; i++)
            {
                sb.Append(result.GetString(i));
                sb.Append(' ');
            }

            sb.AppendLine();
        }

        await connection.CloseAsync();
        return $"[BotMyAdmin Query]\n{sb.ToString()}";
    }
}