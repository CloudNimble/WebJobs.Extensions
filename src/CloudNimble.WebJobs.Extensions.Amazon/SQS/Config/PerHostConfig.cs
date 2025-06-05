// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Amazon.SQS;
using CloudNimble.WebJobs.Extensions.Common;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.Config
{

    // $$$ Get rid of PerHostConfig part?  
    // Multiple JobHost objects may share the same JobHostConfiguration.
    // But queues have per-host instance state (IMessageEnqueuedWatcher). 
    // so capture that and create new binding rules per host instance. 
    internal class PerHostConfig : IConverter<SQSAttribute, IAsyncCollector<SQSMessage>>,
                                    IConverter<SQSOutputAttribute, IAsyncCollector<SQSMessage>>
    {

        #region Private Fields

        // Fields that the various binding funcs need to close over. 
        private IAmazonSQS _amazonSQSClient;
        private ILoggerFactory _loggerFactory;
        private IOptions<SQSOptions> _sqsOptions;

        // Optimization where a queue output can directly trigger a queue input. 
        // This is per-host (not per-config)
        private IContextGetter<IMessageEnqueuedWatcher> _messageEnqueuedWatcherGetter;
        private QueueMessageCausalityManager _causalityManager;
        private Microsoft.Azure.WebJobs.INameResolver _nameResolver;

        #endregion

        public void Initialize(
            ExtensionConfigContext context,
            IAmazonSQS amazonSQSClient,
            IContextGetter<IMessageEnqueuedWatcher> contextGetter,
            QueueMessageCausalityManager causalityManager,
            ILoggerFactory loggerFactory,
            IOptions<SQSOptions> sqsOptions,
            Microsoft.Azure.WebJobs.INameResolver nameResolver)
        {
            _amazonSQSClient = amazonSQSClient;
            _messageEnqueuedWatcherGetter = contextGetter;
            _causalityManager = causalityManager;
            _loggerFactory = loggerFactory;
            _sqsOptions = sqsOptions;
            _nameResolver = nameResolver;

            // IStorageQueueMessage is the core testing interface 
            var binding = context.AddBindingRule<SQSAttribute>();
            binding
                .AddConverter<string, SQSMessage>(ConvertStringToSQSMessage)
                .AddOpenConverter<OpenType.Poco, SQSMessage>(ConvertPocoToSQSMessage);

            context // global converters, apply to multiple attributes. 
                 .AddConverter<SQSMessage, string>(ConvertSQSMessageToString);

            var builder = new SQSQueueBuilder(this);

            binding.AddValidator(ValidateQueueAttribute);

            binding.BindToCollector<SQSMessage>(this);

            binding.BindToInput<SQSQueue>(builder);

            binding.BindToInput<SQSQueue>(builder);

            // Configure SQSOutputAttribute bindings
            var outputBinding = context.AddBindingRule<SQSOutputAttribute>();
            outputBinding
                .AddConverter<string, SQSMessage>(ConvertStringToSQSMessageForOutput)
                .AddOpenConverter<OpenType.Poco, SQSMessage>(ConvertPocoToSQSMessageForOutput);

            outputBinding.AddValidator(ValidateOutputQueueAttribute);
            outputBinding.BindToCollector<SQSMessage>(this);

        }

        private async Task<object> ConvertPocoToSQSMessage(object arg, Attribute attrResolved, ValueBindingContext context)
        {
            var attr = (SQSAttribute)attrResolved;
            var jobj = SerializeToJsonObject(arg, attr, context);
            var msg = ConvertJsonObjectToSQSMessage(jobj, attr);
            return await Task.FromResult(msg);
        }

        private async Task<object> ConvertPocoToSQSMessageForOutput(object arg, Attribute attrResolved, ValueBindingContext context)
        {
            var attr = (SQSOutputAttribute)attrResolved;
            var jobj = SerializeToJsonObject(arg, attr, context);
            var msg = ConvertJsonObjectToSQSMessageForOutput(jobj, attr);
            return await Task.FromResult(msg);
        }

        private SQSMessage ConvertJsonObjectToSQSMessage(JsonObject obj, SQSAttribute attrResolved)
        {
            var json = obj.ToString(); // convert to JSon
            return ConvertStringToSQSMessage(json, attrResolved);
        }

        private SQSMessage ConvertJsonObjectToSQSMessageForOutput(JsonObject obj, SQSOutputAttribute attrResolved)
        {
            var json = obj.ToString(); // convert to JSon
            return ConvertStringToSQSMessageForOutput(json, attrResolved);
        }

        /// <summary>
        /// Serialize the input object to a JsonObject, and stamp it with a causality marker.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="attrResolved"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static JsonObject SerializeToJsonObject(object input, Attribute attrResolved, ValueBindingContext context)
        {
            var node = JsonNode.Parse(JsonSerializer.Serialize(input)).AsObject();
            QueueMessageCausalityManager.SetOwner(context.FunctionInstanceId, node);

            return node;
        }

        // This is a static validation (so only %% are resolved; not {} ) 
        // For runtime validation, the regular builder functions can do the resolution.
        private void ValidateQueueAttribute(SQSAttribute attribute, Type parameterType)
        {
            string queueName = attribute.QueueName;

            // Queue pre-existing behavior: if there are { }in the path, then defer validation until runtime. 
            if (!queueName.Contains('{'))
            {
                // Note: Queue name normalization will be handled automatically by INameResolver
                // We validate the raw name here since normalization happens at runtime
                SQSQueue.ValidateQueueName(queueName.ToLowerInvariant());
            }
        }

        private void ValidateOutputQueueAttribute(SQSOutputAttribute attribute, Type parameterType)
        {
            string queueName = attribute.QueueName;

            // Queue pre-existing behavior: if there are { }in the path, then defer validation until runtime. 
            if (!queueName.Contains('{'))
            {
                // Note: Queue name normalization will be handled automatically by INameResolver
                // We validate the raw name here since normalization happens at runtime
                SQSQueue.ValidateQueueName(queueName.ToLowerInvariant());
            }
        }

        private string ConvertSQSMessageToString(SQSMessage sqsMessage)
        {
            return sqsMessage.Body;
        }

        private SQSMessage ConvertStringToSQSMessage(string arg, SQSAttribute attrResolved)
        {
            return new SQSMessage(arg);
        }

        private SQSMessage ConvertStringToSQSMessageForOutput(string arg, SQSOutputAttribute attrResolved)
        {
            return new SQSMessage(arg);
        }

        public IAsyncCollector<SQSMessage> Convert(SQSAttribute attrResolved)
        {
            var queue = GetQueue(attrResolved);
            return new SQSMessageAsyncCollector(queue, _messageEnqueuedWatcherGetter.Value);
        }

        public IAsyncCollector<SQSMessage> Convert(SQSOutputAttribute attrResolved)
        {
            var queue = GetOutputQueue(attrResolved);
            return new SQSMessageAsyncCollector(queue, _messageEnqueuedWatcherGetter.Value);
        }

        internal SQSQueue GetQueue(SQSAttribute attrResolved)
        {
            // Resolve the queue name using INameResolver if available
            string queueName = attrResolved.QueueName;
            if (_nameResolver != null && !string.IsNullOrEmpty(queueName))
            {
                queueName = _nameResolver.ResolveWholeString(queueName);
            }
            SQSQueue.ValidateQueueName(queueName);
            return new SQSQueue(queueName, _amazonSQSClient, _loggerFactory, _sqsOptions);
        }

        internal SQSQueue GetOutputQueue(SQSOutputAttribute attrResolved)
        {
            // Resolve the queue name using INameResolver if available
            string queueName = attrResolved.QueueName;
            if (_nameResolver != null && !string.IsNullOrEmpty(queueName))
            {
                queueName = _nameResolver.ResolveWholeString(queueName);
            }
            SQSQueue.ValidateQueueName(queueName);
            return new SQSQueue(queueName, _amazonSQSClient, _loggerFactory, _sqsOptions);
        }
    }
}