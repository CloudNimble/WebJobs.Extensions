using CloudNimble.WebJobs.Extensions.Amazon.SQS;
using CloudNimble.WebJobs.Extensions.Examples.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Examples.Functions
{
    /// <summary>
    /// SQS-triggered function that processes messages from the queue.
    /// </summary>
    public class MessageProcessorFunction
    {
        private readonly ILogger<MessageProcessorFunction> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageProcessorFunction"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for logging operations.</param>
        public MessageProcessorFunction(ILogger<MessageProcessorFunction> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Processes messages from SQS queue using the raw SQSMessage type.
        /// </summary>
        [FunctionName("ProcessSQSMessage")]
        public async Task ProcessMessage(
            [SQSTrigger("%SQS:QueueName%")] SQSMessage message,
            ILogger log)
        {
            log.LogInformation(
                "Processing SQS message. MessageId: {MessageId}, DequeueCount: {DequeueCount}",
                message.Id,
                message.DequeueCount);

            try
            {
                // Log raw message details
                log.LogDebug("Raw message body: {MessageBody}", message.Body);

                // Try to deserialize as ExampleMessage
                ExampleMessage exampleMessage = null;
                try
                {
                    exampleMessage = JsonSerializer.Deserialize<ExampleMessage>(message.Body);
                }
                catch (JsonException ex)
                {
                    log.LogWarning(ex, "Message body is not a valid ExampleMessage. Processing as plain text.");
                }

                if (exampleMessage != null)
                {
                    // Process structured message
                    await ProcessExampleMessage(exampleMessage, message, log);
                }
                else
                {
                    // Process plain text message
                    await ProcessPlainTextMessage(message.Body, message, log);
                }

                // Simulate some processing time
                await Task.Delay(TimeSpan.FromSeconds(1));

                log.LogInformation(
                    "Successfully processed message {MessageId}",
                    message.Id);
            }
            catch (Exception ex)
            {
                log.LogError(ex, 
                    "Failed to process message {MessageId}. It will be retried or moved to poison queue.",
                    message.Id);
                throw; // Re-throw to let the framework handle retry/poison logic
            }
        }

        /// <summary>
        /// Alternative function that processes messages as strings.
        /// </summary>
        [FunctionName("ProcessSQSMessageAsString")]
        public void ProcessMessageAsString(
            [SQSTrigger("%SQS:QueueName%")] string messageContent,
            ILogger log)
        {
            log.LogInformation("Received message as string: {MessageContent}", messageContent);
            
            // Process the string content
            if (messageContent.Contains("error", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Message contains 'error' keyword");
            }
        }

        /// <summary>
        /// Alternative function that processes messages as strongly-typed objects.
        /// </summary>
        [FunctionName("ProcessTypedSQSMessage")]
        public async Task ProcessTypedMessage(
            [SQSTrigger("%SQS:QueueName%")] ExampleMessage message,
            ILogger log)
        {
            log.LogInformation(
                "Processing typed message. Id: {Id}, Type: {Type}, Priority: {Priority}",
                message.Id,
                message.MessageType,
                message.Priority);

            // Process based on message type
            switch (message.MessageType)
            {
                case "ScheduledMessage":
                    await HandleScheduledMessage(message, log);
                    break;
                case "UserMessage":
                    await HandleUserMessage(message, log);
                    break;
                default:
                    log.LogWarning("Unknown message type: {MessageType}", message.MessageType);
                    break;
            }
        }

        private async Task ProcessExampleMessage(ExampleMessage message, SQSMessage sqsMessage, ILogger log)
        {
            log.LogInformation(
                "Processing ExampleMessage. Type: {MessageType}, Priority: {Priority}, Content: {Content}",
                message.MessageType,
                message.Priority,
                message.Content);

            // Log metadata
            foreach (var kvp in message.Metadata)
            {
                log.LogDebug("Metadata - {Key}: {Value}", kvp.Key, kvp.Value);
            }

            // Simulate processing based on priority
            var processingTime = TimeSpan.FromSeconds(6 - message.Priority); // Higher priority = less time
            await Task.Delay(processingTime);

            // Record metrics
            _logger.LogMetric("MessagesProcessed", 1, new System.Collections.Generic.Dictionary<string, object>
            {
                { "MessageType", message.MessageType },
                { "Priority", message.Priority },
                { "ProcessingTimeMs", processingTime.TotalMilliseconds }
            });
        }

        private async Task ProcessPlainTextMessage(string content, SQSMessage sqsMessage, ILogger log)
        {
            log.LogInformation("Processing plain text message: {Content}", content);
            
            // Simple processing logic
            if (content.Length > 100)
            {
                log.LogWarning("Large message detected. Length: {Length}", content.Length);
            }

            await Task.Delay(TimeSpan.FromSeconds(0.5));
        }

        private async Task HandleScheduledMessage(ExampleMessage message, ILogger log)
        {
            log.LogInformation("Handling scheduled message created at {CreatedAt}", message.CreatedAt);
            
            // Check if message is stale
            var age = DateTime.UtcNow - message.CreatedAt;
            if (age > TimeSpan.FromMinutes(5))
            {
                log.LogWarning("Scheduled message is {AgeMinutes} minutes old", age.TotalMinutes);
            }

            await Task.CompletedTask;
        }

        private async Task HandleUserMessage(ExampleMessage message, ILogger log)
        {
            log.LogInformation("Handling user message: {Content}", message.Content);
            
            // Simulate user message processing
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}