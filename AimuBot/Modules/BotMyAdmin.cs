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
    Description = "数据库操作")]
public class BotMyAdmin : ModuleBase
{
    private string ConnectionString;

    [Command("bma open",
        Name = "Open",
        Tip = "/bma open <db_name>",
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
        Tip = "/bma exec <sql>",
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
        Tip = "/bma q <sql>",
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
