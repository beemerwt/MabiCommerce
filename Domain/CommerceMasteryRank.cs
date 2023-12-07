using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MabiCommerce.Domain
{
	[JsonSerializable(typeof(CommerceMasteryRank))]
	public class CommerceMasteryRank : INotifyPropertyChanged
	{
		public int Id { get; private set; }
		public string Rank { get; private set; }
		public double Bonus { get; private set; }

		private bool _enabled;

		[JsonIgnore]
		public bool Enabled
		{
			get { return _enabled; }
			set
			{
				_enabled = value;
				RaisePropertyChanged();
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;
		private void RaisePropertyChanged([CallerMemberName] string caller = "")
		{
			RaisePropertyChangedExplicit(caller);
		}

		private void RaisePropertyChangedExplicit(string name)
		{
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

		public CommerceMasteryRank(int id, string rank, double bonus)
		{
			Id = id;
			Rank = rank;
			Bonus = bonus;
		}
	}
}
