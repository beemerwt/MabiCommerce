using System.Text.Json.Serialization;
using System.Windows;

namespace MabiCommerce.Domain.Mapping
{
    [JsonSerializable(typeof(Waypoint))]
	public class Waypoint
	{
		public string Id { get; private set; }
		public Point Location { get; private set; }

		[JsonIgnore]
		public Region Region { get; set; }

		[JsonConstructor]
		public Waypoint(string id, Point location)
		{
			Id = id;
			Location = location;
		}

		public override string ToString()
		{
			return string.Format("{0} - {1}", Id, Location);
		}
	}
}
