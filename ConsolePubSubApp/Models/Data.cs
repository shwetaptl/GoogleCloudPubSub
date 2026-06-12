using System;

namespace ConsolePubSubApp.Models
{
    public class Data
    {
        public string MessageId { get; set; }
        public string CorrelationId { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string EventType { get; set; }
        public string Payload { get; set; }
    }
}
