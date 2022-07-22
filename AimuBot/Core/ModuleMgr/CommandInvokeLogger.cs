using System.Reflection;

using AimuBot.Core.Message;
using AimuBot.Data;

namespace AimuBot.Core.ModuleMgr;

public enum EventSource
{
    QQ,
    QQChannel,
    Discord,
    Telegram,
    Web,
}

[SqliteTable("cmd_log")]
internal class CmdInvokeLog
{
    [SqliteColumn("id", AutoField = true, Constraint = "primary key autoincrement")]
    public int Id = 0;
    
    [SqliteColumn("grp_id")] public long GroupId = 0;
    [SqliteColumn("grp_name")] public string GroupName = "";
    [SqliteColumn("user_id")] public long UserId = 0;
    [SqliteColumn("user_name")] public string UserName = "";
    [SqliteColumn("time_show")] public string Time = "";
    
    [SqliteColumn("func")] public string FunctionName = "";
    
    [SqliteColumn("content")] public string Content = "";
    [SqliteColumn("reply")] public string Reply = "";
}

public class CmdInvokeLogDatabase : SqliteDatabase
{
    public CmdInvokeLogDatabase() : base("CmdInvokeLog.db")
    {
        
    }
    
    public void CreateTables()
        => CreateTables(typeof(CmdInvokeLog));
}


public class CommandInvokeLogger
{
    public static CommandInvokeLogger Instance = new();

    private CmdInvokeLogDatabase _db = new();

    public void Init()
    {
        _db.CreateTables();
    }

    public void Log(MethodInfo info, BotMessage msg, MessageChain? reply)
    {
        var entry = new CmdInvokeLog()
        {
            GroupId = msg.SubjectId,
            GroupName = msg.SubjectName,
            UserId = msg.SenderId,
            UserName = msg.SenderName,
            Time = DateTime.Now.ToString("s"),
            FunctionName = info.Name,
            Content = msg.Body,
            Reply = reply?.ToCsCode() ?? "",
        };
        _db.SaveObject(entry);
    }
}