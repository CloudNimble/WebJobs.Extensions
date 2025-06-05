// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS
{
    /// <summary>
    /// SQS-specific name resolver that handles app settings resolution and queue name normalization.
    /// Follows the Azure WebJobs INameResolver pattern for consistent configuration handling.
    /// </summary>
    public class SQSNameResolver : INameResolver
    {
        private readonly IConfiguration _configuration;
        private readonly IOptions<SQSOptions> _sqsOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="SQSNameResolver"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="sqsOptions">The SQS configuration options.</param>
        public SQSNameResolver(IConfiguration configuration, IOptions<SQSOptions> sqsOptions)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _sqsOptions = sqsOptions ?? throw new ArgumentNullException(nameof(sqsOptions));
        }

        /// <summary>
        /// Resolves the whole string, handling %AppSetting% placeholders and applying SQS queue name normalization.
        /// </summary>
        /// <param name="name">The name to resolve.</param>
        /// <returns>The resolved and normalized name.</returns>
        public string ResolveWholeString(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            // Handle %AppSetting% style placeholders
            var resolvedName = ResolveConfigurationPlaceholders(name);

            // Apply SQS-specific normalization
            return NormalizeQueueName(resolvedName);
        }

        /// <summary>
        /// Resolves a single configuration key.
        /// </summary>
        /// <param name="name">The configuration key to resolve.</param>
        /// <returns>The resolved value.</returns>
        public string Resolve(string name)
        {
            return _configuration[name];
        }

        /// <summary>
        /// Resolves configuration placeholders in the format %ConfigKey% or %Section:Key%.
        /// </summary>
        /// <param name="value">The value containing placeholders to resolve.</param>
        /// <returns>The value with placeholders resolved.</returns>
        private string ResolveConfigurationPlaceholders(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            // Handle %ConfigKey% pattern
            if (value.StartsWith('%') && value.EndsWith('%') && value.Length > 2)
            {
                var configKey = value.Substring(1, value.Length - 2);
                var configValue = _configuration[configKey];
                return configValue ?? value; // Return original if not found
            }

            return value;
        }

        /// <summary>
        /// Normalizes a queue name according to SQS requirements and configuration options.
        /// </summary>
        /// <param name="queueName">The queue name to normalize.</param>
        /// <returns>The normalized queue name.</returns>
        private string NormalizeQueueName(string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                return queueName;
            }

            // Convert to lowercase as SQS queue names must be lowercase
            queueName = queueName.ToLowerInvariant();

            // Handle FIFO queue naming
            if (_sqsOptions?.Value?.UseFifo == true && !queueName.EndsWith(".fifo", StringComparison.OrdinalIgnoreCase))
            {
                queueName += ".fifo";
            }

            return queueName;
        }
    }
}