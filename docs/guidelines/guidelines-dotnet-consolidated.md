# .NET Development Standards and Guidelines

## Table of Contents
1. [Logging Standards](#logging-standards)
2. [Module Library Structure](#module-library-structure)
3. [Application Monitoring](#application-monitoring)
   - [Option A: Elastic APM](#option-a-elastic-apm-for-application-monitoring)
   - [Option B: Prometheus Metrics](#option-b-prometheus-metrics)
4. [Data Access Guidelines](#data-access-guidelines)
   - [Service Layer & Data Access](#service-layer--data-access)
   - [Repository Pattern Considerations](#repository-pattern-considerations)
   - [DTO Strategy](#dto-strategy)
   - [Mapping Strategies](#mapping-strategies)
   - [Performance Optimization](#performance-optimization)

## Logging Standards

### Logging Levels

| Level | Description | Usage |
|-------|-------------|-------|
| **TRACE** | Most detailed information | Only for detailed debugging |
| **DEBUG** | Detailed debugging information | Development environments |
| **INFO** | General operational information | Normal operations, significant events |
| **WARN** | Potential issues | Deprecated API usage, poor performance |
| **ERROR** | Error conditions | Exception handling, failed operations |
| **FATAL** | Critical failures | Application crashes, data corruption |

### Recommended Implementation

#### Using Microsoft.Extensions.Logging
```csharp
// Service registration
public void ConfigureServices(IServiceCollection services)
{
    services.AddLogging(builder =>
    {
        builder.AddConsole();
        builder.AddDebug();
        builder.AddFilter("Microsoft", LogLevel.Warning);
        builder.AddFilter("YourApp", LogLevel.Information);
    });
}

// Usage in a class
public class ExampleService
{
    private readonly ILogger<ExampleService> _logger;

    public ExampleService(ILogger<ExampleService> logger)
    {
        _logger = logger;
    }

    public void DoSomething()
    {
        _logger.LogInformation("Operation started at {time}", DateTime.UtcNow);
        
        try
        {
            // Operation code
            _logger.LogDebug("Operation details: {details}", "relevant info");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during operation");
            throw;
        }
    }
}
```

#### Using Serilog (Recommended)
```csharp
// In Program.cs
public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .UseSerilog((hostingContext, loggerConfiguration) => 
        {
            loggerConfiguration
                .ReadFrom.Configuration(hostingContext.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day);
        });
```

### Structured Logging Best Practices

- Use structured logging format
  ```csharp
  // Good (structured)
  _logger.LogInformation("User {UserName} logged in at {LoginTime}", userName, DateTime.UtcNow);
  
  // Bad (unstructured)
  _logger.LogInformation("User " + userName + " logged in at " + DateTime.UtcNow);
  ```

- Include context with log scopes
  ```csharp
  using (_logger.BeginScope(new Dictionary<string, object>
  {
      ["UserId"] = userId,
      ["RequestId"] = requestId
  }))
  {
      _logger.LogInformation("Processing payment for order {OrderId}", orderId);
  }
  ```

- Proper exception logging
  ```csharp
  try
  {
      // Operation
  }
  catch (Exception ex)
  {
      _logger.LogError(ex, "Failed to process order {OrderId}", orderId);
      throw;
  }
  ```

### What to Log

#### Always Log
- Application startup and shutdown
- Authentication events 
- Authorization failures
- Data access operations (especially writes)
- All exceptions and errors
- API calls (especially external services)
- Configuration changes

#### Never Log
- Passwords or authentication tokens
- Credit card numbers, SSNs, or other PII
- Encryption keys or credentials
- Sensitive business data
- Session identifiers

### JavaScript Logging

```javascript
// Simple console wrapper
const Logger = {
    debug: (message, ...args) => console.debug(`[DEBUG] ${message}`, ...args),
    info: (message, ...args) => console.info(`[INFO] ${message}`, ...args),
    warn: (message, ...args) => console.warn(`[WARN] ${message}`, ...args),
    error: (message, ...args) => console.error(`[ERROR] ${message}`, ...args),
    
    // Send critical errors to backend
    logToServer: (level, message, data) => {
        $.ajax({
            url: '/api/logs',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                level,
                message,
                data,
                url: window.location.href,
                userAgent: navigator.userAgent,
                timestamp: new Date().toISOString()
            })
        });
    }
};

// Global error handler
window.onerror = function(message, source, lineno, colno, error) {
    Logger.error('Unhandled exception', { message, source, lineno, colno });
    Logger.logToServer('ERROR', message, { source, lineno, colno, stack: error?.stack });
    return false;
};
```

## Module Library Structure

### Project Structure
```
ProjectName
├── Configuration
│   └── ProjectOptions.cs
├── Controllers (if contains API endpoints)
├── Data (for data access and models)
├── DTO (Data Transfer Objects)
├── Enum
├── Extensions
│   └── ServiceCollectionExtensions.cs
├── Models
├── Services
│   ├── Interfaces
│   └── Implementation
└── Utilities (optional helper classes)
```

### Configuration Guidelines

Use the Options pattern for dependency injection and configuration binding:

```csharp
public class ProjectOptions
{
    public static readonly string SectionName = "ProjectName";
    
    public string Property1 { get; set; }
    public int Property2 { get; set; }
    public bool EnableFeatureX { get; set; }
    public string BaseUrl { get; set; }
}
```

### Extension Methods

Register all module services with the DI container via extensions:

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProject(this IServiceCollection services, IConfiguration config)
    {
        // Validate parameters
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (config == null) throw new ArgumentNullException(nameof(config));
        
        // Bind configuration
        ProjectOptions options = new ProjectOptions();
        config.GetSection(ProjectOptions.SectionName).Bind(options);
        services.Configure<ProjectOptions>(config.GetSection(ProjectOptions.SectionName));
        
        // Register module services
        services.AddSingleton<IProjectSingleton, ProjectSingleton>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddTransient<IProjectProcessor, ProjectProcessor>();
        
        // Conditionally register optional features
        if (options.EnableFeatureX)
        {
            services.AddTransient<IFeatureXService, FeatureXService>();
        }
        
        return services;
    }
}
```

### Service Implementation Pattern

Follow this pattern for all services:

```csharp
public class ProjectService : IProjectService
{
    private readonly ILogger<ProjectService> _logger;
    private readonly ProjectOptions _options;
    
    public ProjectService(
        ILogger<ProjectService> logger,
        IOptions<ProjectOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }
    
    public async Task<OperationResult<ProjectData>> ProcessDataAsync(int id)
    {
        try
        {
            _logger.LogInformation("Processing data for ID: {Id}", id);
            
            // Implementation logic
            
            return OperationResult<ProjectData>.Success(result);
        }
        catch (SpecificException ex)
        {
            _logger.LogWarning(ex, "Specific issue occurred processing ID: {Id}", id);
            return OperationResult<ProjectData>.Failure("A specific issue occurred", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process data for ID: {Id}", id);
            return OperationResult<ProjectData>.Failure("An unexpected error occurred", ex);
        }
    }
}
```

### Result Pattern

Use a standard result wrapper for service methods:

```csharp
public class OperationResult<T>
{
    public bool Success { get; private set; }
    public T Data { get; private set; }
    public string ErrorMessage { get; private set; }
    public Exception Exception { get; private set; }
    
    public static OperationResult<T> Success(T data)
    {
        return new OperationResult<T>
        {
            Success = true,
            Data = data
        };
    }
    
    public static OperationResult<T> Failure(string errorMessage, Exception ex = null)
    {
        return new OperationResult<T>
        {
            Success = false,
            ErrorMessage = errorMessage,
            Exception = ex
        };
    }
}
```

### Module Usage in Host Application

```csharp
// In Program.cs
var builder = WebApplication.CreateBuilder(args);

// Register module services
builder.Services.AddProject(builder.Configuration);

var app = builder.Build();

// ...configure middleware...

app.Run();
```

Configuration in `appsettings.json`:

```json
{
  "ProjectName": {
    "Property1": "Value1",
    "Property2": 42,
    "EnableFeatureX": true,
    "BaseUrl": "https://api.example.com"
  }
}
```

## Application Monitoring

Choose between Elastic APM or Prometheus metrics based on your project needs:

### Option A: Elastic APM for Application Monitoring

#### Installation
```bash
dotnet add package Elastic.Apm.NetCoreAll
dotnet add package Elastic.Apm.AspNetCore
```

#### Configuration
```csharp
// In Startup.cs or Program.cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddElasticApm();
}
```

```json
// appsettings.json
{
    "ElasticApm": {
        "ServiceName": "MyApp",
        "ServerUrls": "http://localhost:8200",
        "Environment": "development",
        "TransactionSampleRate": 1.0
    }
}
```

#### Transaction Tracing

```csharp
public class OrderService
{
    private readonly ITracer _tracer;

    public OrderService(ITracer tracer)
    {
        _tracer = tracer;
    }

    public async Task ProcessOrderAsync(Order order)
    {
        // Start a transaction
        var transaction = _tracer.StartTransaction("ProcessOrder", "app");
        
        try 
        {
            // Business logic
            await ValidateOrderAsync(order);
            await SaveOrderAsync(order);
            
            transaction.Result = "success";
        }
        catch (Exception ex)
        {
            _tracer.CaptureException(ex);
            transaction.Result = "error";
            throw;
        }
        finally 
        {
            transaction.End();
        }
    }
}
```

#### Span Tracing

```csharp
public async Task ValidateOrderAsync(Order order)
{
    var span = _tracer.CurrentTransaction?.StartSpan("ValidateOrder", "app");
    
    try 
    {
        // Validation logic
    }
    catch (Exception ex)
    {
        _tracer.CaptureException(ex);
        throw;
    }
    finally 
    {
        span?.End();
    }
}
```

#### Best Practices
- Use descriptive, consistent naming for transactions
- Always capture exceptions
- Always end transactions/spans to prevent memory leaks
- Be selective about what you trace (don't trace everything)

### Option B: Prometheus Metrics

#### Installation
```bash
dotnet add package prometheus-net
dotnet add package prometheus-net.AspNetCore
```

#### Configuration
```csharp
// In Program.cs
var builder = WebApplication.CreateBuilder(args);
// Add services...

var app = builder.Build();

// Enable Prometheus metrics collection
app.UseMetricServer();
app.UseHttpMetrics();

// Other middleware...
app.Run();
```

#### Metric Types

1. **Counter**: For values that only increase
   ```csharp
   private static readonly Counter OrdersProcessed = Metrics
       .CreateCounter("app_orders_processed_total", 
                      "Number of processed orders",
                      new CounterConfiguration 
                      { 
                          LabelNames = new[] { "order_type", "status" }
                      });
   
   // Usage
   OrdersProcessed.WithLabels("standard", "success").Inc();
   ```

2. **Gauge**: For values that can increase and decrease
   ```csharp
   private static readonly Gauge ActiveConnections = Metrics
       .CreateGauge("app_active_connections", 
                    "Number of active connections");
   
   // Usage
   ActiveConnections.Set(count);
   ```

3. **Histogram**: For measuring distributions
   ```csharp
   private static readonly Histogram OrderProcessingTime = Metrics
       .CreateHistogram("app_order_processing_seconds", 
                        "Time spent processing orders",
                        new HistogramConfiguration 
                        {
                            LabelNames = new[] { "order_type" },
                            Buckets = Histogram.ExponentialBuckets(0.01, 2, 10)
                        });
   
   // Usage
   using (OrderProcessingTime.WithLabels("standard").NewTimer())
   {
       // Measured code here
   }
   ```

#### Naming Conventions

Format: `[app_name]_[entity]_[action]_[unit]`
- Use snake_case for metric names
- Common suffixes: `_total`, `_count`, `_seconds`, `_bytes`
- Examples: `app_orders_processed_total`, `app_http_request_duration_seconds`

#### Labeling Strategy

- Use labels for low-cardinality dimensions (HTTP method, endpoint, status code)
- Limit to 3-4 labels per metric
- Avoid high-cardinality labels (user IDs, session IDs, full URLs)

#### Implementation Example

```csharp
public class MetricsService
{
    private static readonly Counter OrdersProcessed = Metrics
        .CreateCounter("app_orders_processed_total", "Number of processed orders",
                    new CounterConfiguration { 
                        LabelNames = new[] { "order_type", "status" } 
                    });
    
    private static readonly Histogram OrderProcessingTime = Metrics
        .CreateHistogram("app_order_processing_seconds", "Time spent processing orders",
                      new HistogramConfiguration
                      {
                          Buckets = Histogram.ExponentialBuckets(0.01, 2, 10),
                          LabelNames = new[] { "order_type" }
                      });
    
    public void ProcessOrder(string orderType)
    {
        using (OrderProcessingTime.WithLabels(orderType).NewTimer())
        {
            try
            {
                // Process order logic
                
                OrdersProcessed.WithLabels(orderType, "success").Inc();
            }
            catch (Exception)
            {
                OrdersProcessed.WithLabels(orderType, "error").Inc();
                throw;
            }
        }
    }
}
```

## Data Access Guidelines

### Service Layer & Data Access

#### Direct DbContext Access Approach

Our architectural decision is to access the DbContext directly from the service layer, bypassing the traditional repository pattern in favor of a more streamlined approach.

##### Benefits
- Reduced abstraction overhead
- Full access to EF Core features and optimizations
- Simpler transaction management
- More straightforward LINQ queries
- Better testability with in-memory database providers

##### Guidelines
1. **Inject DbContext Directly**: Service classes should receive DbContext via constructor injection
2. **Use Async Methods**: Always prefer async/await for database operations
3. **Scope Transactions Appropriately**: Use transactions for operations that span multiple entities
4. **Encapsulate Query Logic**: Keep complex queries within the service layer
5. **Consider Domain Integrity**: The service layer is responsible for maintaining domain rules and data integrity

```csharp
public class OrderService : IOrderService
{
    private readonly AppDbContext _dbContext;
    private readonly IMapper _mapper;
    
    public OrderService(AppDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }
    
    public async Task<OrderDto> GetOrderWithDetailsAsync(int orderId)
    {
        // Query logic encapsulated in service
        var order = await _dbContext.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);
            
        return _mapper.Map<OrderDto>(order);
    }
}
```

### Repository Pattern Considerations

While we prefer direct DbContext access, consider repositories in these cases:

#### When to Consider Repositories
- For extremely complex domains with sophisticated business rules
- When the same data access patterns are repeated across many services
- For specific performance optimizations that benefit from caching

#### When to Avoid Repositories
- For CRUD operations on simple entities
- When query requirements frequently change
- When full EF Core feature access is needed

### DTO Strategy

Data Transfer Objects decouple your domain model from your application's interface layers.

#### Types of DTOs

1. **Request DTOs**: Used for incoming data
   - Include only fields necessary for the operation
   - Apply validation attributes
   - Consider versioning for API stability

2. **Response DTOs**: Used for outgoing data
   - Shape data according to client needs
   - Flatten complex object graphs
   - Exclude sensitive or unnecessary information

3. **View-Specific DTOs**: Tailor data representation to specific views or use cases

#### Design Principles

1. **Purpose-Specific**: Design DTOs for specific use cases rather than creating generic ones
2. **Flat Structure**: Minimize nesting to improve serialization/deserialization performance
3. **Immutability**: Consider making response DTOs immutable (read-only properties)
4. **Validation**: Include validation logic in request DTOs
5. **Documentation**: Document DTO properties thoroughly, especially for public APIs

```csharp
// Base DTO with common properties
public class ProductBaseDto
{
    public int Id { get; set; }
    public string Name { get; set; }
}

// Request-specific DTO with validation
public class CreateProductDto
{
    [Required, StringLength(100)]
    public string Name { get; set; }
    
    [Range(0.01, 10000)]
    public decimal Price { get; set; }
    
    public int CategoryId { get; set; }
}

// Response-specific DTO with additional data
public class ProductDetailDto : ProductBaseDto
{
    public decimal Price { get; set; }
    public string CategoryName { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsAvailable { get; set; }
}
```

### Mapping Strategies

#### AutoMapper Integration

```csharp
// In startup/program configuration
services.AddAutoMapper(typeof(Startup));

// Profile configuration
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Product, ProductDetailDto>()
            .ForMember(dest => dest.CategoryName, 
                      opt => opt.MapFrom(src => src.Category.Name));
        
        CreateMap<CreateProductDto, Product>();
    }
}

// Usage in service
public async Task<ProductDetailDto> GetProductAsync(int id)
{
    var product = await _dbContext.Products
        .Include(p => p.Category)
        .FirstOrDefaultAsync(p => p.Id == id);
        
    return _mapper.Map<ProductDetailDto>(product);
}
```

#### Direct Projection Queries

For performance-critical operations, direct projection with LINQ can be more efficient:

```csharp
public async Task<List<ProductSummaryDto>> GetProductsForListingAsync()
{
    return await _dbContext.Products
        .AsNoTracking()
        .Select(p => new ProductSummaryDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            IsAvailable = p.StockQuantity > 0
        })
        .ToListAsync();
}
```

### Performance Optimization

#### Query Optimization
1. **Use AsNoTracking()**: For read-only operations
2. **Projection Queries**: Select only required fields
3. **Include Related Data Judiciously**: Only include what's needed
4. **Pagination**: Always paginate large result sets

#### Efficient Loading Strategies
1. **Eager Loading**: Use Include() for required related entities
2. **Explicit Loading**: Load related entities on demand
3. **Select Loading**: Load only specific properties of related entities

```csharp
// Efficient paged query example
public async Task<PagedResult<ProductSummaryDto>> GetPagedProductsAsync(int page, int pageSize)
{
    var query = _dbContext.Products.AsNoTracking();
    
    var totalCount = await query.CountAsync();
    
    var products = await query
        .OrderBy(p => p.Name)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(p => new ProductSummaryDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            IsAvailable = p.StockQuantity > 0
        })
        .ToListAsync();
        
    return new PagedResult<ProductSummaryDto>
    {
        Items = products,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize
    };
}
```

## Best Practices Summary

1. **Logging**
   - Use structured logging with semantic property names
   - Log at appropriate levels based on severity
   - Include context information in logs
   - Never log sensitive data
   - Configure appropriate log destinations for each environment

2. **Module Structure**
   - Follow consistent project organization
   - Use interface-based design for all services
   - Implement appropriate service lifetimes
   - Use the Options pattern for configuration
   - Follow the Result pattern for service methods

3. **Application Monitoring**
   - Choose monitoring solution based on project needs
   - Implement consistent naming conventions
   - Be selective about what to monitor
   - Consider performance impact
   - Set up appropriate alerting

4. **Data Access**
   - Use DbContext directly in service layer
   - Use async/await for all database operations
   - Create purpose-specific DTOs
   - Use AutoMapper for entity-to-DTO mapping
   - Apply performance optimizations like AsNoTracking() and projection
   - Return DTOs, never entities to calling layers
   - Implement pagination for list operations
   - Optimize for N+1 query problems