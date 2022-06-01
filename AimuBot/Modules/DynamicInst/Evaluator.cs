using System.Reflection;

using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Text;

namespace AimuBot.Modules.DynamicInst
{
    // from: https://gist.github.com/TheSnowfield/2c52641d58e73ade1df2447c15f48683

    static class BlackInteractiveMagic
    {
        private static readonly Type? CompilerType =
            Type.GetType("Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScriptCompiler, " +
                         "Microsoft.CodeAnalysis.CSharp.Scripting");

        private static readonly Type? ScriptBuilderType =
            Type.GetType("Microsoft.CodeAnalysis.Scripting.ScriptBuilder, " +
                         "Microsoft.CodeAnalysis.Scripting");

        public static object? Compiler
            => CompilerType!.GetField("Instance")!.GetValue(null);

        public static Script<TType> CreateInitialScript<TType>(object compiler, SourceText sourceText,
            ScriptOptions? optionsOpt, Type? globalsTypeOpt, InteractiveAssemblyLoader? assemblyLoaderOpt)
        {
            // Create the script builder
            object? scriptBuilder = ScriptBuilderType?.GetConstructor(new[] { typeof(InteractiveAssemblyLoader) })
                !.Invoke(new[] { assemblyLoaderOpt ?? new InteractiveAssemblyLoader(null) });

            // Create script instance
            var scriptType = typeof(Script<>).MakeGenericType(typeof(TType));
            var scriptConstr = scriptType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)[0];
            object? scriptInstance = scriptConstr.Invoke(new[]
            {
                compiler,
                scriptBuilder,
                sourceText,
                optionsOpt ?? ScriptOptions.Default,
                globalsTypeOpt, null
            });

            return (Script<TType>)scriptInstance;
        }
    }
}
