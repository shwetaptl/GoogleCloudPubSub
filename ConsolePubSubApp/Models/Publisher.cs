using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Threading.Tasks;

namespace ConsolePubSubApp.Models
{
    public class Publisher
    {
        private readonly IConfiguration _config;

        public Publisher(IConfiguration config)
        {
            _config = config;
        }

        public async Task Publish()
        {
            var projectId = _config["ProjectId"];
            var topicId   = _config["TopicId"];
            var topicName = new TopicName(projectId, topicId);

            var apiClient = PublisherServiceApiClient.Create();
            try
            {
                apiClient.CreateTopic(topicName);
                Log.Information("Created topic {TopicId}", topicId);
            }
            catch (RpcException e) when (e.Status.StatusCode == StatusCode.AlreadyExists)
            {
                Log.Information("Topic {TopicId} already exists", topicId);
            }

            var client = await PublisherClient.CreateAsync(topicName);

            // Publish a batch of domain events that share a correlation ID,
            // simulating a real order lifecycle flowing through the system.
            var events = new[]
            {
                ("OrderPlaced",     "Customer placed order #1001 for 3 items totalling $149.99"),
                ("OrderUpdated",    "Order #1001 quantity revised to 5 items"),
                ("PaymentCaptured", "Payment of $149.99 captured for order #1001"),
                ("OrderShipped",    "Order #1001 dispatched via courier, tracking TRK-9988"),
                ("OrderDelivered",  "Order #1001 delivered successfully"),
            };

            const string correlationId = "corr-order-1001";

            foreach (var (eventType, payload) in events)
            {
                var data = new Data
                {
                    MessageId     = Guid.NewGuid().ToString(),
                    CorrelationId = correlationId,
                    Timestamp     = DateTimeOffset.UtcNow,
                    EventType     = eventType,
                    Payload       = payload,
                };

                var pubsubMessage = new PubsubMessage
                {
                    Data = ByteString.CopyFromUtf8(JsonConvert.SerializeObject(data)),
                    Attributes =
                    {
                        { "source",    "ConsolePubSubApp" },
                        { "eventType", eventType },
                        { "version",   "1.0" },
                    },
                };

                var serverId = await client.PublishAsync(pubsubMessage);
                Log.Information(
                    "Published {EventType} | correlationId={CorrelationId} messageId={MessageId} serverId={ServerId}",
                    eventType, correlationId, data.MessageId, serverId);
            }

            await client.ShutdownAsync(TimeSpan.FromSeconds(15));
            Log.Information("Publisher shut down cleanly after {Count} messages", events.Length);
        }
    }
}
