using System;
using System.Collections.Generic;

namespace CloudNimble.WebJobs.Extensions.Examples.Models
{
    /// <summary>
    /// Example message model for demonstrating SQS message processing.
    /// </summary>
    public class ExampleMessage
    {
        /// <summary>
        /// Gets or sets the unique identifier for this message.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the timestamp when this message was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the message content.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the message type for routing purposes.
        /// </summary>
        public string MessageType { get; set; } = "ExampleMessage";

        /// <summary>
        /// Gets or sets the priority of the message.
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Gets or sets additional metadata for the message.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}