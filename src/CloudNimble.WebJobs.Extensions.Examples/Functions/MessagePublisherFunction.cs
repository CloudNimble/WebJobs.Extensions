using Amazon.SQS;
using Amazon.SQS.Model;
using CloudNimble.WebJobs.Extensions.Examples.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Examples.Functions
{
    /// <summary>
    /// Timer-triggered function that publishes messages to an SQS queue on a schedule.
    /// </summary>
    public class MessagePublisherFunction
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MessagePublisherFunction> _logger;
        private readonly string _queueName;
        private readonly bool _useFifo;
        private readonly string _messageGroupId;
        private readonly bool _useContentBasedDeduplication;
        private string _queueUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePublisherFunction"/> class.
        /// </summary>
        /// <param name="sqsClient">The Amazon SQS client for queue operations.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="logger">The logger instance for logging operations.</param>
        public MessagePublisherFunction(
            IAmazonSQS sqsClient,
            IConfiguration configuration,
            ILogger<MessagePublisherFunction> logger)
        {
            _sqsClient = sqsClient;
            _configuration = configuration;
            _logger = logger;
            
            var baseQueueName = _configuration["SQS:QueueName"] ?? "webjobs-example-queue";
            _useFifo = _configuration.GetValue<bool>("SQS:UseFifo");
            _messageGroupId = _configuration["SQS:MessageGroupId"] ?? "default";
            _useContentBasedDeduplication = _configuration.GetValue<bool>("SQS:UseContentBasedDeduplication");
            
            // Append .fifo if UseFifo is true and queue name doesn't already end with .fifo
            _queueName = _useFifo && !baseQueueName.EndsWith(".fifo") 
                ? baseQueueName + ".fifo" 
                : baseQueueName;
        }

        /// <summary>
        /// Publishes a message to SQS queue every X seconds (configurable).
        /// Default is 30 seconds, but can be overridden in configuration.
        /// Uses a cron expression: "*/30 * * * * *" = every 30 seconds
        /// </summary>
        [FunctionName("PublishMessageToSQS")]
        public async Task PublishMessage(
            [TimerTrigger("*/30 * * * * *")] TimerInfo timer,
            ILogger log)
        {
            try
            {
                // Ensure queue URL is cached
                if (string.IsNullOrEmpty(_queueUrl))
                {
                    _queueUrl = await GetOrCreateQueueUrlAsync();
                }

                // Create example message
                var message = new ExampleMessage
                {
                    Content = $"Automated message published at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
                    MessageType = "ScheduledMessage",
                    Priority = Random.Shared.Next(1, 5),
                    Metadata = new Dictionary<string, string>
                    {
                        { "Source", "TimerTrigger" },
                        { "Host", Environment.MachineName },
                        { "ScheduleStatus", timer.ScheduleStatus?.ToString() ?? "Unknown" },
                        { "IsPastDue", timer.IsPastDue.ToString() }
                    }
                };

                // Serialize message
                var messageBody = JsonSerializer.Serialize(message, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                // Send message to SQS
                var sendRequest = new SendMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MessageBody = messageBody,
                    MessageAttributes = new Dictionary<string, MessageAttributeValue>
                    {
                        {
                            "MessageType",
                            new MessageAttributeValue
                            {
                                DataType = "String",
                                StringValue = message.MessageType
                            }
                        },
                        {
                            "Priority",
                            new MessageAttributeValue
                            {
                                DataType = "Number",
                                StringValue = message.Priority.ToString()
                            }
                        }
                    }
                };

                // Add FIFO attributes if this is a FIFO queue
                if (_useFifo)
                {
                    sendRequest.MessageGroupId = _messageGroupId;
                    
                    // Generate deduplication ID if content-based deduplication is not enabled
                    if (!_useContentBasedDeduplication)
                    {
                        sendRequest.MessageDeduplicationId = Guid.NewGuid().ToString();
                    }
                }

                var response = await _sqsClient.SendMessageAsync(sendRequest);

                log.LogInformation(
                    "Successfully published message to SQS. MessageId: {MessageId}, Content: {Content}",
                    response.MessageId,
                    message.Content);

                // Log metrics
                _logger.LogMetric("MessagesPublished", 1, new Dictionary<string, object>
                {
                    { "Queue", _queueName },
                    { "MessageType", message.MessageType },
                    { "Priority", message.Priority }
                });
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to publish message to SQS queue {QueueName}", _queueName);
                throw;
            }
        }

        /// <summary>
        /// Gets the queue URL, creating the queue if it doesn't exist.
        /// </summary>
        private async Task<string> GetOrCreateQueueUrlAsync()
        {
            try
            {
                // Try to get existing queue
                var getQueueUrlResponse = await _sqsClient.GetQueueUrlAsync(_queueName);
                _logger.LogInformation("Using existing SQS queue: {QueueUrl}", getQueueUrlResponse.QueueUrl);
                return getQueueUrlResponse.QueueUrl;
            }
            catch (QueueDoesNotExistException)
            {
                // Create queue if it doesn't exist
                _logger.LogInformation("Queue {QueueName} does not exist. Creating it...", _queueName);
                
                var createQueueRequest = new CreateQueueRequest
                {
                    QueueName = _queueName,
                    Attributes = new Dictionary<string, string>
                    {
                        { "VisibilityTimeout", (_configuration.GetValue<int?>("SQS:VisibilityTimeout") ?? 30).ToString() },
                        { "MessageRetentionPeriod", "86400" }, // 1 day
                        { "ReceiveMessageWaitTimeSeconds", "20" } // Long polling
                    }
                };

                // Add FIFO attributes if this is a FIFO queue
                if (_useFifo)
                {
                    createQueueRequest.Attributes["FifoQueue"] = "true";
                    createQueueRequest.Attributes["ContentBasedDeduplication"] = _useContentBasedDeduplication.ToString().ToLowerInvariant();
                }

                var createQueueResponse = await _sqsClient.CreateQueueAsync(createQueueRequest);
                _logger.LogInformation("Created new SQS queue: {QueueUrl}", createQueueResponse.QueueUrl);
                return createQueueResponse.QueueUrl;
            }
        }
    }

    /// <summary>
    /// Extension methods for ILogger to support metrics.
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>
        /// Logs a metric value with optional properties.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="metricName">The name of the metric to log.</param>
        /// <param name="value">The numeric value of the metric.</param>
        /// <param name="properties">Optional properties to include with the metric.</param>
        public static void LogMetric(this ILogger logger, string metricName, double value, IDictionary<string, object> properties = null)
        {
            using (logger.BeginScope(properties ?? new Dictionary<string, object>()))
            {
                logger.LogInformation("Metric: {MetricName} = {MetricValue}", metricName, value);
            }
        }
    }
}