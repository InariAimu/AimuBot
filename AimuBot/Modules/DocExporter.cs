
using System.Reflection;
using System.Text;

using AimuBot.Core;
using AimuBot.Core.Config;
using AimuBot.Core.Extensions;
using AimuBot.Core.Message;
using AimuBot.Core.ModuleMgr;

namespace AimuBot.Modules;


[Module("DocExporter",
    Version = "1.0.0",
    Description = "帮助导出")]
public class DocExporter : ModuleBase
{
    [Command("doc export",
        Name = "Open",
        Tip = "/bma open <db_name>",
        Level = RbacLevel.Super,
        Matching = Matching.Full,
        SendType = SendType.Reply)]
    public async Task<MessageChain> OnDocExport(BotMessage msg)
    {
        DirectoryInfo di = new(Bot.Config.ResourcePath.CombinePath("Doc"));
        if (!di.Exists)
            di.Create();

        var cmdCount = 0;
        var moduleCount = 0;
        foreach (var m in Bot.Instance.ModuleMgr.ModuleList)
        {
            var attr = m.GetType().GetCustomAttribute<ModuleAttribute>();
            if (attr is null)
                continue;

            var filePath = Bot.Config.ResourcePath.CombinePath($"Doc/docs/命令集/{m.GetType().Name}.md");
            StringBuilder sb = new();

            sb.AppendLine($"# {attr.Name}");
            sb.AppendLine();
            sb.AppendLine($"> 版本: {attr.Version}");
            sb.AppendLine();
            sb.AppendLine(attr.Description);
            sb.AppendLine();

            if (attr.Eula.IsNotEmpty())
            {
                sb.AppendLine("!!! info \"最终用户许可协议\"");
                sb.Append("    ");
                sb.AppendLine(attr.Eula.Replace("\n", "\n    "));
                sb.AppendLine();
            }
            
            if (attr.Privacy.IsNotEmpty())
            {
                sb.AppendLine("!!! note \"隐私政策\"");
                sb.Append("    ");
                sb.AppendLine(attr.Privacy.Replace("\n", "\n    "));
                sb.AppendLine();
            }

            if (attr.Command.IsNotEmpty())
            {
                sb.AppendLine("## 主命令");
                sb.AppendLine();
                sb.AppendLine("```text");
                sb.AppendLine("/" + attr.Command);
                sb.AppendLine("```");
                sb.AppendLine();
            }

            foreach (var c in m.CommandList)
            {
                sb.AppendLine("----");
                sb.AppendLine();
                sb.Append("## ");
                sb.AppendLine(c.CommandInfo.Name);
                sb.AppendLine();

                if (c.CommandInfo.Level < RbacLevel.Normal)
                {
                    sb.AppendLine("!!! note \"命令权限\"");
                    sb.Append("    ");
                    sb.AppendLine(c.CommandInfo.Level.ToString());
                    sb.AppendLine();
                }

                sb.AppendLine(c.CommandInfo.Description.Replace("\n", "\n\n"));
                sb.AppendLine();

                if (c.CommandInfo.Tip != c.CommandInfo.Example || c.CommandInfo.Example.IsNullOrEmpty())
                {
                    sb.AppendLine("命令格式：");
                    sb.AppendLine();
                    sb.AppendLine("```text");
                    sb.AppendLine(c.CommandInfo.Tip);
                    sb.AppendLine("```");
                    sb.AppendLine();
                }

                if (c.CommandInfo.Example.IsNotEmpty())
                {
                    sb.AppendLine("示例：");
                    sb.AppendLine();
                    sb.AppendLine("```text");
                    sb.AppendLine(c.CommandInfo.Example);
                    sb.AppendLine("```");
                    sb.AppendLine();
                }

                cmdCount++;
            }

            moduleCount++;
            await File.WriteAllTextAsync(filePath, sb.ToString());
        }

        return $"[DocExporter]\nExport {cmdCount} commands in {moduleCount} categories.";
    }
}