using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace MabiCommerce.Domain
{
    [JsonSerializable(typeof(Transportation))]
	public class Transportation : INotifyPropertyChanged
	{
		public string Name { get; private set; }
		public string Icon { get; private set; }
		public float SpeedFactor { get; private set; }
		public int Slots { get; private set; }
		public int Weight { get; private set; }
		public bool IsRequired { get; private set; }
		public int Id { get; private set; }

		private bool _enabled;
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

		[JsonConstructor]
		public Transportation(string name, string icon, float speedFactor, int slots, int weight, bool isRequired, int id)
		{
			Id = id;
			IsRequired = isRequired;
			Weight = weight;
			Slots = slots;
			SpeedFactor = speedFactor;
			Icon = icon;
			Name = name;

			if (IsRequired)
				Enabled = true;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
