using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace MabiCommerce.Json
{
    public class SizeConverter : JsonConverter<Size>
    {
        public override Size Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var size = reader.GetString().Split(',');
            return new Size(int.Parse(size[0].Trim()), int.Parse(size[1].Trim()));
        }

        public override void Write(Utf8JsonWriter writer, Size value, JsonSerializerOptions options)
        {
            writer.WriteStringValue($"{value.Width}, {value.Height}");
        }
    }
}
