using System.Reflection;
using System.Text;

using AimuBot.Core;
using AimuBot.Core.Config;
using AimuBot.Core.Extensions;
using AimuBot.Core.Message;
using AimuBot.Core.ModuleMgr;

namespace AimuBot.Modules;

[Module("帮助模块",
    Version = "1.0.0",
    Description = "帮助模块")]
public class Help : ModuleBase
{
    [Command("help",
        Name = "获取帮助",
        Template = "/help",
        Matching = Matching.StartsWith,
        SendType = SendType.Reply)]
    public MessageChain OnHelp(BotMessage msg)
    {
        return $"[AimuBot] {Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)}\n请访问 https://docs-aimubot.amu.moe 来获取帮助信息";
    }
    
    [Command("doc export",
        Name = "帮助文档自动导出",
        Template = "/doc export",
        Level = RbacLevel.Super,
        Matching = Matching.Exact,
        SendType = SendType.Reply)]
    public async Task<MessageChain> OnDocExport(BotMessage msg)
    {
        DirectoryInfo di = new(Bot.Config.ResourcePath.CombinePath("Doc"));
        if (!di.Exists)
            di.Create();

        var cmdCount = 0;
        var moduleCount = 0;
        var pageStr = "\n";
        foreach (var m in Bot.Instance.ModuleMgr.ModuleList)
        {
            var attr = m.GetType().GetCustomAttribute<ModuleAttribute>();
            if (attr is null)
                continue;

            pageStr += $"                        '/modules/{m.GetType().Name}',\n";

            var filePath = Bot.Config.ResourcePath.CombinePath($"Doc/docs/modules/{m.GetType().Name}.md");
            StringBuilder sb = new();

            sb.AppendLine($"# {attr.Name}");
            sb.AppendLine();
            //sb.AppendLine($"> 版本: {attr.Version}");
            //sb.AppendLine();
            sb.AppendLine(attr.Description.Replace("\n", "\n\n"));
            sb.AppendLine();

            if (attr.Eula.IsNotEmpty())
            {
                sb.AppendLine("::: tip 最终用户许可协议");
                sb.AppendLine();
                sb.AppendLine(attr.Eula.Replace("\n", "\n\n"));
                sb.AppendLine();
                sb.AppendLine(":::");
                sb.AppendLine();
            }

            if (attr.Privacy.IsNotEmpty())
            {
                sb.AppendLine("::: tip 隐私政策");
                sb.AppendLine();
                sb.AppendLine(attr.Privacy.Replace("\n", "\n\n"));
                sb.AppendLine();
                sb.AppendLine(":::");
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
                if (attr.CommandDesc.IsNotEmpty())
                {
                    sb.AppendLine(attr.CommandDesc.Replace("\n", "\n\n"));
                    sb.AppendLine();
                }
            }

            var t = m.GetType();
            var methods = t.GetMethods();
            var cmdAttrs = new List<CommandAttribute>();

            foreach (var method in methods)
            {
                if (method.ReturnType == typeof(void))
                    continue;

                var cmdInfo = method.GetCustomAttributes(typeof(CommandAttribute), false);

                foreach (var param in cmdInfo)
                {
                    if (param.GetType() != typeof(CommandAttribute)) continue;

                    if (param is not CommandAttribute cmd) continue;

                    cmdAttrs.Add(cmd);
                }
            }

            cmdAttrs = cmdAttrs.OrderBy(x => x.Name).ToList();

            foreach (var c in cmdAttrs)
            {
                sb.AppendLine();
                sb.Append("## ");
                sb.Append(c.Name);
                if (c.Level < RbacLevel.Normal)
                {
                    var levelStr = c.Level switch
                    {
                        RbacLevel.Super => "开发者",
                        RbacLevel.Owner => "群主",
                        RbacLevel.Admin => "管理员/群主",
                        _               => ""
                    };
                    sb.Append($" <Badge type=\"tip\" text=\"{levelStr}\" vertical=\"top\" />");
                }

                if (c.State is not State.Normal)
                {
                    var (typeStr, stateStr) = c.State switch
                    {
                        State.Test             => ("warning", "测试中"),
                        State.Developing       => ("warning", "开发中"),
                        State.DisableByDefault => ("warning", "默认关闭"),
                        State.Disabled         => ("danger", "关闭"),
                        _                      => ("", "")
                    };
                    sb.Append($" <Badge type=\"{typeStr}\" text=\"{stateStr}\" vertical=\"top\" />");
                }

                sb.AppendLine();
                sb.AppendLine();
                
                sb.AppendLine(c.Description.Replace("\n", "\n\n"));
                sb.AppendLine();

                foreach (var methodInfo in methods)
                {
                    if (methodInfo.Name != c.DescCustomFunc || methodInfo.ReturnType != typeof(string))
                        continue;

                    var customDesc = methodInfo.Invoke(m, null) as string;
                    sb.AppendLine(customDesc ?? "");
                    sb.AppendLine();
                }

                if (c.BlocksBefore != null)
                    foreach (var block in c.BlocksBefore)
                    {
                        sb.AppendLine(block);
                        sb.AppendLine();
                    }

                if (c.Template != c.Example || c.Example.IsNullOrEmpty())
                {
                    sb.AppendLine("命令格式：");
                    sb.AppendLine();
                    sb.AppendLine("```text");
                    sb.AppendLine(c.Template);
                    sb.AppendLine("```");
                    sb.AppendLine();
                }

                if (c.NekoBoxExample.IsNullOrEmpty() && c.Example.IsNotEmpty())
                {
                    sb.AppendLine("示例：");
                    sb.AppendLine();
                    sb.AppendLine("```text");
                    sb.AppendLine(c.Example);
                    sb.AppendLine("```");
                    sb.AppendLine();
                }

                if (c.NekoBoxExample.IsNotEmpty())
                {
                    sb.AppendLine("示例：");
                    sb.AppendLine();
                    sb.AppendLine("<ClientOnly>\n\t<neko-box :messages=\"[");
                    sb.Append("\t\t");
                    sb.AppendLine(c.NekoBoxExample);
                    sb.AppendLine("]\">\n\t</neko-box>\n</ClientOnly>");
                    sb.AppendLine();
                }

                cmdCount++;
            }

            moduleCount++;
            await File.WriteAllTextAsync(filePath, sb.ToString());
        }

        pageStr += "                        ";
        var configFile = Bot.Config.ResourcePath.CombinePath("Doc/docs/.vuepress/config.ts");
        var s = File.ReadAllText(configFile);
        s = s.Replace(s.GetSandwichedText("// <auto-generated>", "// </auto-generated>"), pageStr);
        await File.WriteAllTextAsync(configFile, s);

        //return $"[DocExporter]\nExport {cmdCount} commands in {moduleCount} categories.";
        return "";
    }
}