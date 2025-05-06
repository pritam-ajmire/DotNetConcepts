using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncLocal.Examples;

/// <summary>
/// Demonstrates common pitfalls when using AsyncLocal
/// </summary>
public static class PitfallExamples
{
    // AsyncLocal instance to store a session ID
    private static readonly AsyncLocal<string> SessionId = new();
    
    // AsyncLocal instance to store a mutable object
    private static readonly AsyncLocal<UserContextInfo> UserContextHolder = new();
    
    public static async Task RunAsync()
    {
        // Example 1: Pitfall - Using AsyncLocal with mutable reference types
        await MutableReferenceTypePitfallExample();
        
        // Example 2: Pitfall - Forgetting to check for null
        await NullCheckPitfallExample();
        
        // Example 3: Pitfall - Incorrect usage with static variables
        await StaticVariablePitfallExample();
        
        // Example 4: Pitfall - Incorrect usage with synchronous continuations
        await SynchronousContinuationPitfallExample();
    }
    
    private static async Task MutableReferenceTypePitfallExample()
    {
        Console.WriteLine("\n--- Pitfall: Using AsyncLocal with mutable reference types ---");
        
        // Initialize with a new UserContext
        UserContextHolder.Value = new UserContextInfo { Name = "John", Role = "User" };
        Console.WriteLine($"Main Thread: {UserContextHolder.Value}");
        
        await Task.Run(() =>
        {
            // The same UserContext instance is shared
            Console.WriteLine($"Task Thread Initial: {UserContextHolder.Value}");
            
            // Modifying the properties affects all contexts that share this instance
            UserContextHolder.Value.Role = "Admin";
            Console.WriteLine($"Task Thread After Property Change: {UserContextHolder.Value}");
            
            // The correct way is to create a new instance
            UserContextHolder.Value = new UserContextInfo { Name = UserContextHolder.Value.Name, Role = "Manager" };
            Console.WriteLine($"Task Thread After Instance Change: {UserContextHolder.Value}");
        });
        
        // The property change in the task affects the main thread
        // But the instance change does not
        Console.WriteLine($"Main Thread After Task: {UserContextHolder.Value}");
        
        // Solution: Always create a new instance when modifying
        var currentContext = UserContextHolder.Value;
        UserContextHolder.Value = new UserContextInfo { Name = currentContext.Name, Role = "Supervisor" };
        Console.WriteLine($"Main Thread After Proper Change: {UserContextHolder.Value}");
    }
    
    private static async Task NullCheckPitfallExample()
    {
        Console.WriteLine("\n--- Pitfall: Forgetting to check for null ---");
        
        // Don't set a value initially
        SessionId.Value = null;
        
        await Task.Run(() =>
        {
            // Incorrect: Not checking for null
            try
            {
                string uppercaseId = SessionId.Value.ToUpper();
                Console.WriteLine($"Uppercase ID: {uppercaseId}");
            }
            catch (NullReferenceException)
            {
                Console.WriteLine("Error: NullReferenceException occurred because SessionId.Value is null");
            }
            
            // Correct: Check for null
            string safeUppercaseId = SessionId.Value?.ToUpper() ?? "NO_SESSION";
            Console.WriteLine($"Safe Uppercase ID: {safeUppercaseId}");
            
            // Set a value
            SessionId.Value = "session-123";
        });
        
        // Always check for null when using AsyncLocal values
        string currentSession = SessionId.Value ?? "NO_SESSION";
        Console.WriteLine($"Current Session: {currentSession}");
    }
    
    private static async Task StaticVariablePitfallExample()
    {
        Console.WriteLine("\n--- Pitfall: Incorrect usage with static variables ---");
        
        // Reset the static variable
        StaticContextHolder.ResetContext();
        
        // Set the AsyncLocal value
        SessionId.Value = "static-session-123";
        
        // Incorrect: Storing AsyncLocal value in a static variable
        StaticContextHolder.StoreCurrentContext();
        Console.WriteLine($"Stored context: {StaticContextHolder.StoredSessionId}");
        
        await Task.Run(() =>
        {
            // Change the AsyncLocal value
            SessionId.Value = "static-session-456";
            Console.WriteLine($"AsyncLocal in task: {SessionId.Value}");
            
            // The static variable still has the old value
            Console.WriteLine($"Static variable in task: {StaticContextHolder.StoredSessionId}");
            
            // Update the static variable
            StaticContextHolder.StoreCurrentContext();
            Console.WriteLine($"Updated static variable: {StaticContextHolder.StoredSessionId}");
        });
        
        // The AsyncLocal value is back to the original in the main thread
        Console.WriteLine($"AsyncLocal in main thread: {SessionId.Value}");
        
        // But the static variable has the value from the task
        Console.WriteLine($"Static variable in main thread: {StaticContextHolder.StoredSessionId}");
    }
    
    private static async Task SynchronousContinuationPitfallExample()
    {
        Console.WriteLine("\n--- Pitfall: Incorrect usage with synchronous continuations ---");
        
        SessionId.Value = "sync-session-123";
        Console.WriteLine($"Initial value: {SessionId.Value}");
        
        // Create a completed task
        var completedTask = Task.FromResult(true);
        
        // Incorrect: Using ContinueWith with synchronous execution
        await completedTask.ContinueWith(t =>
        {
            // This might execute synchronously on the same thread
            Console.WriteLine($"ContinueWith initial: {SessionId.Value}");
            
            // Change the value
            SessionId.Value = "sync-session-456";
            Console.WriteLine($"ContinueWith after change: {SessionId.Value}");
        });
        
        // The value might be changed in the main thread if the continuation executed synchronously
        Console.WriteLine($"After ContinueWith: {SessionId.Value}");
        
        // Reset the value
        SessionId.Value = "sync-session-123";
        
        // Correct: Use ConfigureAwait(false) to avoid synchronous execution
        await completedTask.ContinueWith(t =>
        {
            Console.WriteLine($"ConfigureAwait(false) initial: {SessionId.Value}");
            
            // Change the value
            SessionId.Value = "sync-session-789";
            Console.WriteLine($"ConfigureAwait(false) after change: {SessionId.Value}");
        }).ConfigureAwait(false);
        
        // The value is preserved in the main thread
        Console.WriteLine($"After ConfigureAwait(false): {SessionId.Value}");
    }
    
    // Helper class for the mutable reference type example
    private class UserContextInfo
    {
        public string Name { get; set; }
        public string Role { get; set; }
        
        public override string ToString()
        {
            return $"UserContextInfo {{ Name = {Name}, Role = {Role} }}";
        }
    }
    
    // Helper class for the static variable example
    private static class StaticContextHolder
    {
        public static string StoredSessionId { get; private set; }
        
        public static void StoreCurrentContext()
        {
            // This captures the current AsyncLocal value in a static variable
            var asyncLocal = new AsyncLocal<string>();
            StoredSessionId = SessionId.Value;
        }
        
        public static void ResetContext()
        {
            StoredSessionId = null;
        }
    }
}
