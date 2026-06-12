using Google.Cloud.PubSub.V1;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConsolePubSubApp.Models
{
    public class Subscriber
    {
        private readonly IConfiguration _config;

        public Subscriber(IConfiguration config)
        {
            _config = config;
        }

        public async Task Subscription()
        {
            var projectId      = _config["ProjectId"];
            var subscriptionId = _config["SubScriptionId"];
            var topicId        = _config["TopicId"];

            var subscriptionName = new SubscriptionName(projectId, subscriptionId);
            var topicName        = new TopicName(projectId, topicId);

            var apiClient = SubscriberServiceApiClient.Create();
            try
            {
                apiClient.CreateSubscription(subscriptionName, topicName, pushConfig: null, ackDeadlineSeconds: 60);
                Log.Information("Created subscription {SubscriptionId}", subscriptionId);
            }
            catch (RpcException e) when (e.Status.StatusCode == StatusCode.AlreadyExists)
            {
                Log.Information("Subscription {SubscriptionId} already exists", subscriptionId);
            }

            var client = await SubscriberClient.CreateAsync(subscriptionName);

            const int expectedCount = 5;
            int received = 0;

            Log.Information("Subscriber started — waiting for {Expected} messages", expectedCount);

            await client.StartAsync((message, cancellationToken) =>
            {
                try
                {
                    var data = JsonConvert.DeserializeObject<Data>(message.Data.ToStringUtf8());

                    message.Attributes.TryGetValue("source", out var source);
                    message.Attributes.TryGetValue("version", out var version);

                    var count = Interlocked.Increment(ref received);
                    Log.Information(
                        "Received [{Count}/{Expected}] {EventType} | correlationId={CorrelationId} source={Source} v={Version} publishedAt={Timestamp}",
                        count, expectedCount,
                        data.EventType, data.CorrelationId,
                        source ?? "unknown", version ?? "unknown",
                        data.Timestamp);

                    if (count >= expectedCount)
                        _ = client.StopAsync(TimeSpan.FromSeconds(5));

                    return Task.FromResult(SubscriberClient.Reply.Ack);
                }
                catch (JsonException ex)
                {
                    // Nack so the message is redelivered rather than silently lost.
                    Log.Error(ex, "Failed to deserialize message {MessageId} — sending Nack for redelivery. Raw: {Raw}",
                        message.MessageId, message.Data.ToStringUtf8());
                    return Task.FromResult(SubscriberClient.Reply.Nack);
                }
            });

            Log.Information("Subscriber stopped after {Count} messages", received);
        }
    }
}
