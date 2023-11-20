using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs;
using Azure.Storage.Blobs;
using EventHubStarter.Processor;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;


HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

Console.WriteLine("Application started.");

var hubConnection = builder.Configuration["EventHub:SAS"];
var blobConnection = builder.Configuration["AzureBlobStorage:Connection"];
var blobContainerName = builder.Configuration["AzureBlobStorage:Container"];

Console.WriteLine("Create a blob container client that the event processor will use.");
var storageClient = new BlobContainerClient(blobConnection, blobContainerName);

Console.WriteLine("Create an event processor client to process events in the event hub");
var processor = new EventProcessorClient(storageClient, EventHubConsumerClient.DefaultConsumerGroupName, hubConnection);

var sqlConnectionString = builder.Configuration.GetConnectionString("MSSQL");

var handler = new ProcessorEventHandlers(sqlConnectionString);

Console.WriteLine("Register handlers for processing events and handling errors");
processor.ProcessEventAsync += handler.OnEventAsync;
processor.ProcessErrorAsync += handler.OnProcessErrorAsync;


Console.WriteLine("Start the processing");

await processor.StartProcessingAsync();

while ((Console.ReadKey()).Key != ConsoleKey.Enter)
{
    Console.WriteLine();
}

Console.WriteLine("Stop the processing");
await processor.StopProcessingAsync();