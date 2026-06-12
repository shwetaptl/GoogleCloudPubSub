# Google Cloud Pub/Sub — C# Demo

A **.NET 8** console application demonstrating publish/subscribe messaging on **Google Cloud Pub/Sub** with service account authentication.

This project is a companion to [QueueBacklogIntelligence](https://github.com/shwetaptl/QueueBacklogIntelligence), a production-grade Azure Service Bus monitoring system. Together they demonstrate cross-cloud messaging expertise — the same pub/sub pattern applied on both **Google Cloud** and **Azure**.

---

## What it demonstrates

| Concept | Implementation |
|---------|---------------|
| Publisher / Subscriber pattern | Separate `Publisher` and `Subscriber` classes wired via DI |
| Service account authentication | `GOOGLE_APPLICATION_CREDENTIALS` env var set from config |
| Batch publishing | 5 domain events published per run (OrderPlaced → OrderDelivered) |
| Message attributes | `source`, `eventType`, `version` attached to every message |
| Nack on failure | Deserialization errors return `Nack`; malformed messages are redelivered, not silently lost |
| Graceful shutdown | Subscriber stops cleanly once all expected messages are received |
| Structured logging | Serilog with named properties, console + rolling-file sinks |
| External configuration | `appsettings.json` + `Microsoft.Extensions.Configuration` |
| Dependency injection | `Microsoft.Extensions.DependencyInjection` in a console app |

---

## Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│  ConsolePubSubApp                                                │
│                                                                  │
│  ┌───────────┐   5 messages    ┌─────────────┐   pull    ┌────────────┐ │
│  │ Publisher │ ──────────────► │  Pub/Sub    │ ────────► │ Subscriber │ │
│  └───────────┘  + attributes   │    Topic    │           └────────────┘ │
│                                └─────────────┘                   │
│                                      │                           │
│                                      ▼                           │
│                               ┌─────────────┐                   │
│                               │Subscription │  Ack valid msgs    │
│                               │             │  Nack malformed    │
│                               └─────────────┘  Stop after N      │
└──────────────────────────────────────────────────────────────────┘
```

Each published message carries:
- **Body** — JSON-serialized `Data` object with `MessageId`, `CorrelationId`, `Timestamp`, `EventType`, `Payload`
- **Attributes** — `source`, `eventType`, `version` as key-value metadata

The subscriber reads both body and attributes, logs them with structured fields, and shuts down gracefully after receiving the expected number of messages.

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- A Google Cloud project with the **Pub/Sub API** enabled
- A service account with the **Pub/Sub Editor** role
- A service account JSON key file downloaded locally

---

## Setup

**1. Clone**
```bash
git clone https://github.com/shwetaptl/GoogleCloudPubSub.git
cd GoogleCloudPubSub
```

**2. Configure `appsettings.json`**

Edit `ConsolePubSubApp/appsettings.json`:
```json
{
  "GOOGLE_APPLICATION_CREDENTIALS": "/path/to/service-account-key.json",
  "ProjectId": "your-gcp-project-id",
  "TopicId": "your-topic-id",
  "SubScriptionId": "your-subscription-id"
}
```

The topic and subscription are created automatically on first run if they don't exist.

**3. Run**
```bash
cd ConsolePubSubApp
dotnet run
```

---

## Sample output

```
[12:00:01 INF] Created topic orders-topic
[12:00:01 INF] Published OrderPlaced     | correlationId=corr-order-1001 messageId=a1b2c3... serverId=8472...
[12:00:01 INF] Published OrderUpdated    | correlationId=corr-order-1001 messageId=d4e5f6... serverId=8473...
[12:00:01 INF] Published PaymentCaptured | correlationId=corr-order-1001 messageId=g7h8i9... serverId=8474...
[12:00:01 INF] Published OrderShipped    | correlationId=corr-order-1001 messageId=j0k1l2... serverId=8475...
[12:00:01 INF] Published OrderDelivered  | correlationId=corr-order-1001 messageId=m3n4o5... serverId=8476...
[12:00:01 INF] Publisher shut down cleanly after 5 messages
[12:00:02 INF] Subscriber started — waiting for 5 messages
[12:00:02 INF] Received [1/5] OrderPlaced     | correlationId=corr-order-1001 source=ConsolePubSubApp v=1.0 publishedAt=...
[12:00:02 INF] Received [2/5] OrderUpdated    | correlationId=corr-order-1001 source=ConsolePubSubApp v=1.0 publishedAt=...
[12:00:02 INF] Received [3/5] PaymentCaptured | correlationId=corr-order-1001 source=ConsolePubSubApp v=1.0 publishedAt=...
[12:00:02 INF] Received [4/5] OrderShipped    | correlationId=corr-order-1001 source=ConsolePubSubApp v=1.0 publishedAt=...
[12:00:02 INF] Received [5/5] OrderDelivered  | correlationId=corr-order-1001 source=ConsolePubSubApp v=1.0 publishedAt=...
[12:00:03 INF] Subscriber stopped after 5 messages
```

Logs are also written to `app<date>.log` in the run directory.

---

## Project structure

```
GoogleCloudPubSub/
├── ConsolePubSubApp/
│   ├── Models/
│   │   ├── Data.cs         # Message contract (EventType, CorrelationId, Timestamp, Payload)
│   │   ├── Publisher.cs    # Batch-publishes messages with attributes
│   │   └── Subscriber.cs   # Pulls, Acks/Nacks, and shuts down gracefully
│   ├── Program.cs          # DI wiring, configuration, Serilog setup
│   └── appsettings.json    # GCP credentials path + project/topic/subscription IDs
└── ConsolePubSubApp.sln
```

---

## Related project

**[QueueBacklogIntelligence](https://github.com/shwetaptl/QueueBacklogIntelligence)** — Production Azure Service Bus monitoring system with React dashboard, Docker Compose, SLA breach detection, root-cause classification, and Microsoft Teams alerting.

The two projects cover the same pub/sub pattern on different cloud platforms, showing that messaging fundamentals — producers, consumers, acknowledgements, dead-lettering — translate across Google Cloud and Azure.
