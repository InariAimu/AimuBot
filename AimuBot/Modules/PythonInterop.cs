using System.Diagnostics;
using System.Text;

using AimuBot.Core.Config;
using AimuBot.Core.Extensions;
using AimuBot.Core.Message;
using AimuBot.Core.ModuleMgr;

namespace AimuBot.Modules;

[Module("PythonInterop",
    Command = "",
    Description = "一些Python功能")]
internal class PythonInterop : ModuleBase
{
    [Config("asm_file", DefaultValue = "keystone_module.py")]
    private readonly string _pyAsmFile = null!;

    [Config("dasm_file", DefaultValue = "capstone_module.py")]
    private readonly string _pyDasmFile = null!;

    [Config("python3", DefaultValue = "d:/Software/Anaconda3/python.exe")]
    private readonly string _python3 = null!;

    [Command("py-asm",
        Name = "汇编（arm）",
        Description = "Arm汇编",
        Tip = "/py-asm <asm_code>",
        Example = "/py-asm\nIT AL\nNOP",
        Matching = Matching.StartsWith,
        SendType = SendType.Reply)]
    public MessageChain OnAsm(BotMessage msg)
    {
        Dictionary<string, string> param = new()
        {
            ["-a"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(msg.Content))
        };
        return RunPythonFile(_pyAsmFile, param);
    }

    [Command("py-dasm",
        Name = "反汇编（arm）",
        Description = "Arm反汇编\nT/A:Thumb/Arm\n</>:大小端序",
        Tip = "/py-dasm [T|A][<|>] <base_addr>\n<asm_hexcode>",
        Example = "/py-dasm T<\n1A 0C",
        Matching = Matching.StartsWith,
        Level = RbacLevel.Normal,
        SendType = SendType.Reply)]
    public MessageChain OnDAsm(BotMessage msg)
    {
        var content = msg.Content;
        var s = content.SubstringBefore("\n");
        var orderMode = s.SubstringBefore(" ");
        var address = s.SubstringAfter(" ");

        Dictionary<string, string> param = new()
        {
            ["-b"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(content.SubstringAfter("\n"))),
            ["-a"] = address,
            ["-o"] = orderMode.Contains('<') ? "<" : ">",
            ["-m"] = orderMode.Contains('T') ? "T" : "A"
        };

        return RunPythonFile(_pyDasmFile, param);
    }

    public string RunPythonFile(string pyFile, Dictionary<string, string> param)
    {
        StringBuilder sb = new();
        StringBuilder err = new();

        var sArguments = pyFile + " ";

        foreach (var (k, v) in param) sArguments += $"{k} \"{v}\" ";

        ProcessStartInfo start = new()
        {
            FileName = _python3,
            Arguments = sArguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        Process process = new();
        process.StartInfo = start;
        process.Start();
        while (!process.HasExited)
        {
            if (process.StandardOutput.BaseStream.CanRead)
            {
                var input = process.StandardOutput.ReadToEnd();
                sb.Append(input);
            }

            if (process.StandardError.BaseStream.CanRead)
                err.Append(process.StandardError.ReadToEnd());
        }

        return sb + err.ToString();
    }
}