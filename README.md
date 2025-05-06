# AsyncLocal in .NET - Comprehensive Guide

This project demonstrates the usage of `AsyncLocal<T>` in .NET, a powerful feature for maintaining context across asynchronous control flows. The examples and tests provide a deep understanding of how AsyncLocal works, its use cases, and best practices.

## Table of Contents

- [What is AsyncLocal?](#what-is-asynclocal)
- [Project Structure](#project-structure)
- [Key Features](#key-features)
- [Examples Overview](#examples-overview)
- [Running the Examples](#running-the-examples)
- [Best Practices](#best-practices)
- [Common Pitfalls](#common-pitfalls)
- [Real-World Use Cases](#real-world-use-cases)
- [Further Reading](#further-reading)

## What is AsyncLocal?

`AsyncLocal<T>` is a class that provides a way to store data that is local to a logical execution context. Unlike thread-local storage (ThreadStatic), which is tied to a specific thread, `AsyncLocal<T>` values flow with the asynchronous control flow, making it ideal for maintaining context in asynchronous applications.

## Project Structure

- **AsyncLocal**: Main project with examples
  - **Examples**: Contains various examples demonstrating AsyncLocal usage
    - BasicUsageExample.cs
    - ContextFlowExamples.cs
    - ThreadPoolExamples.cs
    - ParallelExecutionExamples.cs
    - ValueChangeNotificationExamples.cs
    - PitfallExamples.cs
    - RealWorldScenarios.cs
- **AsyncLocal.Tests**: Unit tests verifying AsyncLocal behavior
  - BasicAsyncLocalTests.cs
  - AdvancedAsyncLocalTests.cs
  - RealWorldScenarioTests.cs

## Key Features

- **Execution Context Flow**: Values flow naturally through `async`/`await` operations
- **Task Isolation**: Each task gets its own copy of the value
- **Thread Independence**: Not tied to a specific thread
- **Value Change Notifications**: Optional callbacks when values change
- **Context Suppression**: Can be suppressed using `ExecutionContext.SuppressFlow()`

## Examples Overview

### 1. Basic Usage Examples
Demonstrates the fundamental behavior of AsyncLocal:
- Simple value flow through async/await
- Value isolation between execution contexts
- Value change notifications

```csharp
// Creating an AsyncLocal instance
private static readonly AsyncLocal<string> AsyncLocalString = new();

// Setting a value
AsyncLocalString.Value = "Initial Value";

// The value flows to async methods
await MethodAsync();

// Each task gets its own copy
await Task.Run(() => {
    AsyncLocalString.Value = "Task-specific Value";
});

// The main thread's value is unaffected
Console.WriteLine(AsyncLocalString.Value); // Still "Initial Value"
```

### 2. Context Flow Examples
Shows how AsyncLocal values flow through different execution contexts:
- AsyncLocal with async/await
- AsyncLocal with Task.Run
- AsyncLocal with ConfigureAwait(false)
- AsyncLocal with ExecutionContext.SuppressFlow
- AsyncLocal with nested contexts

```csharp
// Values flow through ConfigureAwait(false)
CorrelationId.Value = "Request-789";
await Task.Delay(10).ConfigureAwait(false);
Console.WriteLine(CorrelationId.Value); // Still "Request-789"

// Values don't flow when suppressing execution context
using (ExecutionContext.SuppressFlow())
{
    task = Task.Run(() => {
        // Value will be null here
        Console.WriteLine(CorrelationId.Value); // null
    });
}
await task;
```

### 3. Thread Pool Examples
Demonstrates AsyncLocal behavior with thread pool threads:
- AsyncLocal with ThreadPool.QueueUserWorkItem
- AsyncLocal with fire-and-forget tasks
- AsyncLocal with thread reuse

```csharp
// Values flow to thread pool threads
UserId.Value = "User-123";
ThreadPool.QueueUserWorkItem(_ => {
    Console.WriteLine(UserId.Value); // "User-123"
    UserId.Value = "Modified-123"; // Only affects this thread
});
```

### 4. Parallel Execution Examples
Shows AsyncLocal behavior in parallel and concurrent scenarios:
- AsyncLocal with Parallel.ForEach
- AsyncLocal with Task.WhenAll
- AsyncLocal with Parallel LINQ

```csharp
// Each parallel task gets its own copy
RequestId.Value = "Main-Request";
var tasks = Enumerable.Range(1, 5).Select(i => Task.Run(async () => {
    RequestId.Value = $"Task-{i}";
    await Task.Delay(10);
    return RequestId.Value; // Returns "Task-{i}"
}));
await Task.WhenAll(tasks);
```

### 5. Value Change Notification Examples
Demonstrates AsyncLocal value change notifications:
- Direct value changes
- Context flow notifications
- Context suppression notifications

```csharp
// AsyncLocal with value change notification
var asyncLocal = new AsyncLocal<string>(args => {
    Console.WriteLine($"Value changed from '{args.PreviousValue}' to '{args.CurrentValue}'");
    Console.WriteLine($"Due to context flow: {args.ThreadContextChanged}");
});

// Direct change triggers notification with ThreadContextChanged = false
asyncLocal.Value = "New Value";

// Context flow doesn't trigger notification
await Task.Run(() => {
    // Just accessing the value doesn't trigger notification
    string value = asyncLocal.Value;
});
```

### 6. Common Pitfalls Examples
Highlights issues to avoid when using AsyncLocal:
- Using AsyncLocal with mutable reference types
- Forgetting to check for null values
- Incorrect usage with static variables
- Incorrect usage with synchronous continuations

```csharp
// PITFALL: Modifying properties of reference types
var userContext = new UserContext { Name = "John", Role = "User" };
UserContextHolder.Value = userContext;

await Task.Run(() => {
    // This affects all contexts that share this instance!
    UserContextHolder.Value.Role = "Admin";
});

// CORRECT: Create a new instance when modifying
var current = UserContextHolder.Value;
UserContextHolder.Value = new UserContext { 
    Name = current.Name, 
    Role = "Supervisor" 
};
```

### 7. Real-World Scenarios
Demonstrates practical applications of AsyncLocal:
- Request context in web applications
- Logging with correlation IDs
- Ambient transaction scope
- User impersonation

```csharp
// Logging with correlation IDs
public static class Logger {
    private static readonly AsyncLocal<string> _correlationId = new();
    
    public static void SetCorrelationId(string correlationId) {
        _correlationId.Value = correlationId;
    }
    
    public static void Log(string message) {
        var correlationId = _correlationId.Value ?? "no-correlation-id";
        Console.WriteLine($"[{correlationId}] {message}");
    }
}

// Usage
Logger.SetCorrelationId("request-123");
Logger.Log("Starting operation");
await Task.Run(() => {
    Logger.Log("Processing in background"); // Still has "request-123"
});
```

## Running the Examples

To run the examples:
```bash
cd AsyncLocal
dotnet run
```

To run the tests:
```bash
cd AsyncLocal.Tests
dotnet test
```

## Best Practices

1. **Use AsyncLocal for Logical Context**
   - Use AsyncLocal for data that should flow with the logical execution context
   - Prefer AsyncLocal over ThreadStatic for async code

2. **Immutable Values**
   - Use immutable types or value types when possible
   - Create new instances when modifying reference types

3. **Null Checking**
   - Always check for null values when using AsyncLocal
   - Use null-conditional operators or null-coalescing operators

   ```csharp
   string value = asyncLocal.Value ?? "default";
   ```

4. **Proper Cleanup**
   - Clear AsyncLocal values when they're no longer needed
   - Consider using a scope pattern for automatic cleanup

   ```csharp
   using (new AsyncLocalScope<string>(asyncLocal, "value"))
   {
       // Value is set within this scope
   } // Value is restored to previous when disposed
   ```

5. **ExecutionContext Suppression**
   - When using ExecutionContext.SuppressFlow(), dispose the AsyncFlowControl on the same thread
   - Use the correct pattern for suppression with async code:

   ```csharp
   Task task;
   using (ExecutionContext.SuppressFlow())
   {
       task = Task.Run(() => { /* code */ });
   } // Dispose here, on the same thread
   await task; // Await after disposing
   ```

## Common Pitfalls

1. **Mutable Reference Types**
   - Problem: Modifying properties of reference types affects all contexts
   - Solution: Create new instances when modifying reference types

2. **Null Values**
   - Problem: Default value is null, can cause NullReferenceException
   - Solution: Always check for null values

3. **Static Variables**
   - Problem: Storing AsyncLocal values in static variables breaks context isolation
   - Solution: Use AsyncLocal directly, don't store its value in static fields

4. **ExecutionContext Suppression**
   - Problem: AsyncFlowControl must be disposed on the same thread where created
   - Solution: Use the correct pattern shown in Best Practices

5. **Synchronous Continuations**
   - Problem: ContinueWith without proper options can lead to unexpected behavior
   - Solution: Use await or configure continuations properly

## Real-World Use Cases

1. **Request Context in Web Applications**
   - Store request-specific data that flows through the entire request processing pipeline
   - Examples: Request ID, User ID, Tenant ID, Culture information

2. **Logging and Diagnostics**
   - Correlation IDs for distributed tracing
   - Contextual information for logging

3. **Transaction Scopes**
   - Ambient transaction context that flows through async operations
   - Database connection context

4. **Security Context**
   - User identity and impersonation
   - Authorization context

5. **Dependency Injection**
   - Scoped services in asynchronous operations
   - Per-request lifetime in async scenarios

## Further Reading

- [AsyncLocal<T> Class Documentation](https://docs.microsoft.com/en-us/dotnet/api/system.threading.asynclocal-1)
- [ExecutionContext Class Documentation](https://docs.microsoft.com/en-us/dotnet/api/system.threading.executioncontext)
- [Stephen Cleary's Blog on AsyncLocal](https://blog.stephencleary.com/2013/04/implicit-async-context-asynclocal.html)
- [Async Programming: Introduction to Async/Await on ASP.NET](https://learn.microsoft.com/en-us/archive/msdn-magazine/2014/october/async-programming-introduction-to-async-await-on-asp-net)
