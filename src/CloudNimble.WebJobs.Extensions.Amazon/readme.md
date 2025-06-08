# CloudNimble.WebJobs.Extensions.Amazon

[![NuGet](https://img.shields.io/nuget/v/CloudNimble.WebJobs.Extensions.Amazon.svg)](https://www.nuget.org/packages/CloudNimble.WebJobs.Extensions.Amazon/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Overview

CloudNimble.WebJobs.Extensions.Amazon brings the power of AWS services to Azure WebJobs! This package enables you to use Amazon SQS queues as triggers and bindings in your Azure Functions and WebJobs, allowing you to build cloud-agnostic solutions that leverage the best of both worlds.

## Why Use This?

- **Multi-Cloud Strategy**: Run Azure Functions that process AWS SQS messages
- **Gradual Migration**: Move from AWS to Azure (or vice versa) without rewriting your queue processing logic
- **Best of Both Worlds**: Use Azure's serverless compute with AWS's messaging infrastructure
- **Familiar Programming Model**: If you know Azure WebJobs, you already know how to use this

## Features

- 🚀 **SQS Triggers**: Process messages from Amazon SQS queues with automatic scaling
- 📤 **SQS Output Bindings**: Send messages to SQS queues from your functions
- 🔄 **Automatic Retries**: Built-in retry logic with configurable poison message handling
- 🎯 **Multiple Message Types**: Support for raw SQS messages, strings, and custom POCOs
- 📊 **Metrics & Monitoring**: Integration with Azure Monitor for SQS queue metrics
- 🔒 **Secure**: Support for IAM roles, credentials, and LocalStack for development

## Installation

```bash
dotnet add package CloudNimble.WebJobs.Extensions.Amazon
```

## Quick Start

### 1. Configure Your Host

```csharp
using Microsoft.Extensions.Hosting;
using CloudNimble.WebJobs.Extensions.Amazon;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // Add AWS services
        services.AddDefaultAWSOptions(Configuration.GetAWSOptions());
        services.AddAWSService<IAmazonSQS>();
        
        // Add SQS extension
        services.AddSQSExtension();
    })
    .Build();

host.Run();
```

### 2. Create Your First SQS Trigger

```csharp
public class OrderProcessor
{
    [FunctionName("ProcessOrder")]
    public async Task ProcessNewOrder(
        [SQSTrigger("orders-queue")] Order order,
        ILogger log)
    {
        log.LogInformation($"Processing order {order.Id} for {order.CustomerName}");
        
        // Your business logic here
        await ProcessOrderAsync(order);
    }
}

public class Order
{
    public string Id { get; set; }
    public string CustomerName { get; set; }
    public decimal Total { get; set; }
    public List<OrderItem> Items { get; set; }
}
```

### 3. Send Messages to SQS

```csharp
public class OrderService
{
    [FunctionName("CreateOrder")]
    public async Task<IActionResult> CreateOrder(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
        [SQS("orders-queue")] IAsyncCollector<Order> orderQueue)
    {
        var order = await req.GetBodyAsync<Order>();
        
        // Send to SQS queue
        await orderQueue.AddAsync(order);
        
        return new OkObjectResult(new { orderId = order.Id });
    }
}
```

## Advanced Scenarios

### Working with Raw SQS Messages

```csharp
[FunctionName("ProcessRawMessage")]
public async Task ProcessRawSQSMessage(
    [SQSTrigger("raw-queue")] SQSMessage message,
    ILogger log)
{
    log.LogInformation($"MessageId: {message.MessageId}");
    log.LogInformation($"Body: {message.Body}");
    
    // Access message attributes
    foreach (var attr in message.MessageAttributes)
    {
        log.LogInformation($"Attribute {attr.Key}: {attr.Value.StringValue}");
    }
    
    // Process based on content
    if (message.MessageAttributes.ContainsKey("Type"))
    {
        await RouteMessageByType(message);
    }
}
```

### Batch Processing

```csharp
[FunctionName("ProcessBatch")]
public async Task ProcessMessageBatch(
    [SQSTrigger("batch-queue", BatchSize = 10)] SQSMessage[] messages,
    ILogger log)
{
    log.LogInformation($"Processing batch of {messages.Length} messages");
    
    var tasks = messages.Select(msg => ProcessSingleMessageAsync(msg));
    await Task.WhenAll(tasks);
}
```

### Dead Letter Queue Handling

```csharp
[FunctionName("ProcessDeadLetters")]
public async Task HandleDeadLetterMessages(
    [SQSTrigger("orders-queue-dlq")] DeadLetterMessage<Order> deadLetter,
    ILogger log)
{
    log.LogWarning($"Dead letter received. Attempts: {deadLetter.DequeueCount}");
    log.LogWarning($"Original error: {deadLetter.LastError}");
    
    // Attempt remediation or alert operations team
    await NotifyOperationsTeam(deadLetter);
}
```

### Configuration Options

```csharp
services.Configure<SQSOptions>(options =>
{
    // Polling configuration
    options.MaxPollingInterval = TimeSpan.FromSeconds(20);
    options.BatchSize = 10;
    
    // Retry configuration
    options.MaxDequeueCount = 5;
    options.VisibilityTimeout = TimeSpan.FromMinutes(5);
    
    // Processing configuration
    options.MaxConcurrentCalls = 16;
    options.PrefetchCount = 32;
});
```

### Using with LocalStack

Perfect for local development and testing:

```csharp
services.AddDefaultAWSOptions(new AWSOptions
{
    ServiceURL = "http://localhost:4566",
    AuthenticationRegion = "us-east-1",
    Credentials = new BasicAWSCredentials("test", "test")
});
```

## Queue Name Resolution

Use application settings for flexible queue naming:

```json
{
  "Values": {
    "OrdersQueue": "prod-orders-queue",
    "OrdersQueueDLQ": "prod-orders-queue-dlq"
  }
}
```

```csharp
[FunctionName("ProcessOrder")]
public async Task ProcessOrder(
    [SQSTrigger("%OrdersQueue%")] Order order)
{
    // Queue name resolved from settings
}
```

## Monitoring and Metrics

The extension provides built-in metrics for monitoring:

- **Queue Length**: Current approximate message count
- **Message Age**: Age of oldest message in queue
- **Processing Rate**: Messages processed per second
- **Error Rate**: Failed message processing rate

These metrics integrate with Azure Monitor for alerting and autoscaling.

## Best Practices

1. **Use POCO Types**: Define strongly-typed classes for your messages
2. **Handle Idempotency**: Messages may be processed more than once
3. **Set Appropriate Timeouts**: Configure visibility timeout based on processing time
4. **Monitor Dead Letter Queues**: Set up alerts for DLQ messages
5. **Use Batch Processing**: For high-throughput scenarios, process messages in batches

## Migration from AWS Lambda

Moving from AWS Lambda to Azure Functions? It's easy:

**AWS Lambda:**
```csharp
public async Task Handler(SQSEvent sqsEvent, ILambdaContext context)
{
    foreach (var message in sqsEvent.Records)
    {
        await ProcessMessage(message.Body);
    }
}
```

**Azure Functions with this extension:**
```csharp
[FunctionName("ProcessMessages")]
public async Task Run([SQSTrigger("my-queue")] string message)
{
    await ProcessMessage(message);
}
```

## Troubleshooting

### Common Issues

**Q: Messages are not being processed**
- Check AWS credentials and permissions
- Verify queue name and region
- Check Azure Functions logs for errors

**Q: Messages are being processed multiple times**
- Increase visibility timeout
- Ensure processing completes within timeout
- Implement idempotency in your logic

**Q: High latency when polling**
- Adjust polling intervals
- Consider batch processing
- Check network connectivity to AWS

## Contributing

We welcome contributions! Please see our [Contributing Guide](https://github.com/CloudNimble/WebJobs.Extensions/blob/main/CONTRIBUTING.md) for details.

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/CloudNimble/WebJobs.Extensions/blob/main/LICENSE) file for details.

## Support

- 📧 Email: opensource@nimbleapps.cloud
- 🐛 Issues: [GitHub Issues](https://github.com/CloudNimble/WebJobs.Extensions/issues)
- 💬 Discussions: [GitHub Discussions](https://github.com/CloudNimble/WebJobs.Extensions/discussions)
- 📖 Documentation: [GitHub Wiki](https://github.com/CloudNimble/WebJobs.Extensions/wiki)

---

Made with ❤️ by [CloudNimble](https://nimbleapps.cloud)