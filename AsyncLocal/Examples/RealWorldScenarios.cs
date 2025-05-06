using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncLocal.Examples;

/// <summary>
/// Demonstrates real-world scenarios where AsyncLocal is useful
/// </summary>
public static class RealWorldScenarios
{
    public static async Task RunAsync()
    {
        // Example 1: Request context in a web application
        await RequestContextExample();
        
        // Example 2: Logging with correlation IDs
        await LoggingWithCorrelationIdExample();
        
        // Example 3: Ambient transaction scope
        await AmbientTransactionExample();
        
        // Example 4: User impersonation
        await UserImpersonationExample();
    }
    
    private static async Task RequestContextExample()
    {
        Console.WriteLine("\n--- Real-world scenario: Request context in a web application ---");
        
        // Simulate a web request
        await SimulateWebRequest("GET", "/api/products", "user123");
        
        // Simulate another concurrent web request
        await SimulateWebRequest("POST", "/api/orders", "user456");
    }
    
    private static async Task SimulateWebRequest(string method, string path, string userId)
    {
        // Create a request context
        var requestContext = new WebRequestContext
        {
            RequestId = Guid.NewGuid().ToString(),
            Method = method,
            Path = path,
            UserId = userId,
            Timestamp = DateTime.UtcNow
        };
        
        // Set the request context
        WebRequestContext.Current = requestContext;
        
        Console.WriteLine($"Request started: {requestContext.RequestId} - {method} {path}");
        
        // Process the request through multiple layers
        await ProcessRequestAsync();
        
        Console.WriteLine($"Request completed: {requestContext.RequestId} - {method} {path}");
    }
    
    private static async Task ProcessRequestAsync()
    {
        // Access the current request context
        var context = WebRequestContext.Current;
        Console.WriteLine($"Processing request: {context.RequestId} - {context.Method} {context.Path}");
        
        // Simulate calling a service
        await CallServiceAsync();
        
        // The request context is still available
        context = WebRequestContext.Current;
        Console.WriteLine($"Request processing completed: {context.RequestId}");
    }
    
    private static async Task CallServiceAsync()
    {
        // Access the current request context
        var context = WebRequestContext.Current;
        Console.WriteLine($"Service called for request: {context.RequestId} by user: {context.UserId}");
        
        // Simulate some async work
        await Task.Delay(10);
        
        // The request context is still available after the await
        context = WebRequestContext.Current;
        Console.WriteLine($"Service completed for request: {context.RequestId}");
    }
    
    private static async Task LoggingWithCorrelationIdExample()
    {
        Console.WriteLine("\n--- Real-world scenario: Logging with correlation IDs ---");
        
        // Simulate processing a message
        await ProcessMessageAsync("message-123", "Order placed");
        
        // Simulate processing another message
        await ProcessMessageAsync("message-456", "Payment received");
    }
    
    private static async Task ProcessMessageAsync(string messageId, string content)
    {
        // Set the correlation ID for this message processing
        Logger.SetCorrelationId(messageId);
        
        Logger.Log($"Started processing message: {content}");
        
        // Simulate some processing steps
        await ValidateMessageAsync();
        await ProcessMessageContentAsync(content);
        await SaveMessageResultAsync();
        
        Logger.Log("Message processing completed");
    }
    
    private static async Task ValidateMessageAsync()
    {
        Logger.Log("Validating message");
        await Task.Delay(10);
    }
    
    private static async Task ProcessMessageContentAsync(string content)
    {
        Logger.Log($"Processing content: {content}");
        
        // Simulate parallel processing
        await Task.WhenAll(
            Task.Run(() => Logger.Log("Processing part 1")),
            Task.Run(() => Logger.Log("Processing part 2"))
        );
    }
    
    private static async Task SaveMessageResultAsync()
    {
        Logger.Log("Saving message result");
        await Task.Delay(10);
    }
    
    private static async Task AmbientTransactionExample()
    {
        Console.WriteLine("\n--- Real-world scenario: Ambient transaction scope ---");
        
        // Simulate a transaction
        using (var transaction = new TransactionScope())
        {
            Console.WriteLine($"Transaction started: {transaction.Id}");
            
            // Perform some operations within the transaction
            await UpdateCustomerAsync("customer-123", "John Doe");
            await UpdateOrderAsync("order-456", "Shipped");
            
            // Commit the transaction
            transaction.Complete();
            Console.WriteLine($"Transaction committed: {transaction.Id}");
        }
        
        // Start another transaction
        using (var transaction = new TransactionScope())
        {
            Console.WriteLine($"Another transaction started: {transaction.Id}");
            
            // Perform some operations within the transaction
            await UpdateProductAsync("product-789", 99.99m);
            
            // Simulate a failure
            Console.WriteLine("Error occurred, transaction will be rolled back");
            
            // Don't call Complete(), which will cause a rollback
        }
        
        Console.WriteLine("Transaction scope exited");
    }
    
    private static async Task UpdateCustomerAsync(string customerId, string name)
    {
        // Get the current transaction
        var transaction = TransactionScope.Current;
        Console.WriteLine($"Updating customer {customerId} in transaction {transaction?.Id ?? "none"}");
        
        await Task.Delay(10);
        
        // The transaction is still available after the await
        transaction = TransactionScope.Current;
        Console.WriteLine($"Customer {customerId} updated in transaction {transaction?.Id ?? "none"}");
    }
    
    private static async Task UpdateOrderAsync(string orderId, string status)
    {
        // Get the current transaction
        var transaction = TransactionScope.Current;
        Console.WriteLine($"Updating order {orderId} in transaction {transaction?.Id ?? "none"}");
        
        await Task.Delay(10);
        
        // The transaction is still available after the await
        transaction = TransactionScope.Current;
        Console.WriteLine($"Order {orderId} updated in transaction {transaction?.Id ?? "none"}");
    }
    
