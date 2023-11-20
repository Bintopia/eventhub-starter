using Azure.Messaging.EventHubs.Consumer;

using EventHubStarter.Common.SqlSink;
using EventHubStarter.Consumer;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

Console.WriteLine("Application started.");

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

var sqlConnectionString = builder.Configuration.GetConnectionString("MSSQL");

if (string.IsNullOrEmpty(sqlConnectionString))
{
    throw new Exception("MSSQL connection string configuration required.");
}

var hubConnection = builder.Configuration["EventHub:SAS"];
var eventHubConsumerClient = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, hubConnection);
var sqlWriter = new SqlWriter(sqlConnectionString);
//var reader = new EventReader(eventHubConsumerClient, sqlWriter);
var reader = new EventReaderV2(eventHubConsumerClient, sqlWriter);

while (true)
{
    await reader.ReadAsync();
}
