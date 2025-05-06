using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncLocal.Examples;

/// <summary>
/// Demonstrates how AsyncLocal values flow through different execution contexts
/// </summary>
public static class ContextFlowExamples
{
    // AsyncLocal instance to store a correlation ID
    private static readonly AsyncLocal<string> CorrelationId = new();
    
    public static async Task RunAsync()
    {
        // Example 1: AsyncLocal flows through async/await
        await AsyncAwaitFlowExample();
        
        // Example 2: AsyncLocal flows through Task.Run
        await TaskRunFlowExample();
        
        // Example 3: AsyncLocal with ConfigureAwait(false)
        await ConfigureAwaitExample();
        
        // Example 4: AsyncLocal with ExecutionContext.SuppressFlow
        await ExecutionContextSuppressionExample();
        
        // Example 5: AsyncLocal with nested contexts
        await NestedContextsExample();
    }
    
    private static async Task AsyncAwaitFlowExample()
    {
        Console.WriteLine("\n--- AsyncLocal with async/await ---");
        
        CorrelationId.Value = "Request-123";
        Console.WriteLine($"Main Thread: {CorrelationId.Value}");
        
        await Task.Delay(10);
        Console.WriteLine($"After await: {CorrelationId.Value}");
        
        await AsyncMethod();
        Console.WriteLine($"After AsyncMethod: {CorrelationId.Value}");
    }
    
    private static async Task AsyncMethod()
    {
        Console.WriteLine($"In AsyncMethod before await: {CorrelationId.Value}");
        await Task.Delay(10);
        Console.WriteLine($"In AsyncMethod after await: {CorrelationId.Value}");
    }
    
    private static async Task TaskRunFlowExample()
    {
        Console.WriteLine("\n--- AsyncLocal with Task.Run ---");
        
        CorrelationId.Value = "Request-456";
        Console.WriteLine($"Main Thread: {CorrelationId.Value}");
        
        await Task.Run(() =>
        {
            Console.WriteLine($"Inside Task.Run: {CorrelationId.Value}");
            CorrelationId.Value = "Modified-456";
            Console.WriteLine($"After modification in Task.Run: {CorrelationId.Value}");
        });
        
        Console.WriteLine($"After Task.Run: {CorrelationId.Value}");
    }
    
    private static async Task ConfigureAwaitExample()
    {
        Console.WriteLine("\n--- AsyncLocal with ConfigureAwait(false) ---");
        
        CorrelationId.Value = "Request-789";
        Console.WriteLine($"Main Thread: {CorrelationId.Value}");
        
        await Task.Delay(10).ConfigureAwait(false);
        Console.WriteLine($"After ConfigureAwait(false): {CorrelationId.Value}");
        
        // Even with ConfigureAwait(false), AsyncLocal values still flow
        await Task.Run(async () =>
        {
            Console.WriteLine($"In Task.Run before ConfigureAwait(false): {CorrelationId.Value}");
            await Task.Delay(10).ConfigureAwait(false);
            Console.WriteLine($"In Task.Run after ConfigureAwait(false): {CorrelationId.Value}");
        });
    }
    
    private static async Task ExecutionContextSuppressionExample()
    {
        Console.WriteLine("\n--- AsyncLocal with ExecutionContext.SuppressFlow ---");
        
        CorrelationId.Value = "Request-ABC";
        Console.WriteLine($"Main Thread: {CorrelationId.Value}");
        
        // Create a separate method to demonstrate suppression without causing thread issues
        await DemonstrateSuppressionAsync();
        
        // The original value is preserved
        Console.WriteLine($"After suppressed flow: {CorrelationId.Value}");
    }
    
    private static async Task DemonstrateSuppressionAsync()
    {
        // Store the current value to demonstrate it's not affected
        string originalValue = CorrelationId.Value;
        
        // Run a task with suppressed flow
        Task task;
        
        // Suppress the flow only for task creation, not for awaiting
        using (ExecutionContext.SuppressFlow())
        {
            task = Task.Run(() =>
            {
                // The AsyncLocal value will be null here because flow is suppressed
                Console.WriteLine($"Inside Task.Run with suppressed flow: {CorrelationId.Value ?? "null"}");
                
                // Setting a value here won't affect the parent context
                CorrelationId.Value = "Modified-ABC";
                Console.WriteLine($"After modification with suppressed flow: {CorrelationId.Value}");
            });
        } // AsyncFlowControl is disposed here, on the same thread that created it
        
        // Now await the task
        await task;
        
        // The original value is preserved
        Console.WriteLine($"After suppressed flow: {CorrelationId.Value}");
    }
    
    private static async Task NestedContextsExample()
    {
        Console.WriteLine("\n--- AsyncLocal with nested contexts ---");
        
        CorrelationId.Value = "Parent-Context";
        Console.WriteLine($"Parent context: {CorrelationId.Value}");
        
        await Task.Run(async () =>
        {
            Console.WriteLine($"Child task inherits: {CorrelationId.Value}");
            
            // Modify the value in the child context
            CorrelationId.Value = "Child-Context";
            Console.WriteLine($"Child context modified: {CorrelationId.Value}");
            
            await Task.Run(() =>
            {
                Console.WriteLine($"Nested task inherits: {CorrelationId.Value}");
                
                // Modify the value in the nested context
                CorrelationId.Value = "Nested-Context";
                Console.WriteLine($"Nested context modified: {CorrelationId.Value}");
            });
            
            // The nested task's changes don't affect this level
            Console.WriteLine($"Child context after nested task: {CorrelationId.Value}");
        });
        
        // The child task's changes don't affect the parent
        Console.WriteLine($"Parent context after child task: {CorrelationId.Value}");
    }
}
