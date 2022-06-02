using System.Reflection;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("[AimuBot Server]");
Console.WriteLine("version " + Assembly.GetExecutingAssembly().GetName().Version?.ToString(4));
Console.WriteLine();

await new AimuBot.Core.AimuBot().Start();
