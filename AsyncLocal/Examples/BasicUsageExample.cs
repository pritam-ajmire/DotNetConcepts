using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncLocal.Examples;

/// <summary>
/// Demonstrates the basic usage of AsyncLocal
/// </summary>
public static class BasicUsageExample
{
    // AsyncLocal instance to store a string value
    private static readonly AsyncLocal<string> AsyncLocalString = new();
    
    // AsyncLocal instance with value change notification
    private static readonly AsyncLocal<string> AsyncLocalWithNotification = new(OnValueChanged);
    
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Basic AsyncLocal Usage ===");
        
        // Set the value in the current execution context
        AsyncLocalString.Value = "Initial Value";
        Console.WriteLine($"Main Thread: {AsyncLocalString.Value}");
        
        // The value flows to the async method
        await MethodAsync();
        
        // Value remains unchanged after async call
        Console.WriteLine($"Back to Main Thread: {AsyncLocalString.Value}");
        
        // Changing the value only affects the current execution context
        AsyncLocalString.Value = "Updated Value";
        Console.WriteLine($"Main Thread After Update: {AsyncLocalString.Value}");
        
        // Start a new task with a different execution context
        await Task.Run(() =>
        {
            // The value flows to the new task
            Console.WriteLine($"Task Thread Initial: {AsyncLocalString.Value}");
            
            // Change the value in this execution context
            AsyncLocalString.Value = "Task-specific Value";
            Console.WriteLine($"Task Thread After Update: {AsyncLocalString.Value}");
        });
        
        // The main thread's value is unaffected by changes in the task
        Console.WriteLine($"Main Thread After Task: {AsyncLocalString.Value}");
    }
    
    private static async Task MethodAsync()
    {
        // The value flows through the async method
        Console.WriteLine($"Async Method Before Await: {AsyncLocalString.Value}");
        
        await Task.Delay(100);
        
        // The value is preserved after the await
        Console.WriteLine($"Async Method After Await: {AsyncLocalString.Value}");
        
        // Change the value
        AsyncLocalString.Value = "Value Changed in Async Method";
        Console.WriteLine($"Async Method After Change: {AsyncLocalString.Value}");
    }
    
    private static void OnValueChanged(AsyncLocalValueChangedArgs<string> args)
    {
        Console.WriteLine($"Value changed from '{args.PreviousValue}' to '{args.CurrentValue}', ThreadID: {Thread.CurrentThread.ManagedThreadId}");
        Console.WriteLine($"Value change was due to flow suppression: {args.ThreadContextChanged}");
    }
}
