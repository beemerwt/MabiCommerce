using MabiCommerce.Domain;
using System;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MabiCommerce.Json
{
    public class ProfitCollectionConverter : JsonConverter<ObservableCollection<Profit>>
    {
        public override ObservableCollection<Profit>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var collection = new ObservableCollection<Profit>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    break;

                var profit = JsonSerializer.Deserialize<Profit>(ref reader, options);
                if (profit != null) collection.Add(profit);
            }

            return collection;
        }

        public override void Write(Utf8JsonWriter writer, ObservableCollection<Profit> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();

            foreach (var profit in value)
            {
                JsonSerializer.Serialize(writer, profit, options);
            }

            writer.WriteEndArray();
        }
    }
}
