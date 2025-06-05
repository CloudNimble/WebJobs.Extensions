# CloudNimble WebJobs Extensions Examples

This example project demonstrates how to use the CloudNimble WebJobs Extensions for Amazon SQS with Azure WebJobs. It includes:

- **Timer-triggered function** that publishes messages to an SQS queue using the `SQSOutputAttribute` output binding
- **SQS-triggered functions** that process messages from the queue
- Configuration for connecting to real AWS SQS queues
- Proper error handling and logging

## Prerequisites

1. **AWS Account** with SQS access
2. **.NET 8.0 SDK** or later
3. **AWS CLI** (optional, for testing)
4. **Visual Studio 2022** or **VS Code** (optional)

## Quick Start

### 1. Configure AWS Credentials

Choose one of these methods:

#### Option A: Environment Variables
```bash
export AWS_ACCESS_KEY_ID=your-access-key
export AWS_SECRET_ACCESS_KEY=your-secret-key
export AWS_DEFAULT_REGION=us-east-1
```

#### Option B: AWS Profile
```bash
aws configure --profile webjobs-example
```

Then set the profile in `appsettings.json`:
```json
{
  "AWS": {
    "Profile": "webjobs-example"
  }
}
```

#### Option C: User Secrets (Development)
```bash
dotnet user-secrets set "AWS:AccessKey" "your-access-key"
dotnet user-secrets set "AWS:SecretKey" "your-secret-key"
```

### 2. Configure SQS Queue

Update `appsettings.json` with your queue name:
```json
{
  "SQS": {
    "QueueName": "your-queue-name",
    "MessagePublishInterval": 30
  }
}
```

The `MessagePublishInterval` is in seconds. Default is 30 seconds.

### 3. Run the Application

```bash
cd CloudNimble.WebJobs.Extensions.Examples
dotnet run
```

## Features

### Message Publisher (Timer Trigger)

The `MessagePublisherFunction` publishes a message to SQS every X seconds (configurable):

```csharp
[FunctionName("PublishMessageToSQS")]
public async Task PublishMessage(
    [TimerTrigger("*/30 * * * * *")] TimerInfo timer,
    [SQSOutput("%SQS:QueueName%")] IAsyncCollector<ExampleMessage> messageCollector,
    ILogger log)
```

- Uses the `SQSOutputAttribute` for declarative output binding
- Automatically handles message serialization to JSON
- The SQS extension creates the queue if it doesn't exist
- Publishes structured JSON messages with metadata

### Message Processors (SQS Triggers)

Three different processing patterns are demonstrated:

1. **Raw SQSMessage Processing**
   ```csharp
   [SQSTrigger("%SQS:QueueName%")] SQSMessage message
   ```

2. **String Message Processing**
   ```csharp
   [SQSTrigger("%SQS:QueueName%")] string messageContent
   ```

3. **Typed Message Processing**
   ```csharp
   [SQSTrigger("%SQS:QueueName%")] ExampleMessage message
   ```

## Configuration Options

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Function": "Information"
    }
  },
  "AWS": {
    "Region": "us-east-1",
    "Profile": "default"
  },
  "SQS": {
    "QueueName": "webjobs-example-queue",
    "ServiceUrl": null,
    "MaxDequeueCount": 5,
    "VisibilityTimeout": 30,
    "MessagePublishInterval": 30
  }
}
```

### Environment Variables

All settings can be overridden with environment variables:

- `AWS_ACCESS_KEY_ID` - AWS access key
- `AWS_SECRET_ACCESS_KEY` - AWS secret key
- `AWS_DEFAULT_REGION` - AWS region
- `SQS_QUEUE_NAME` - Override queue name
- `SQS__ServiceUrl` - Override SQS endpoint (for LocalStack)

## Testing with LocalStack

To test with LocalStack instead of real AWS:

1. Start LocalStack:
   ```bash
   localstack start -d
   ```

2. Set the service URL:
   ```json
   {
     "SQS": {
       "ServiceUrl": "http://localhost:4566"
     }
   }
   ```

3. Use test credentials:
   ```bash
   export AWS_ACCESS_KEY_ID=test
   export AWS_SECRET_ACCESS_KEY=test
   ```

## Monitoring

The example includes:

- Detailed console logging
- Metrics logging for messages published/processed
- Error logging with stack traces
- Message metadata in logs

Watch the console output to see:
- Timer trigger firing every X seconds
- Messages being published to SQS
- Messages being received and processed
- Processing time based on message priority

## Troubleshooting

### Queue Not Found
- The publisher will automatically create the queue if it doesn't exist
- Check your AWS credentials and region
- Verify the queue name in configuration

### Access Denied
- Ensure your AWS credentials have these SQS permissions:
  - `sqs:CreateQueue`
  - `sqs:SendMessage`
  - `sqs:ReceiveMessage`
  - `sqs:DeleteMessage`
  - `sqs:GetQueueUrl`
  - `sqs:GetQueueAttributes`

### Messages Not Processing
- Check the console for error messages
- Verify the queue name matches in publisher and processor
- Check if messages are in the dead letter queue

### High AWS Costs
- Adjust `MessagePublishInterval` to reduce message frequency
- Use LocalStack for development/testing
- Set up CloudWatch alarms for queue metrics

## Architecture

```
Timer (every 30s) → PublishMessage → SQS Queue → ProcessMessage → Console
                                           ↓
                                    (Failed messages)
                                           ↓
                                    Dead Letter Queue
```

## Next Steps

1. **Add Custom Processing Logic** - Modify `MessageProcessorFunction` to do real work
2. **Add More Message Types** - Create different message models and processors
3. **Add Database Integration** - Store processed messages in a database
4. **Add SNS Integration** - Publish notifications for important messages
5. **Add CloudWatch Metrics** - Send custom metrics to CloudWatch

## Resources

- [Azure WebJobs SDK Documentation](https://docs.microsoft.com/en-us/azure/app-service/webjobs-sdk-how-to)
- [AWS SQS Documentation](https://docs.aws.amazon.com/sqs/)
- [CloudNimble WebJobs Extensions](https://github.com/CloudNimble/WebJobs.Extensions)