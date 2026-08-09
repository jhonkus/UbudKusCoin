using System;
using System.Threading.Tasks;

namespace UbudKusCoin.Services;

/// <summary>
/// Helper to run background tasks safely, capturing and logging exceptions to prevent process crash or silent failures.
/// </summary>
public static class SafeTask
{
    public static void Run(Func<Task> action, string description)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Background task '{description}' failed: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        });
    }

    public static void Run(Action action, string description)
    {
        _ = Task.Run(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Background task '{description}' failed: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        });
    }
}
