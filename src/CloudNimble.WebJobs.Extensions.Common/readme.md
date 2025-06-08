# CloudNimble.WebJobs.Extensions.Common

[![NuGet](https://img.shields.io/nuget/v/CloudNimble.WebJobs.Extensions.Common.svg)](https://www.nuget.org/packages/CloudNimble.WebJobs.Extensions.Common/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Overview

CloudNimble.WebJobs.Extensions.Common provides a robust foundation for building cloud-agnostic Azure WebJobs extensions. This package contains base classes, interfaces, and utilities that enable you to create reliable, scalable queue processing solutions that work across different cloud providers.

## Features

- **Queue Processing Framework**: Generic queue listener and processor implementations with built-in retry logic and poison message handling
- **Flexible Type Conversion**: Comprehensive converter system for seamless message deserialization
- **Advanced Timing Strategies**: Exponential backoff, linear speedup, and customizable delay strategies
- **Metrics and Monitoring**: Built-in metrics providers for queue depth monitoring and autoscaling
- **Dependency Injection Ready**: Full support for modern .NET dependency injection patterns
- **Thread-Safe Operations**: Context accessors and shared listeners for safe concurrent processing

## Installation

```bash
dotnet add package CloudNimble.WebJobs.Extensions.Common
```

## Key Components

### Queue Processing

The library provides a complete queue processing pipeline:

```csharp
// Custom queue processor with advanced error handling
public class MyQueueProcessor : QueueProcessor<MyMessage>
{
    protected override async Task<bool> BeginProcessingMessageAsync(
        MyMessage message, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Your processing logic here
            await ProcessBusinessLogic(message);
            return true; // Message processed successfully
        }
        catch (TransientException)
        {
            return false; // Retry the message
        }
    }
    
    protected override async Task CompleteProcessingMessageAsync(
        MyMessage message, 
        FunctionResult result, 
        CancellationToken cancellationToken)
    {
        if (result.Succeeded)
        {
            // Clean up or audit successful processing
            await LogSuccess(message);
        }
    }
}
```

### Type Converters

Create custom converters for your message types:

```csharp
public class CustomMessageConverter : IAsyncObjectToTypeConverter<IQueueMessage>
{
    public async Task<ConversionResult<TOutput>> TryConvertAsync<TOutput>(
        IQueueMessage input, 
        CancellationToken cancellationToken)
    {
        if (typeof(TOutput) == typeof(MyCustomType))
        {
            var json = await input.GetBodyAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<MyCustomType>(json);
            return ConversionResult<TOutput>.Success((TOutput)(object)result);
        }
        
        return ConversionResult<TOutput>.Failure();
    }
}
```

### Delay Strategies

Implement custom backoff strategies for retry scenarios:

```csharp
public class CustomDelayStrategy : IDelayStrategy
{
    public TimeSpan GetNextDelay(bool executionSucceeded, TimeSpan currentDelay)
    {
        if (executionSucceeded)
        {
            return TimeSpan.Zero; // No delay on success
        }
        
        // Custom backoff logic
        return TimeSpan.FromSeconds(Math.Min(currentDelay.TotalSeconds * 2, 300));
    }
}
```

### Metrics and Scaling

Implement queue metrics for autoscaling:

```csharp
public class MyQueueMetricsProvider : IQueueMetricsProvider
{
    public async Task<QueueProperties> GetQueuePropertiesAsync(
        string queueName, 
        CancellationToken cancellationToken)
    {
        var messageCount = await GetMessageCountFromService(queueName);
        
        return new QueueProperties
        {
            ApproximateMessagesCount = messageCount,
            QueueLength = messageCount
        };
    }
}
```

## Integration with WebJobs

The Common package integrates seamlessly with Azure WebJobs:

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Register your custom implementations
        services.AddSingleton<IQueueProcessorFactory, DefaultQueueProcessorFactory>();
        services.AddSingleton<IDelayStrategy, RandomizedExponentialBackoffStrategy>();
        
        // Configure queue processing options
        services.Configure<QueueProcessorOptions>(options =>
        {
            options.MaxDequeueCount = 5;
            options.VisibilityTimeout = TimeSpan.FromMinutes(5);
            options.MaxPollingInterval = TimeSpan.FromSeconds(30);
        });
    }
}
```

## Advanced Scenarios

### Poison Message Handling

```csharp
public class PoisonMessageHandler
{
    public async Task HandlePoisonMessageAsync(PoisonMessageEventArgs args)
    {
        // Log the poison message
        await LogPoisonMessage(args.Message, args.Exception);
        
        // Move to dead letter queue
        await MoveToDeadLetterQueue(args.Message);
        
        // Send alert
        await NotifyOperations(args);
    }
}
```

### Context Sharing

Share context across different components:

```csharp
public class RequestContextAccessor : ContextAccessor<RequestContext>
{
    // Automatically provides thread-safe access to RequestContext
}

// Usage
services.AddScoped<IContextGetter<RequestContext>, RequestContextAccessor>();
services.AddScoped<IContextSetter<RequestContext>>(
    provider => provider.GetService<RequestContextAccessor>());
```

## Best Practices

1. **Use Dependency Injection**: Leverage the built-in DI support for better testability
2. **Implement Proper Retry Logic**: Use the provided delay strategies or create custom ones
3. **Monitor Queue Metrics**: Implement metrics providers for visibility and autoscaling
4. **Handle Poison Messages**: Always implement poison message handling to prevent message loss
5. **Use Type Converters**: Create converters for your custom types to maintain clean code

## Contributing

We welcome contributions! Please see our [Contributing Guide](https://github.com/CloudNimble/WebJobs.Extensions/blob/main/CONTRIBUTING.md) for details.

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/CloudNimble/WebJobs.Extensions/blob/main/LICENSE) file for details.

## Support

- 📧 Email: opensource@nimbleapps.cloud
- 🐛 Issues: [GitHub Issues](https://github.com/CloudNimble/WebJobs.Extensions/issues)
- 📖 Documentation: [GitHub Wiki](https://github.com/CloudNimble/WebJobs.Extensions/wiki)

---

Made with ❤️ by [CloudNimble](https://nimbleapps.cloud)