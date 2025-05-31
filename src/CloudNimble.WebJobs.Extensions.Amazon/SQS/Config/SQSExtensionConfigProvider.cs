// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Amazon.SQS;
using CloudNimble.EasyAF.Core;
using CloudNimble.WebJobs.Extensions.Amazon.SQS.Triggers;
using CloudNimble.WebJobs.Extensions.Common;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.Config
{

    /// <summary>
    /// Extension configuration provider for Amazon SQS integration with Azure WebJobs SDK.
    /// Handles the registration and configuration of SQS triggers and bindings.
    /// </summary>
    [Extension(AmazonConstants.SQSExtensionName, AmazonConstants.SQSConfigSectionName)]
    internal partial class SQSExtensionConfigProvider : IExtensionConfigProvider
    {

        #region Private Members

        private readonly IContextGetter<IMessageEnqueuedWatcher> _contextGetter;
        private readonly AmazonSQSClient _amazonSQSClient;
        private readonly SQSTriggerAttributeBindingProvider _triggerProvider;
        private readonly QueueMessageCausalityManager _causalityManager;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IOptions<SQSOptions> _sqsOptions;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SQSExtensionConfigProvider"/> class.
        /// </summary>
        /// <param name="amazonSQSClient">The Amazon SQS client for queue operations.</param>
        /// <param name="contextGetter">The context getter for message enqueued watchers.</param>
        /// <param name="triggerProvider">The trigger attribute binding provider for SQS triggers.</param>
        /// <param name="causalityManager">The causality manager for tracking message relationships.</param>
        /// <param name="loggerFactory">The logger factory for creating loggers.</param>
        /// <param name="sqsOptions">The SQS-specific configuration options.</param>
        /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
        public SQSExtensionConfigProvider(
            AmazonSQSClient amazonSQSClient,
            IContextGetter<IMessageEnqueuedWatcher> contextGetter,
            SQSTriggerAttributeBindingProvider triggerProvider,
            QueueMessageCausalityManager causalityManager,
            ILoggerFactory loggerFactory,
            IOptions<SQSOptions> sqsOptions)
        {
            _contextGetter = contextGetter;
            _amazonSQSClient = amazonSQSClient;
            _triggerProvider = triggerProvider;
            _causalityManager = causalityManager;
            _loggerFactory = loggerFactory;
            _sqsOptions = sqsOptions;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the SQS extension configuration by setting up trigger bindings and queue operations.
        /// </summary>
        /// <param name="context">The extension configuration context.</param>
        /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
        /// <example>
        /// <code>
        /// // This method is called automatically by the WebJobs runtime during startup
        /// // to configure SQS triggers and bindings for the application.
        /// </code>
        /// </example>
        public void Initialize(ExtensionConfigContext context)
        {
            Ensure.ArgumentNotNull(context, nameof(context));

            // Register SQS trigger binding provider for handling [SQSTrigger] attributes
            context.AddBindingRule<SQSTriggerAttribute>().BindToTrigger(_triggerProvider);

            // Initialize per-host configuration for queue operations and message conversion
            var config = new PerHostConfig();
            config.Initialize(context, _amazonSQSClient, _contextGetter, _causalityManager, _loggerFactory, _sqsOptions);
        }

        #endregion

    }

}