    private static async Task UpdateProductAsync(string productId, decimal price)
    {
        // Get the current transaction
        var transaction = TransactionScope.Current;
        Console.WriteLine($"Updating product {productId} in transaction {transaction?.Id ?? "none"}");
        
        await Task.Delay(10);
        
        // The transaction is still available after the await
        transaction = TransactionScope.Current;
        Console.WriteLine($"Product {productId} updated in transaction {transaction?.Id ?? "none"}");
    }
    
    private static async Task UserImpersonationExample()
    {
        Console.WriteLine("\n--- Real-world scenario: User impersonation ---");
        
        // Set the current user
        SecurityContext.SetCurrentUser("admin", new[] { "Admin", "User" });
        
        Console.WriteLine($"Current user: {SecurityContext.CurrentUser.Username} with roles: {string.Join(", ", SecurityContext.CurrentUser.Roles)}");
        
        // Perform an operation as the current user
        await PerformAdminOperationAsync();
        
        // Temporarily impersonate another user
        using (SecurityContext.Impersonate("system", new[] { "System", "Admin" }))
        {
            Console.WriteLine($"Impersonated user: {SecurityContext.CurrentUser.Username} with roles: {string.Join(", ", SecurityContext.CurrentUser.Roles)}");
            
            // Perform an operation as the impersonated user
            await PerformSystemOperationAsync();
            
            // The impersonation is maintained across async boundaries
            Console.WriteLine($"Still impersonated as: {SecurityContext.CurrentUser.Username}");
        }
        
        // After the using block, we're back to the original user
        Console.WriteLine($"Back to original user: {SecurityContext.CurrentUser.Username} with roles: {string.Join(", ", SecurityContext.CurrentUser.Roles)}");
    }
    
    private static async Task PerformAdminOperationAsync()
    {
        // Check if the current user has the required role
        if (SecurityContext.CurrentUser.HasRole("Admin"))
        {
            Console.WriteLine($"User {SecurityContext.CurrentUser.Username} performing admin operation");
            await Task.Delay(10);
        }
        else
        {
            Console.WriteLine($"User {SecurityContext.CurrentUser.Username} is not authorized for admin operation");
        }
    }
    
    private static async Task PerformSystemOperationAsync()
    {
        // Check if the current user has the required role
        if (SecurityContext.CurrentUser.HasRole("System"))
        {
            Console.WriteLine($"User {SecurityContext.CurrentUser.Username} performing system operation");
            await Task.Delay(10);
        }
        else
        {
            Console.WriteLine($"User {SecurityContext.CurrentUser.Username} is not authorized for system operation");
        }
    }
    
    #region Helper Classes
    
    // Helper class for the web request context example
    private class WebRequestContext
    {
        private static readonly AsyncLocal<WebRequestContext> _current = new();
        
        public static WebRequestContext Current
        {
            get => _current.Value;
            set => _current.Value = value;
        }
        
        public string RequestId { get; set; }
        public string Method { get; set; }
        public string Path { get; set; }
        public string UserId { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    // Helper class for the logging example
    private static class Logger
    {
        private static readonly AsyncLocal<string> _correlationId = new();
        
        public static void SetCorrelationId(string correlationId)
        {
            _correlationId.Value = correlationId;
        }
        
        public static void Log(string message)
        {
            var correlationId = _correlationId.Value ?? "no-correlation-id";
            var threadId = Thread.CurrentThread.ManagedThreadId;
            Console.WriteLine($"[{correlationId}] [{threadId}] {message}");
        }
    }
    
    // Helper class for the transaction scope example
    private class TransactionScope : IDisposable
    {
        private static readonly AsyncLocal<TransactionScope> _current = new();
        
        public static TransactionScope Current => _current.Value;
        
        public string Id { get; }
        private bool _completed;
        
        public TransactionScope()
        {
            Id = Guid.NewGuid().ToString().Substring(0, 8);
            _current.Value = this;
        }
        
        public void Complete()
        {
            _completed = true;
        }
        
        public void Dispose()
        {
            if (_completed)
            {
                // Transaction was completed successfully
            }
            else
            {
                Console.WriteLine($"Transaction {Id} rolled back");
            }
            
            // Only clear the current transaction if it's this one
            if (_current.Value == this)
            {
                _current.Value = null;
            }
        }
    }
    
    // Helper class for the user impersonation example
    private static class SecurityContext
    {
        private static readonly AsyncLocal<User> _currentUser = new();
        
        public static User CurrentUser => _currentUser.Value;
        
        public static void SetCurrentUser(string username, string[] roles)
        {
            _currentUser.Value = new User(username, roles);
        }
        
        public static IDisposable Impersonate(string username, string[] roles)
        {
            var previousUser = _currentUser.Value;
            _currentUser.Value = new User(username, roles);
            
            return new ImpersonationContext(previousUser);
        }
        
        private class ImpersonationContext : IDisposable
        {
            private readonly User _previousUser;
            
            public ImpersonationContext(User previousUser)
            {
                _previousUser = previousUser;
            }
            
            public void Dispose()
            {
                _currentUser.Value = _previousUser;
            }
        }
        
        public class User
        {
            public string Username { get; }
            public string[] Roles { get; }
            
            public User(string username, string[] roles)
            {
                Username = username;
                Roles = roles;
            }
            
            public bool HasRole(string role)
            {
                return Array.Exists(Roles, r => r.Equals(role, StringComparison.OrdinalIgnoreCase));
            }
        }
    }
    
    #endregion
}
