using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using MabiCommerce.Domain.Mapping;

namespace MabiCommerce.Domain
{
    [JsonSerializable(typeof(TradingPost))]
	public class TradingPost : INotifyPropertyChanged
	{
		public int Id { get; private set; }
		public string Name { get; private set; }
		public string Image { get; private set; }
		public ObservableCollection<Item> Items { get; private set; }
		public string WaypointRegion { get; private set; }
		public string WaypointId { get; private set; }
		public List<int> NoProfits { get; private set; }
		public Dictionary<int, double> Weights { get; private set; }

		[JsonIgnore]
		public Waypoint Waypoint { get; set; }

		private MerchantLevel _merchantLevel;

		[JsonIgnore]
		public MerchantLevel MerchantLevel
		{
			get { return _merchantLevel; }
			set
			{
				_merchantLevel = value;
				RaisePropertyChanged();

				foreach (var item in Items)
				{
					item.IsRatingMet = item.MerchantRating <= value.Level;
				}
			}
		}

		[JsonConstructor]
		public TradingPost(int id, string name, string image, ObservableCollection<Item> items, string waypointRegion, string waypointId, List<int> noProfits, Dictionary<int, double> weights)
		{
			Weights = weights;
			NoProfits = noProfits;
			WaypointId = waypointId;
			WaypointRegion = waypointRegion;
			Id = id;
			Items = items;
			Image = image;
			Name = name;
		}

		public event PropertyChangedEventHandler? PropertyChanged;
		private void RaisePropertyChanged([CallerMemberName] string caller = "")
		{
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(caller));
        }

		public override string ToString()
		{
			return Name;
		}
	}
}
