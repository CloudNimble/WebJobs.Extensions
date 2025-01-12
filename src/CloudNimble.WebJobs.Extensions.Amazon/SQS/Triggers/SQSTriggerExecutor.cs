// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.EasyAF.Core;
using CloudNimble.WebJobs.Extensions.Common;
using CloudNimble.WebJobs.Extensions.Common.Listeners;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using Microsoft.Azure.WebJobs.Host.Executors;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.Triggers
{

    /// <summary>
    /// 
    /// </summary>
    internal class SQSTriggerExecutor : ITriggerExecutor<SQSMessage>
    {

        #region Private Members

        private readonly ITriggeredFunctionExecutor _innerExecutor;
        private readonly QueueMessageCausalityManager _causalityManager;

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="innerExecutor"></param>
        /// <param name="causalityManager"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public SQSTriggerExecutor(ITriggeredFunctionExecutor innerExecutor, QueueMessageCausalityManager causalityManager)
        {
            _innerExecutor = innerExecutor ?? throw new ArgumentNullException(nameof(innerExecutor));
            _causalityManager = causalityManager;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sqsMessage"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<FunctionResult> ExecuteAsync(SQSMessage sqsMessage, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new FunctionResult(false);
            }

            Ensure.ArgumentNotNull(sqsMessage, nameof(sqsMessage));

            var parentId = _causalityManager.GetOwner(sqsMessage);
            var triggerDetails = new Dictionary<string, string>()
            {
                { "MessageId", sqsMessage.Id },
                { "DequeueCount", sqsMessage.DequeueCount.ToString() },
                { "InsertionTime", sqsMessage.DateInserted?.ToString(Constants.DateTimeFormatString) }
            };

            if (sqsMessage.Original.Attributes.TryGetValue("MessageGroupId", out var messageGroupId))
            {
                triggerDetails.Add("MessageGroupId", messageGroupId);
            }
            if (sqsMessage.Original.Attributes.TryGetValue("MessageDeduplicationId", out var messageAttribute))
            {
                triggerDetails.Add("MessageDeduplicationId", messageGroupId);
            }

            var input = new TriggeredFunctionData
            {
                ParentId = parentId,
                TriggerValue = sqsMessage,
                TriggerDetails = triggerDetails
            };

            return await _innerExecutor.TryExecuteAsync(input, cancellationToken);
        }

        #endregion

    }

}
