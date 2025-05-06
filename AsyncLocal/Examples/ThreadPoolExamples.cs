using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncLocal.Examples;

/// <summary>
/// Demonstrates AsyncLocal behavior with thread pool threads
/// </summary>
public static class ThreadPoolExamples
{
    // AsyncLocal instance to store a user ID
    private static readonly AsyncLocal<string> UserId = new();
    
    public static async Task RunAsync()
    {
        // Example 1: AsyncLocal with ThreadPool.QueueUserWorkItem
        await ThreadPoolQueueExample();
        
        // Example 2: AsyncLocal with fire-and-forget tasks
        await FireAndForgetExample();
        
        // Example 3: AsyncLocal with thread reuse
        await ThreadReuseExample();
    }
    
    private static async Task ThreadPoolQueueExample()
    {
        Console.WriteLine("\n--- AsyncLocal with ThreadPool.QueueUserWorkItem ---");
        
        UserId.Value = "User-123";
        Console.WriteLine($"Main Thread: {UserId.Value}");
        
        var taskCompletionSource = new TaskCompletionSource<bool>();
        
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                // The AsyncLocal value flows to the thread pool thread
                Console.WriteLine($"ThreadPool Thread: {UserId.Value}");
                
                // Modify the value
                UserId.Value = "Modified-123";
                Console.WriteLine($"ThreadPool Thread After Modification: {UserId.Value}");
                
                taskCompletionSource.SetResult(true);
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetException(ex);
            }
        });
        
        await taskCompletionSource.Task;
        
        // The main thread's value is unaffected
        Console.WriteLine($"Main Thread After ThreadPool: {UserId.Value}");
    }
    
    private static async Task FireAndForgetExample()
    {
        Console.WriteLine("\n--- AsyncLocal with fire-and-forget tasks ---");
        
        UserId.Value = "User-456";
        Console.WriteLine($"Main Thread: {UserId.Value}");
        
        // Start a fire-and-forget task
        var task = Task.Run(() =>
        {
            Console.WriteLine($"Fire-and-forget Task: {UserId.Value}");
            UserId.Value = "Modified-456";
            Console.WriteLine($"Fire-and-forget Task After Modification: {UserId.Value}");
        });
        
        // Wait for the task to complete for demo purposes
        // In a real fire-and-forget scenario, we wouldn't await it
        await task;
        
        // The main thread's value is unaffected
        Console.WriteLine($"Main Thread After Fire-and-forget: {UserId.Value}");
    }
    
    private static async Task ThreadReuseExample()
    {
        Console.WriteLine("\n--- AsyncLocal with thread reuse ---");
        
        // Run multiple sequential tasks that might reuse the same thread
        for (int i = 1; i <= 3; i++)
        {
            UserId.Value = $"User-{i}";
            Console.WriteLine($"Iteration {i} - Main Thread: {UserId.Value}");
            
            await Task.Run(() =>
            {
                Console.WriteLine($"Iteration {i} - Task Thread: {UserId.Value}, Thread ID: {Thread.CurrentThread.ManagedThreadId}");
                
                // Even if the thread is reused, each task gets its own copy of the AsyncLocal value
                UserId.Value = $"Modified-{i}";
                Console.WriteLine($"Iteration {i} - Task Thread After Modification: {UserId.Value}, Thread ID: {Thread.CurrentThread.ManagedThreadId}");
            });
            
            Console.WriteLine($"Iteration {i} - Main Thread After Task: {UserId.Value}");
        }
    }
}
