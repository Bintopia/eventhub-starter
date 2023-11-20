using System.Text;
using System.Text.Json;

using Azure.Messaging.EventHubs.Consumer;

using EventHubStarter.Common.Events;
using EventHubStarter.Common.SqlSink;

namespace EventHubStarter.Consumer
{
    internal class EventReaderV2(EventHubConsumerClient consumerClient, SqlWriter dataWriter)
    {
        private readonly EventHubConsumerClient _consumerClient = consumerClient;
        private readonly SqlWriter _dataWriter = dataWriter;
        private Dictionary<string, long> PartitionReadOffsets = [];

        public async Task ReadAsync()
        {
            try
            {
                await LoadPartitionReadOffsetsAsync();

                using CancellationTokenSource cancellationSource = new();
                cancellationSource.CancelAfter(TimeSpan.FromSeconds(45));

                var partitions = await _consumerClient.GetPartitionIdsAsync();

                foreach (var partition in partitions)
                {
                    PartitionReadOffsets.TryGetValue(partition, out long lastReadOffset);

                    EventPosition startingPosition = EventPosition.FromOffset(lastReadOffset);

                    await foreach (PartitionEvent partitionEvent in _consumerClient.ReadEventsFromPartitionAsync(partition, startingPosition, cancellationSource.Token))
                    {
                        Console.WriteLine($"EventHub: Reading event from partition {partition} at offset {lastReadOffset}...");

                        byte[] eventBodyBytes = partitionEvent.Data.EventBody.ToArray();

                        Console.WriteLine($"EventHub: Read event of length {eventBodyBytes.Length} from {partition}");

                        var payload = Encoding.UTF8.GetString(eventBodyBytes);

                        Console.WriteLine(payload);

                        var @event = RecordChangedEvent.FromJson(payload);
                        if (@event != null && !string.IsNullOrEmpty(@event.Action))
                        {
                            await _dataWriter.WriteAsync(@event);
                        }

                        var currentOffset = partitionEvent.Data.Offset;
                        Console.WriteLine($"EventHub: Update read offset to {currentOffset}");
                        PartitionReadOffsets[partition] = currentOffset;
                        await SavePartitionReadOffsetsAsync();
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

        const string PARTITION_READ_OFFSET_DATA = "Data/readoffsets.json";

        private async Task LoadPartitionReadOffsetsAsync()
        {
            if (File.Exists(PARTITION_READ_OFFSET_DATA))
            {
                var payload = await File.ReadAllTextAsync(PARTITION_READ_OFFSET_DATA);
                PartitionReadOffsets = JsonSerializer.Deserialize<Dictionary<string, long>>(payload);
            }
            else
            {
                var partitions = await _consumerClient.GetPartitionIdsAsync();
                foreach (var partition in partitions)
                {
                    PartitionReadOffsets.Add(partition, 0L);
                }
            }
        }

        private async Task SavePartitionReadOffsetsAsync()
        {
            var payload = JsonSerializer.Serialize(PartitionReadOffsets);
            await File.WriteAllTextAsync(PARTITION_READ_OFFSET_DATA, payload);
        }
    }
}
