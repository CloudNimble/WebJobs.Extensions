using CloudNimble.EasyAF.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Text.Json.Nodes;

namespace CloudNimble.WebJobs.Extensions.Common.Queues
{

    /// <summary>
    /// Manages the causality of queue messages by setting and retrieving the owner of the message.
    /// </summary>
    public class QueueMessageCausalityManager
    {
        private const string ParentGuidFieldName = "$AzureWebJobsParentId";

        private readonly ILogger<QueueMessageCausalityManager> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueMessageCausalityManager"/> class.
        /// </summary>
        /// <param name="loggerFactory"></param>
        public QueueMessageCausalityManager(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<QueueMessageCausalityManager>();
        }

        /// <summary>
        /// Sets the owner of the queue message.
        /// </summary>
        /// <param name="functionOwner">The GUID of the function owner.</param>
        /// <param name="token">The JSON object representing the queue message.</param>
        public static void SetOwner(Guid functionOwner, JsonObject token)
        {
            Ensure.ArgumentNotNull(token, nameof(token));

            if (Equals(Guid.Empty, functionOwner))
            {
                return;
            }

            token[ParentGuidFieldName] = functionOwner.ToString();
        }

        /// <summary>
        /// Gets the owner of the queue message.
        /// </summary>
        /// <param name="msg">The queue message.</param>
        /// <returns>The GUID of the owner if found; otherwise, null.</returns>
        [DebuggerNonUserCode]
        public Guid? GetOwner(IQueueMessage msg)
        {
            if (string.IsNullOrWhiteSpace(msg.Body)) return null;

            JsonObject json;
            try
            {
                json = JsonNode.Parse(msg.Body)?.AsObject();
            }
            catch (Exception)
            {
                return null;
            }

            if (json is null || !json.TryGetPropertyValue(ParentGuidFieldName, out JsonNode value) || value is not JsonValue jsonValue || jsonValue.TryGetValue(out string val))
            {
                return null;
            }

            if (Guid.TryParse(val, out Guid guid))
            {
                return guid;
            }
            return null;
        }

    }

}
