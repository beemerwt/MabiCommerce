using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace MabiCommerce.Json
{
    public class PointConverter : JsonConverter<Point>
    {
        public override Point Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var point = reader.GetString().Split(',');
            return new Point(double.Parse(point[0].Trim()), double.Parse(point[1].Trim()));
        }

        public override void Write(Utf8JsonWriter writer, Point value, JsonSerializerOptions options)
        {
            writer.WriteStringValue($"{value.X}, {value.Y}");
        }
    }
}
