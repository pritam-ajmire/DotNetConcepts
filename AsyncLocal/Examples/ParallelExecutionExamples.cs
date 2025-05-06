using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncLocal.Examples;

/// <summary>
/// Demonstrates AsyncLocal behavior with parallel execution
/// </summary>
public static class ParallelExecutionExamples
{
    // AsyncLocal instance to store a request ID
    private static readonly AsyncLocal<string> RequestId = new();
    
    public static async Task RunAsync()
    {
        // Example 1: AsyncLocal with Parallel.ForEach
        await ParallelForEachExample();
        
        // Example 2: AsyncLocal with Task.WhenAll
        await TaskWhenAllExample();
        
        // Example 3: AsyncLocal with Parallel LINQ
        await ParallelLinqExample();
    }
    
    private static async Task ParallelForEachExample()
    {
        Console.WriteLine("\n--- AsyncLocal with Parallel.ForEach ---");
        
        RequestId.Value = "Main-Request";
        Console.WriteLine($"Main Thread: {RequestId.Value}");
        
        var items = Enumerable.Range(1, 5).ToList();
        
        // Using Task.Run because Parallel.ForEach is synchronous
        await Task.Run(() =>
        {
            Console.WriteLine($"Before Parallel.ForEach: {RequestId.Value}");
            
            Parallel.ForEach(items, item =>
            {
                // Each parallel iteration inherits the AsyncLocal value
                Console.WriteLine($"Parallel item {item} initial: {RequestId.Value}, Thread ID: {Thread.CurrentThread.ManagedThreadId}");
                
                // Set a unique value for this iteration
                RequestId.Value = $"Request-{item}";
                Console.WriteLine($"Parallel item {item} after setting: {RequestId.Value}, Thread ID: {Thread.CurrentThread.ManagedThreadId}");
                
                // Simulate some work
                Thread.Sleep(10);
                
                // The value is preserved within this iteration
                Console.WriteLine($"Parallel item {item} after work: {RequestId.Value}, Thread ID: {Thread.CurrentThread.ManagedThreadId}");
            });
            
            // The original value is preserved after Parallel.ForEach
            Console.WriteLine($"After Parallel.ForEach: {RequestId.Value}");
        });
        
        // The main thread's value is unaffected
        Console.WriteLine($"Main Thread After Parallel.ForEach: {RequestId.Value}");
    }
    
    private static async Task TaskWhenAllExample()
    {
        Console.WriteLine("\n--- AsyncLocal with Task.WhenAll ---");
        
        RequestId.Value = "Main-Request";
        Console.WriteLine($"Main Thread: {RequestId.Value}");
        
        // Create multiple tasks
        var tasks = new List<Task>();
        for (int i = 1; i <= 5; i++)
        {
            int taskId = i;
            tasks.Add(Task.Run(async () =>
            {
                // Each task inherits the AsyncLocal value
                Console.WriteLine($"Task {taskId} initial: {RequestId.Value}, Thread ID: {Thread.CurrentThread.ManagedThreadId}");
                
                // Set a unique value for this task
                RequestId.Value = $"Task-{taskId}";
                Console.WriteLine($"Task {taskId} after setting: {RequestId.Value}, Thread ID: {Thread.CurrentThread.ManagedThreadId}");
                
                // Simulate async work
                await Task.Delay(10);
                
                // The value is preserved after the await
                Console.WriteLine($"Task {taskId} after await: {RequestId.Value}, Thread ID: {Thread.CurrentThread.ManagedThreadId}");
            }));
        }
        
        // Wait for all tasks to complete
        await Task.WhenAll(tasks);
        
        // The main thread's value is unaffected
        Console.WriteLine($"Main Thread After Task.WhenAll: {RequestId.Value}");
    }
    
    private static async Task ParallelLinqExample()
    {
        Console.WriteLine("\n--- AsyncLocal with Parallel LINQ ---");
        
        RequestId.Value = "Main-Request";
        Console.WriteLine($"Main Thread: {RequestId.Value}");
        
        var items = Enumerable.Range(1, 5).ToList();
        
        // Using Task.Run because PLINQ is synchronous
        await Task.Run(() =>
        {
            Console.WriteLine($"Before PLINQ: {RequestId.Value}");
            
            var results = items.AsParallel().Select(item =>
            {
                // Each parallel operation inherits the AsyncLocal value
                Console.WriteLine($"PLINQ item {item} initial: {RequestId.Value}, Thread ID: {Thread.CurrentThread.ManagedThreadId}");
                
                // Set a unique value for this operation
                RequestId.Value = $"PLINQ-{item}";
                Console.WriteLine($"PLINQ item {item} after setting: {RequestId.Value}, Thread ID: {Thread.CurrentThread.ManagedThreadId}");
                
                // Simulate some work
                Thread.Sleep(10);
                
                // The value is preserved within this operation
                Console.WriteLine($"PLINQ item {item} after work: {RequestId.Value}, Thread ID: {Thread.CurrentThread.ManagedThreadId}");
                
                return item * 2;
            }).ToList();
            
            // The original value is preserved after PLINQ
            Console.WriteLine($"After PLINQ: {RequestId.Value}");
        });
        
        // The main thread's value is unaffected
        Console.WriteLine($"Main Thread After PLINQ: {RequestId.Value}");
    }
}
