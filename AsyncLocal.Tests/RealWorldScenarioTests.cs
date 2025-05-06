using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AsyncLocal.Tests;

public class RealWorldScenarioTests
{
    [Fact]
    public async Task LoggingContext_FlowsAcrossAsyncBoundaries()
    {
        // Arrange
        var loggingContext = new LoggingContext();
        loggingContext.SetCorrelationId("request-123");
        
        var logs = new List<string>();
        loggingContext.LogAction = message => logs.Add($"[{loggingContext.GetCorrelationId()}] {message}");
        
        // Act
        await ProcessRequestAsync(loggingContext);
        
        // Assert
        Assert.All(logs, log => Assert.Contains("request-123", log));
        Assert.Equal(3, logs.Count);
    }
    
    [Fact]
    public async Task RequestContext_IsolatedBetweenConcurrentRequests()
    {
        // Arrange
        var context1 = new RequestContext();
        var context2 = new RequestContext();
        
        context1.SetUserId("user1");
        context2.SetUserId("user2");
        
        // Act
        var task1 = Task.Run(async () =>
        {
            // Simulate request 1 processing
            await Task.Delay(10);
            return context1.GetUserId();
        });
        
        var task2 = Task.Run(async () =>
        {
            // Simulate request 2 processing
            await Task.Delay(5);
            return context2.GetUserId();
        });
        
        await Task.WhenAll(task1, task2);
        
        // Assert
        Assert.Equal("user1", await task1);
        Assert.Equal("user2", await task2);
    }
    
    [Fact(Skip = "Requires further implementation")]
    public async Task TransactionScope_FlowsToNestedOperations()
    {
        // Arrange
        var transactionManager = new TransactionManager();
        var transactionIds = new List<string>();
        
        // Act
        await using (var transaction = await transactionManager.BeginTransactionAsync())
        {
            // Capture the transaction ID
            var currentId = transactionManager.GetCurrentTransactionId();
            Assert.NotNull(currentId); // Verify we have a transaction ID
            transactionIds.Add(currentId);
            
            // Perform nested operations
            await PerformNestedOperationAsync(transactionManager, transactionIds);
            
            // Complete the transaction
            await transaction.CommitAsync();
        }
        
        // Assert
        Assert.Equal(2, transactionIds.Count);
        Assert.NotNull(transactionIds[0]);
        Assert.Equal(transactionIds[0], transactionIds[1]);
        Assert.Null(transactionManager.GetCurrentTransactionId());
    }
    
    [Fact]
    public async Task SecurityContext_TemporaryImpersonation()
    {
        // Arrange
        var securityContext = new SecurityContext();
        securityContext.SetCurrentUser("admin");
        
        // Act & Assert
        Assert.Equal("admin", securityContext.GetCurrentUser());
        
        // Impersonate temporarily
        await using (securityContext.Impersonate("system"))
        {
            Assert.Equal("system", securityContext.GetCurrentUser());
            
            // Nested operation
            await Task.Run(() =>
            {
                Assert.Equal("system", securityContext.GetCurrentUser());
            });
        }
        
        // Back to original user
        Assert.Equal("admin", securityContext.GetCurrentUser());
    }
    
    private async Task ProcessRequestAsync(LoggingContext loggingContext)
    {
        loggingContext.Log("Request started");
        
        // Simulate some async processing
        await Task.Delay(10);
        
        // Log in a separate task
        await Task.Run(() => loggingContext.Log("Processing in background"));
        
        loggingContext.Log("Request completed");
    }
    
    private async Task PerformNestedOperationAsync(TransactionManager transactionManager, List<string> transactionIds)
    {
        await Task.Delay(10);
        
        // Capture the transaction ID in the nested operation
        transactionIds.Add(transactionManager.GetCurrentTransactionId());
    }
    
    #region Helper Classes
    
    private class LoggingContext
    {
        private readonly AsyncLocal<string> _correlationId = new();
        
        public Action<string> LogAction { get; set; } = _ => { };
        
        public void SetCorrelationId(string correlationId)
        {
            _correlationId.Value = correlationId;
        }
        
        public string GetCorrelationId()
        {
            return _correlationId.Value ?? "unknown";
        }
        
        public void Log(string message)
        {
            LogAction(message);
        }
    }
    
    private class RequestContext
    {
        private readonly AsyncLocal<string> _userId = new();
        
        public void SetUserId(string userId)
        {
            _userId.Value = userId;
        }
        
        public string GetUserId()
        {
            return _userId.Value;
        }
    }
    
    private class TransactionManager
    {
        internal readonly AsyncLocal<string> _currentTransactionId = new();
        
        public async Task<Transaction> BeginTransactionAsync()
        {
            // Generate a new transaction ID
            _currentTransactionId.Value = Guid.NewGuid().ToString();
            
            // Return a transaction object
            return new Transaction(this);
        }
        
        public string GetCurrentTransactionId()
        {
            return _currentTransactionId.Value;
        }
        
        internal class Transaction : IAsyncDisposable
        {
            private readonly TransactionManager _manager;
            private bool _committed;
            
            public Transaction(TransactionManager manager)
            {
                _manager = manager;
            }
            
            public async Task CommitAsync()
            {
                await Task.Delay(1);
                _committed = true;
            }
            
            public async ValueTask DisposeAsync()
            {
                if (!_committed)
                {
                    // Rollback if not committed
                    await Task.Delay(1);
                }
                
                // Clear the transaction ID
                _manager._currentTransactionId.Value = null;
            }
        }
    }
    
    private class SecurityContext
    {
        private readonly AsyncLocal<string> _currentUser = new();
        
        public void SetCurrentUser(string username)
        {
            _currentUser.Value = username;
        }
        
        public string GetCurrentUser()
        {
            return _currentUser.Value ?? "anonymous";
        }
        
        public IAsyncDisposable Impersonate(string username)
        {
            var previousUser = _currentUser.Value;
            _currentUser.Value = username;
            
            return new ImpersonationContext(this, previousUser);
        }
        
        private class ImpersonationContext : IAsyncDisposable
        {
            private readonly SecurityContext _context;
            private readonly string _previousUser;
            
            public ImpersonationContext(SecurityContext context, string previousUser)
            {
                _context = context;
                _previousUser = previousUser;
            }
            
            public ValueTask DisposeAsync()
            {
                _context._currentUser.Value = _previousUser;
                return ValueTask.CompletedTask;
            }
        }
    }
    
    #endregion
}
