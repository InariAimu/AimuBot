namespace AimuBot.Modules.DynamicInst
{
    internal class CodeExecuter
    {
        /// <summary>
        /// Run code
        /// </summary>
        /// <returns></returns>
        public static async Task<string?> OnRunPlainCode(string message)
        {
            string? codeTemplate = @"
                namespace DynamicCodeExecution {
                    public static class Runnable {
                        public static object? Run() { 
                            #nullable enable
                                " + $"{message}" + @"
                            #nullable restore
                        }
                    }
                }
            ";

            // Try compile the code
            if (Compiler.TryCompile(codeTemplate,
                    out var context, out var assembly))
            {
                try
                {
                    // Get type
                    var type = assembly!.GetType("DynamicCodeExecution.Runnable");

#nullable enable
                    // Run the code
                    CancellationTokenSource? cancelation = new CancellationTokenSource();
                    cancelation.CancelAfter(new TimeSpan(0, 0, 0, 5));

                    Task<object?>? task = new Task<object?>(() =>
                    {
                        object? result = type!.GetMethod("Run")!.Invoke(null, null);
                        return result;
                    }, cancelation.Token);
#nullable restore
                    try
                    {
                        // Wait for code return
                        task.Start();
                        await task.WaitAsync(cancelation.Token);

                        return task.Result!.ToString();
                    }
                    catch (Exception e)
                    {
                        if (e is TaskCanceledException)
                            return "Timeout while the code execution. (> 5000ms)";
                    }
                }

                // Any exceptions
                catch (Exception)
                {
                    return null;
                }

                // Unload assembly
                finally
                {
                    context!.Unload();

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }

            return null;
        }
    }
}
