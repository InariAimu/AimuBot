
using System.Reflection;
using System.Runtime.Loader;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AimuBot.Modules.DynamicInst
{
    public static partial class Compiler
    {
        public static string LastError = "";

        public static bool TryCompile(string code,
            out AssemblyLoadContext context, out Assembly assembly)
        {
            // Make syntax tree
            var syntaxTree = CSharpSyntaxTree.ParseText(code);

            // Reference path
            string? assemblyPath = Path.Combine(Path.GetDirectoryName
                (typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location));

            // Code complation
            CSharpCompilation? compilation = CSharpCompilation.Create(null, new[] { syntaxTree },
                options: new CSharpCompilationOptions(
                    usings: new[]
                    {
                    "System",
                    "System.Text",
                    "System.Linq",
                    "System.Drawing",
                    "System.Collections",
                    "System.Numerics",
                    "System.Object",
                    "System.Nullable",
                    },
                    allowUnsafe: true,
                    outputKind: OutputKind.DynamicallyLinkedLibrary
                ),
                references: new[]
                {
                $"{assemblyPath}/System.dll",
                $"{assemblyPath}/System.Drawing.dll",
                $"{assemblyPath}/System.Linq.dll",
                $"{assemblyPath}/System.Runtime.dll",
                $"{assemblyPath}/System.Private.CoreLib.dll",
                $"{assemblyPath}/System.Numerics.dll",
                $"{assemblyPath}/System.Text.Json.dll",
                $"{assemblyPath}/System.Text.Encoding.dll",
                $"{assemblyPath}/System.Text.RegularExpressions.dll",
                }.Select(r => MetadataReference.CreateFromFile(r)).ToArray()
            );
            {
                // Read pe stream
                using (MemoryStream? peStream = new MemoryStream())
                {
                    // Compilation failed
                    var emit = compilation.Emit(peStream);
                    if (!emit.Success)
                    {
                        LastError = emit.Diagnostics.First(x => x.Severity == DiagnosticSeverity.Error).ToString();
                        context = null;
                        assembly = null;
                        return false;
                    }

                    peStream.Seek(0, SeekOrigin.Begin);

                    // Create load context
                    context = new DynamicCodeContext();
                    assembly = context.LoadFromStream(peStream);
                    return true;
                }
            }
        }

        private class DynamicCodeContext : AssemblyLoadContext
        {
            public DynamicCodeContext() : base(isCollectible: true)
            {
            }

            protected override Assembly Load(AssemblyName assemblyName) => null;
        }
    }
}
