using QuikGraph;
using System;

namespace MabiCommerce.Domain.Mapping
{
    public class Connection : IEdge<Waypoint>
	{

		public Waypoint Source { get; private set; }
		public Waypoint Target { get; private set; }

		public TimeSpan Time { get; private set; }

		public Connection(Waypoint src, Waypoint dst, TimeSpan time)
		{
			Source = src;
			Target = dst;

			Time = time;
		}

		public override string ToString()
		{
			return string.Format("{0} to {1} in {2}", Source.Id, Target.Id, Time);
		}
	}
}
