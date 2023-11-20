using System.Text;

using Azure.Messaging.EventHubs.Consumer;

using EventHubStarter.Common.Events;
using EventHubStarter.Common.SqlSink;

namespace EventHubStarter.Consumer
{
    internal class EventReader(EventHubConsumerClient consumerClient, SqlWriter dataWriter)
    {
        private readonly EventHubConsumerClient _consumerClient = consumerClient;
        private readonly SqlWriter _dataWriter = dataWriter;
        private readonly static Dictionary<string, long> PartitionReadOffset = [];

        public async Task ReadAsync()
        {
            try
            {
                await foreach (PartitionEvent partitionEvent in _consumerClient.ReadEventsAsync())
                {
                    Console.WriteLine("EventHub: Reading...");

                    string readFromPartition = partitionEvent.Partition.PartitionId;

                    var currentOffset = partitionEvent.Data.Offset;

                    byte[] eventBodyBytes = partitionEvent.Data.EventBody.ToArray();

                    Console.WriteLine($"EventHub: Read event of length {eventBodyBytes.Length} from {readFromPartition} with offset {currentOffset}");

                    var payload = Encoding.UTF8.GetString(eventBodyBytes);

                    Console.WriteLine(payload);

                    var @event = RecordChangedEvent.FromJson(payload);
                    if (@event != null && !string.IsNullOrEmpty(@event.Action))
                    {
                        await _dataWriter.WriteAsync(@event);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // This is expected if the cancellation token is
                // signaled.
            }
            finally
            {
                await _consumerClient.CloseAsync();
            }
        }
    }
}
