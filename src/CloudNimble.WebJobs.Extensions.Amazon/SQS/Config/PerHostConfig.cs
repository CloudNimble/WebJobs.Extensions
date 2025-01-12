using Amazon.SQS;
using CloudNimble.WebJobs.Extensions.Common;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
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
    internal class PerHostConfig : IConverter<SQSAttribute, IAsyncCollector<SQSMessage>>
    {

        #region Private Fields

        // Fields that the various binding funcs need to close over. 
        private AmazonSQSClient _amazonSQSClient;

        // Optimization where a queue output can directly trigger a queue input. 
        // This is per-host (not per-config)
        private IContextGetter<IMessageEnqueuedWatcher> _messageEnqueuedWatcherGetter;
        private QueueMessageCausalityManager _causalityManager;


        #endregion

        public void Initialize(
            ExtensionConfigContext context,
            AmazonSQSClient amazonSQSClient,
            IContextGetter<IMessageEnqueuedWatcher> contextGetter,
            QueueMessageCausalityManager causalityManager)
        {
            _amazonSQSClient = amazonSQSClient;
            _messageEnqueuedWatcherGetter = contextGetter;
            _causalityManager = causalityManager;

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

        }

        private async Task<object> ConvertPocoToSQSMessage(object arg, Attribute attrResolved, ValueBindingContext context)
        {
            var attr = (SQSAttribute)attrResolved;
            var jobj = SerializeToJsonObject(arg, attr, context);
            var msg = ConvertJsonObjectToSQSMessage(jobj, attr);
            return await Task.FromResult(msg);
        }

        private SQSMessage ConvertJsonObjectToSQSMessage(JsonObject obj, SQSAttribute attrResolved)
        {
            var json = obj.ToString(); // convert to JSon
            return ConvertStringToSQSMessage(json, attrResolved);
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
        private static string NormalizeQueueName(SQSAttribute attribute, INameResolver nameResolver)
        {
            string queueName = attribute.QueueName;
            if (nameResolver != null)
            {
                queueName = nameResolver.ResolveWholeString(queueName);
            }
            queueName = queueName.ToLowerInvariant(); // must be lowercase. coerce here to be nice.
            return queueName;
        }

        // This is a static validation (so only %% are resolved; not {} ) 
        // For runtime validation, the regular builder functions can do the resolution.
        private void ValidateQueueAttribute(SQSAttribute attribute, Type parameterType)
        {
            string queueName = NormalizeQueueName(attribute, null);

            // Queue pre-existing behavior: if there are { }in the path, then defer validation until runtime. 
            if (!queueName.Contains('{'))
            {
                SQSQueue.ValidateQueueName(queueName);
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

        public IAsyncCollector<SQSMessage> Convert(SQSAttribute attrResolved)
        {
            var queue = GetQueue(attrResolved);
            return new SQSMessageAsyncCollector(queue, _messageEnqueuedWatcherGetter.Value);
        }

        internal SQSQueue GetQueue(SQSAttribute attrResolved)
        {
            //var account = _amazonSQSClient.ListQueuesAsync(attrResolved.Connection);
            //var client = account.CreateCloudQueueClient();

            string queueName = attrResolved.QueueName.ToLowerInvariant();
            SQSQueue.ValidateQueueName(queueName);

            //return client.GetQueueReference(queueName);

            return new SQSQueue(queueName, _amazonSQSClient, null);
        }
    }
}
