using Azure.Messaging.EventHubs.Processor;

using EventHubStarter.Common.Events;
using EventHubStarter.Common.SqlSink;

using System.Text;

namespace EventHubStarter.Processor
{
    public class ProcessorEventHandlers(string sqlConnection)
    {
        private readonly SqlWriter _sqlWriter = new SqlWriter(sqlConnection);

        public Task OnEventAsync(ProcessEventArgs eventArgs)
        {

            var eventDataBytes = eventArgs.Data.Body.ToArray();
            var payload = Encoding.UTF8.GetString(eventDataBytes);
            Console.WriteLine("Event data recevied, processing...");
            Console.WriteLine(payload);

            var eventData = RecordChangedEvent.FromJson(payload);

            return _sqlWriter.WriteAsync(eventData);
        }

        public Task OnProcessErrorAsync(ProcessErrorEventArgs eventArgs)
        {
            // Write details about the error to the console window
            Console.WriteLine($"\tPartition '{eventArgs.PartitionId}': an unhandled exception was encountered. This was not expected to happen.");
            Console.WriteLine(eventArgs.Exception.Message);
            return Task.CompletedTask;
        }
    }
}