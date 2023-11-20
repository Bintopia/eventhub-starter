using System.Text;

using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

using EventHubStarter.Common.Events;

namespace EventHubStarter.Producer
{

    public interface INotificationProcessor
    {
        Task PublishAsync(RecordChangedEvent @event);
    }

    public class PgNotificationProcessor(EventHubProducerClient client) : INotificationProcessor
    {
        private readonly EventHubProducerClient _producerClient = client;

        public async Task PublishAsync(RecordChangedEvent @event)
        {
            Console.WriteLine("EventHub: publishing event...");

            var numOfEvents = 1;

            // Create a batch of events
            // The EventDataBatch exists to provide a deterministic and accurate means to measure the size of a
            // message sent to the service, minimizing the chance that a publishing operation will fail.
            //
            // All of the events that belong to an EventDataBatch are considered part of a single unit of work.
            // When a batch is published, the result is atomic; either publishing was successful for all events
            // in the batch, or it has failed for all events. Partial success or failure when publishing a
            // batch is not possible.
            using EventDataBatch eventBatch = await _producerClient.CreateBatchAsync();

            for (int i = 1; i <= numOfEvents; i++)
            {
                var payloadBytes = Encoding.UTF8.GetBytes(@event.ToJson());
                var eventData = new EventData(payloadBytes)
                {
                    ContentType = "application/json",
                };
                eventData.Properties.Add("EventType", "eventhub.poc.pgsql");

                if (!eventBatch.TryAdd(eventData))
                {
                    // if it is too large for the batch
                    throw new Exception($"Event {i} is too large for the batch and cannot be sent.");
                }
            }

            try
            {
                // Use the producer client to send the batch of events to the event hub
                await _producerClient.SendAsync(eventBatch);
                Console.WriteLine($"EventHub: A batch of {numOfEvents} events has been published.");
            }
            finally
            {
                await _producerClient.DisposeAsync();
            }
        }
    }
}