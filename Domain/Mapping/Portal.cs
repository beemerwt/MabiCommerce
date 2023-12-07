using System;
using System.Text.Json.Serialization;

namespace MabiCommerce.Domain.Mapping
{
	[JsonSerializable(typeof(Portal))]
	public class Portal
	{
		public string StartRegionId { get; private set; }
		public string EndRegionId { get; private set; }
		public string StartWaypointId { get; private set; }
		public string EndWaypointId { get; private set; }
		public TimeSpan Time { get; private set; }

		[JsonConstructor]
		public Portal(string startRegionId, string startWaypointId, string endRegionId, string endWaypointId, TimeSpan time)
		{
			Time = time;
			EndWaypointId = endWaypointId;
			EndRegionId = endRegionId;
			StartWaypointId = startWaypointId;
			StartRegionId = startRegionId;
		}
	}
}
