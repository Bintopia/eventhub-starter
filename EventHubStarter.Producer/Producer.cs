using System.Data;

using Azure.Messaging.EventHubs.Producer;

using EventHubStarter.Common.Events;
using EventHubStarter.Producer;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Npgsql;


HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

var pgConnectionString = builder.Configuration.GetConnectionString("PostgreSQL");
var hubConnection = builder.Configuration["EventHub:SAS"];
var hubName = builder.Configuration["EventHub:EventHubName"];

using (var pgConnection = new NpgsqlConnection(pgConnectionString))
{
    Console.WriteLine("PostgreSQL: Connecting...");

    await pgConnection.OpenAsync();

    if (pgConnection.State == ConnectionState.Open)
    {
        Console.WriteLine("PostgreSQL: connected");

        pgConnection.Notification += async (sender, e) =>
        {
            Console.WriteLine($"PostgreSQL: Notification received, processing...");

            var notifyEvent = RecordChangedEvent.FromJson(e.Payload);

            Console.WriteLine(e.Payload);

            var hubClient = new EventHubProducerClient(hubConnection, hubName);

            await new PgNotificationProcessor(hubClient).PublishAsync(notifyEvent);
        };

        var triggerChannel = "datachange";
        Console.WriteLine($"PostgreSQL: listen on channel '{triggerChannel}'");
        using var command = new NpgsqlCommand($"LISTEN {triggerChannel};", pgConnection);
        command.ExecuteNonQuery();

        // Wait for events
        while (true)
        {
            await pgConnection.WaitAsync();
        }
    }
}

// Application code which might rely on the config could start here.
using IHost host = builder.Build();

await host.RunAsync();