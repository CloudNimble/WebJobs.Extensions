// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Amazon.SQS;
using CloudNimble.EasyAF.Core;
using CloudNimble.WebJobs.Extensions.Amazon.SQS.Triggers;
using CloudNimble.WebJobs.Extensions.Common;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.Config
{

    /// <summary>
    /// 
    /// </summary>
    [Extension(AmazonConstants.SQSExtensionName, AmazonConstants.SQSConfigSectionName)]
    internal partial class SQSExtensionConfigProvider : IExtensionConfigProvider
    {

        #region Private Members

        private readonly IContextGetter<IMessageEnqueuedWatcher> _contextGetter;
        private readonly AmazonSQSClient _amazonSQSClient;
        private readonly SQSTriggerAttributeBindingProvider _triggerProvider;
        private readonly QueueMessageCausalityManager _causalityManager;

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="amazonSQSClient"></param>
        /// <param name="contextGetter"></param>
        /// <param name="triggerProvider"></param>
        /// <param name="causalityManager"></param>
        public SQSExtensionConfigProvider(
            AmazonSQSClient amazonSQSClient,
            IContextGetter<IMessageEnqueuedWatcher> contextGetter,
            SQSTriggerAttributeBindingProvider triggerProvider,
            QueueMessageCausalityManager causalityManager)
        {
            _contextGetter = contextGetter;
            _amazonSQSClient = amazonSQSClient;
            _triggerProvider = triggerProvider;
            _causalityManager = causalityManager;

        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public void Initialize(ExtensionConfigContext context)
        {
            Ensure.ArgumentNotNull(context, nameof(context));

            context.AddBindingRule<SQSTriggerAttribute>().BindToTrigger(_triggerProvider);

            var config = new PerHostConfig();
            config.Initialize(context, _amazonSQSClient, _contextGetter, _causalityManager);
        }

        #endregion

    }

}
