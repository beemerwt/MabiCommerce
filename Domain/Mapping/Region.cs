using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Windows;
using QuikGraph;

namespace MabiCommerce.Domain.Mapping
{
	[JsonSerializable(typeof(Region))]
	public class Region
	{
		/// <summary>
		/// This is the speed, in cm/s, of a human using a handcart.
		/// </summary>
		private const double BaseSpeed = 373.8506;
		public string Id { get; private set; }

		public List<Waypoint> WaypointList { get; private set; }

		public List<Tuple<string, string>> Connections { get; private set; }

		public Size Size { get; private set; }
		public string MiniMap { get; private set; }
		public Point MiniMapOffset { get; private set; }
		public string WorldMap { get; private set; }
		public Point WorldMapOffset { get; private set; }
		public bool ChokePoint { get; private set; }

		[JsonIgnore]
		public Dictionary<string, Waypoint> Waypoints { get; private set; }

		[JsonIgnore]
		public AdjacencyGraph<Waypoint, Connection> RegionGraph { get; private set; }

		[JsonConstructor]
		public Region(string id, List<Waypoint> waypointList, List<Tuple<string, string>> connections,
			Size size, string miniMap, Point miniMapOffset, string worldMap, Point worldMapOffset, bool chokePoint)
		{
			ChokePoint = chokePoint;
			WorldMapOffset = worldMapOffset;
			WorldMap = worldMap;
			MiniMapOffset = miniMapOffset;
			MiniMap = miniMap;
			Size = size;
			Id = id;
			WaypointList = waypointList;
			Connections = connections;

			foreach (var wp in WaypointList)
			{
				wp.Region = this;
			}

			Waypoints = WaypointList.ToDictionary(x => x.Id.ToLowerInvariant());

			RegionGraph = new AdjacencyGraph<Waypoint, Connection>(true);
			RegionGraph.AddVertexRange(waypointList);

			foreach (var c in Connections)
			{
				var wp1 = Waypoints[c.Item1];
				var wp2 = Waypoints[c.Item2];

				var dist = Math.Sqrt(Math.Pow(wp1.Location.X - wp2.Location.X, 2) + Math.Pow(wp1.Location.Y - wp2.Location.Y, 2));

				var time = TimeSpan.FromSeconds(dist / BaseSpeed);

				RegionGraph.AddEdge(new Connection(wp1, wp2, time));
				RegionGraph.AddEdge(new Connection(wp2, wp1, time));
			}
		}

		public override string ToString()
		{
			return Id;
		}
	}
}
