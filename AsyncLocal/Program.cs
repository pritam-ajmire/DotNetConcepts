using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AsyncLocal.Examples;

namespace AsyncLocal;

public static class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== AsyncLocal Demo ===\n");
        
        // Basic usage examples
        await BasicUsageExample.RunAsync();
        
        Console.WriteLine("\n=== Context Flow Examples ===");
        
        // Context flow examples
        await ContextFlowExamples.RunAsync();
        
        Console.WriteLine("\n=== Thread Pool Examples ===");
        
        // Thread pool examples
        await ThreadPoolExamples.RunAsync();
        
        Console.WriteLine("\n=== Parallel Execution Examples ===");
        
        // Parallel execution examples
        await ParallelExecutionExamples.RunAsync();
        
        Console.WriteLine("\n=== Value Change Notification Examples ===");
        
        // Value change notification examples
        await ValueChangeNotificationExamples.RunAsync();
        
        Console.WriteLine("\n=== Common Pitfalls Examples ===");
        
        // Common pitfalls examples
        await PitfallExamples.RunAsync();
        
        Console.WriteLine("\n=== Real-world Scenarios ===");
        
        // Real-world scenarios
        await RealWorldScenarios.RunAsync();
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}
