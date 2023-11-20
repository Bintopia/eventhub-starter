# EventHub Starter

## Notes

1. PGSQL notify/listen: https://www.postgresql.org/docs/current/sql-notify.html
1. PGSQL triggers on data changes: https://postgrespro.com/docs/postgresql/14/plpgsql-trigger
1. We may need to fetch all data at the beginning(a initial full copy sync)£¬PG notification can only sync updates/changes, e.g. LISTEN func recevies a row update£¬consumer update may fail case the data doesn't exist, like in MSSQL.
1. Is schema sync needed?: table and it's columns, indexs and relationship etc.
1. Partitions: Way to improve app IO, need to design the algorithm when programming: https://learn.microsoft.com/en-us/azure/event-hubs/event-hubs-scalability#advantages-of-using-partitions
1. Event Size constraints: https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/eventhub/Azure.Messaging.EventHubs/samples/Sample04_PublishingEvents.md#publishing-size-constraints
1. Retry: https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/eventhub/Azure.Messaging.EventHubs/samples/Sample02_EventHubsClients.md#configuring-the-client-retry-thresholds
1. Event Lifetime
        - https://learn.microsoft.com/en-us/azure/event-hubs/log-compaction
        - https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/eventhub/Azure.Messaging.EventHubs/samples/Sample05_ReadingEvents.md#event-lifetime
1. Processor/Balance partition load: https://learn.microsoft.com/en-us/azure/event-hubs/event-processor-balance-partition-load