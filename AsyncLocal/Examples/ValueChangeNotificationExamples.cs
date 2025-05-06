using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncLocal.Examples;

/// <summary>
/// Demonstrates AsyncLocal value change notifications
/// </summary>
public static class ValueChangeNotificationExamples
{
    // AsyncLocal instance with value change notification
    private static readonly AsyncLocal<string> TraceId = new(OnTraceIdChanged);
    
    public static async Task RunAsync()
    {
        // Example 1: Value change notification with direct value changes
        await DirectValueChangeExample();
        
        // Example 2: Value change notification with context flow
        await ContextFlowNotificationExample();
        
        // Example 3: Value change notification with context suppression
        await ContextSuppressionNotificationExample();
    }
    
    private static async Task DirectValueChangeExample()
    {
        Console.WriteLine("\n--- Value change notification with direct value changes ---");
        
        // Initial value set
        TraceId.Value = "Trace-123";
        Console.WriteLine($"Initial value: {TraceId.Value}");
        
        // Change the value directly
        TraceId.Value = "Trace-456";
        Console.WriteLine($"After direct change: {TraceId.Value}");
        
        // Set to the same value (notification still triggers)
        TraceId.Value = "Trace-456";
        Console.WriteLine($"After setting same value: {TraceId.Value}");
        
        // Set to null
        TraceId.Value = null;
        Console.WriteLine($"After setting to null: {TraceId.Value ?? "null"}");
    }
    
    private static async Task ContextFlowNotificationExample()
    {
        Console.WriteLine("\n--- Value change notification with context flow ---");
        
        TraceId.Value = "Parent-Trace";
        Console.WriteLine($"Parent context: {TraceId.Value}");
        
        await Task.Run(() =>
        {
            // No notification when value flows to a new context
            Console.WriteLine($"Child task inherits: {TraceId.Value}");
            
            // Notification when value is changed in the child context
            TraceId.Value = "Child-Trace";
            Console.WriteLine($"Child context after change: {TraceId.Value}");
        });
        
        // No notification when returning to parent context
        Console.WriteLine($"Parent context after child task: {TraceId.Value}");
    }
    
    private static async Task ContextSuppressionNotificationExample()
    {
        Console.WriteLine("\n--- Value change notification with context suppression ---");
        
        TraceId.Value = "Original-Trace";
        Console.WriteLine($"Original context: {TraceId.Value}");
        
        // Create a separate method to demonstrate suppression without causing thread issues
        await DemonstrateSuppressionWithNotificationAsync();
        
        // No notification when returning to original context
        Console.WriteLine($"Original context after suppression: {TraceId.Value}");
    }
    
    private static async Task DemonstrateSuppressionWithNotificationAsync()
    {
        // Store the current value to demonstrate it's not affected
        string originalValue = TraceId.Value;
        
        // Run a task with suppressed flow
        Task task;
        
        // Suppress the flow only for task creation, not for awaiting
        using (ExecutionContext.SuppressFlow())
        {
            task = Task.Run(() =>
            {
                // The AsyncLocal value will be null here because flow is suppressed
                Console.WriteLine($"Suppressed context initial: {TraceId.Value ?? "null"}");
                
                // Setting a value here will trigger a notification
                TraceId.Value = "Suppressed-Trace";
                Console.WriteLine($"Suppressed context after change: {TraceId.Value}");
            });
        } // AsyncFlowControl is disposed here, on the same thread that created it
        
        // Now await the task
        await task;
    }
    
    private static void OnTraceIdChanged(AsyncLocalValueChangedArgs<string> args)
    {
        Console.WriteLine($"[NOTIFICATION] TraceId changed from '{args.PreviousValue ?? "null"}' to '{args.CurrentValue ?? "null"}'");
        Console.WriteLine($"[NOTIFICATION] Change due to context flow: {args.ThreadContextChanged}");
    }
}
