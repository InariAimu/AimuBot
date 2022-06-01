using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;

namespace AimuBot.Modules.DynamicInst
{
    public class SandBox
    {
        private readonly object? _csharpCompiler;
        private ScriptState<object>? _globalState;

        public SandBox()
        {
            // Create compiler
            _csharpCompiler = BlackInteractiveMagic.Compiler;

            if (_csharpCompiler is not null)
            {
                // Initialize script context
                var script = BlackInteractiveMagic.CreateInitialScript<object>(_csharpCompiler,
                    SourceText.From(string.Empty), null, null, null);
                {
                    // Create an empty state
                    _globalState = script.RunAsync
                        (null, _ => true, default).GetAwaiter().GetResult();
                }
            }
        }

        public async Task<object?> RunAsync(string code, CancellationToken token = default)
        {
            if (_globalState is null)
                return null;

            // Append the code to the last session
            var newScript = _globalState.Script
                .ContinueWith(code, ScriptOptions.Default);

            // Diagnostics
            var diagnostics = newScript.Compile(token);
            foreach (var item in diagnostics)
            {
                if (item.Severity > DiagnosticSeverity.Error) return null;
            }

            // Execute the code
            _globalState = await newScript
                .RunFromAsync(_globalState, _ => true, token);

            return _globalState.ReturnValue;
        }
    }
}
