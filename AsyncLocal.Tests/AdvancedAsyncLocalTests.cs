using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AsyncLocal.Tests;

public class AdvancedAsyncLocalTests
{
    [Fact]
    public async Task AsyncLocal_ParallelTasks_EachTaskHasItsOwnCopy()
    {
        // Arrange
        var asyncLocal = new AsyncLocal<string>();
        asyncLocal.Value = "original";
        
        // Act
        var tasks = new List<Task<string>>();
        for (int i = 1; i <= 5; i++)
        {
            int taskId = i;
            tasks.Add(Task.Run(async () =>
            {
                // Set a unique value for this task
                asyncLocal.Value = $"Task-{taskId}";
                
                // Simulate some work
                await Task.Delay(10);
                
                // Return the current value
                return asyncLocal.Value;
            }));
        }
        
        var results = await Task.WhenAll(tasks);
        
        // Assert
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal($"Task-{i + 1}", results[i]);
        }
        
        // The original context's value is unchanged
        Assert.Equal("original", asyncLocal.Value);
    }
    
    [Fact]
    public async Task AsyncLocal_NestedTasks_InnerTaskInheritsOuterTaskValue()
    {
        // Arrange
        var asyncLocal = new AsyncLocal<string>();
        asyncLocal.Value = "original";
        
        // Act
        string innerValue = await Task.Run(async () =>
        {
            // Set a value in the outer task
            asyncLocal.Value = "outer";
            
            // Run an inner task
            return await Task.Run(() =>
            {
                // The inner task inherits the value from the outer task
                return asyncLocal.Value;
            });
        });
        
        // Assert
        Assert.Equal("outer", innerValue);
        Assert.Equal("original", asyncLocal.Value);
    }
    
    [Fact]
    public async Task AsyncLocal_NestedTasks_ChangesInInnerTaskDoNotAffectOuterTask()
    {
        // Arrange
        var asyncLocal = new AsyncLocal<string>();
        asyncLocal.Value = "original";
        
        // Act
        string outerValueAfterInnerTask = await Task.Run(async () =>
        {
            // Set a value in the outer task
            asyncLocal.Value = "outer";
            
            // Run an inner task that changes the value
            await Task.Run(() =>
            {
                asyncLocal.Value = "inner";
            });
            
            // Return the value in the outer task after the inner task completes
            return asyncLocal.Value;
        });
        
        // Assert
        Assert.Equal("outer", outerValueAfterInnerTask);
        Assert.Equal("original", asyncLocal.Value);
    }
    
    [Fact]
    public async Task AsyncLocal_ConfigureAwaitFalse_StillFlowsValue()
    {
        // Arrange
        var asyncLocal = new AsyncLocal<string>();
        asyncLocal.Value = "test";
        
        // Act
        string valueAfterConfigureAwait = await GetValueWithConfigureAwaitFalseAsync(asyncLocal);
        
        // Assert
        Assert.Equal("test", valueAfterConfigureAwait);
    }
    
    [Fact]
    public async Task AsyncLocal_MultipleInstances_AreIndependent()
    {
        // Arrange
        var asyncLocal1 = new AsyncLocal<string>();
        var asyncLocal2 = new AsyncLocal<string>();
        
        asyncLocal1.Value = "value1";
        asyncLocal2.Value = "value2";
        
        // Act
        await Task.Run(() =>
        {
            // Change only one of the values
            asyncLocal1.Value = "modified1";
        });
        
        // Assert
        Assert.Equal("value1", asyncLocal1.Value);
        Assert.Equal("value2", asyncLocal2.Value);
    }
    
    [Fact]
    public async Task AsyncLocal_WithCancellation_PreservesValue()
    {
        // Arrange
        var asyncLocal = new AsyncLocal<string>();
        asyncLocal.Value = "test";
        
        var cts = new CancellationTokenSource();
        
        // Act
        string valueBeforeCancellation = null;
        try
        {
            valueBeforeCancellation = await Task.Run(async () =>
            {
                // Capture the value before cancellation
                string value = asyncLocal.Value;
                
                // Cancel the token
                cts.Cancel();
                cts.Token.ThrowIfCancellationRequested();
                
                return value;
            }, cts.Token);
            
            // We shouldn't reach here
            Assert.True(false, "Task should have been canceled");
            return;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        
        // Assert
        Assert.Equal("test", asyncLocal.Value);
    }
    
    [Fact]
    public async Task AsyncLocal_WithException_PreservesValue()
    {
        // Arrange
        var asyncLocal = new AsyncLocal<string>();
        asyncLocal.Value = "test";
        
        // Act
        try
        {
            await Task.Run(() =>
            {
                // Change the value before throwing
                asyncLocal.Value = "modified";
                throw new InvalidOperationException("Test exception");
            });
            
            // We shouldn't reach here
            Assert.True(false, "Task should have thrown an exception");
            return;
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
        
        // Assert
        Assert.Equal("test", asyncLocal.Value);
    }
    
    [Fact]
    public async Task AsyncLocal_WithContinuation_PreservesValue()
    {
        // Arrange
        var asyncLocal = new AsyncLocal<string>();
        asyncLocal.Value = "test";
        
        // Act
        string valueInContinuation = await Task.FromResult(true)
            .ContinueWith(t => asyncLocal.Value);
        
        // Assert
        Assert.Equal("test", valueInContinuation);
    }
    
    [Fact]
    public async Task AsyncLocal_WithTaskDelay_PreservesValue()
    {
        // Arrange
        var asyncLocal = new AsyncLocal<string>();
        asyncLocal.Value = "test";
        
        // Act
        await Task.Delay(100);
        
        // Assert
        Assert.Equal("test", asyncLocal.Value);
    }
    
    private async Task<string> GetValueWithConfigureAwaitFalseAsync(AsyncLocal<string> asyncLocal)
    {
        await Task.Delay(1).ConfigureAwait(false);
        return asyncLocal.Value;
    }
}
