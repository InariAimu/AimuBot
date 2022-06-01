﻿using AimuBot.Core.Extensions;
using AimuBot.Core.Message;
using AimuBot.Core.ModuleMgr;
using AimuBot.Modules.DynamicInst;

namespace AimuBot.Modules;

[Module("DynamicInst",
    Version = "0.1.0",
    Description = "动态运行C#")]
internal class AlterInst : ModuleBase
{
    public static Dictionary<string, string> CmdAlias { get; } = new Dictionary<string, string>();

    public override bool OnInit()
    {
        return true;
    }

    public bool OnRunCode(BotMessage msg)
    {
        if (msg.Body.IsNullOrEmpty())
            return false;

        Task.Run(async () =>
        {
            try
            {
                string? s = await CodeExecuter.OnRunPlainCode(msg.Body);
                if (!s.IsNullOrEmpty())
                    await msg.Bot.SendGroupMessageSimple(msg.SubjectId, s);
                else
                    LogMessage("[Compiler] " + Compiler.LastError);
            }
            catch (Exception ex)
            {
                LogMessage(ex);
            }
        });
        return false;
    }

    private bool OnCmdAlias(BotMessage msg, string content)
    {
        string[] s = content.Split(" > ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (s.Length >= 2)
        {
            if (s[0] == s[1])
                return true;

            LogMessage($"cmd-alias {s[0]} > {s[1]}");
            CmdAlias[s[0]] = s[1];
        }
        return true;
    }

    private string _code = "";

    private bool OnCmdCode(BotMessage msg, string content)
    {
        string[] s = content.Split(" > ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (s.Length >= 2)
            _code = s[1];
        return true;
    }

    private bool OnRun(BotMessage msg, string content)
    {
        if (content.IsNullOrEmpty())
            return false;

        Task t = new Task(async () =>
        {
            SandBox? sandbox = new SandBox();
            try
            {
                dynamic? result = await sandbox.RunAsync(content);
                string? sr = result?.ToString();
                if (!sr.IsNullOrEmpty())
                    await msg.Bot.SendGroupMessageSimple(msg.SubjectId, sr);
            }
            catch (Exception ex)
            {
                await msg.Bot.SendGroupMessageSimple(msg.SubjectId, ex.Message);
            }
        });
        t.Start();
        return true;
    }

}
