using System.Text.Json;
using System.Text.Json.Serialization;

using EventHubStarter.Common.Json;

namespace EventHubStarter.Common.Events
{
    public class RecordChangedEvent(string schema, string table, string action, Dictionary<string, object> data)
    {
        [JsonPropertyName("schema")]
        public string Schema { get; set; } = schema;

        [JsonPropertyName("table")]
        public string Table { get; set; } = table;

        [JsonPropertyName("action")]
        public string Action { get; set; } = action;

        [JsonPropertyName("data")]
        [JsonConverter(typeof(DictionaryStringObjectJsonConverter))]
        public Dictionary<string, object> Data { get; set; } = data;

        public string ToJson() => JsonSerializer.Serialize(this);

        public static RecordChangedEvent FromJson(string payload)
        {
            var @event = JsonSerializer.Deserialize<RecordChangedEvent>(payload);

            return @event ?? throw new Exception("Event deserializing failure.");
        }
    }
}
