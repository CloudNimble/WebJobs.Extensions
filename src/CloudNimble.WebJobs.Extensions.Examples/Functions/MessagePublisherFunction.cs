using CloudNimble.WebJobs.Extensions.Amazon.SQS;
using CloudNimble.WebJobs.Extensions.Examples.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Examples.Functions
{
    /// <summary>
    /// Timer-triggered function that publishes messages to an SQS queue on a schedule.
    /// </summary>
    public class MessagePublisherFunction
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MessagePublisherFunction> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePublisherFunction"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="logger">The logger instance for logging operations.</param>
        public MessagePublisherFunction(
            IConfiguration configuration,
            ILogger<MessagePublisherFunction> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Publishes a message to SQS queue every X seconds (configurable).
        /// Default is 30 seconds, but can be overridden in configuration.
        /// Uses a cron expression: "*/30 * * * * *" = every 30 seconds
        /// </summary>
        [FunctionName("PublishMessageToSQS")]
        public async Task PublishMessage(
            [TimerTrigger("*/30 * * * * *")] TimerInfo timer,
            [SQSOutput("%SQS:QueueName%")] IAsyncCollector<ExampleMessage> messageCollector,
            ILogger log)
        {
            log.LogInformation("PublishMessage function started. Timer IsPastDue: {IsPastDue}", timer.IsPastDue);
            try
            {
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

                // Add message to the queue using the output binding
                log.LogInformation("Adding message to SQS collector...");
                await messageCollector.AddAsync(message);
                log.LogInformation("Message added to SQS collector successfully");

                log.LogInformation(
                    "Successfully published message to SQS. Content: {Content}",
                    message.Content);

                // Log metrics
                _logger.LogMetric("MessagesPublished", 1, new Dictionary<string, object>
                {
                    { "Queue", _configuration["SQS:QueueName"] ?? "webjobs-example-queue" },
                    { "MessageType", message.MessageType },
                    { "Priority", message.Priority }
                });
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to publish message to SQS queue");
                throw;
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