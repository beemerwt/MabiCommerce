using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;

namespace MabiCommerce.Domain
{
    [JsonSerializable(typeof(MerchantLevel))]
	public class Modifier : INotifyPropertyChanged
	{
		public int Id { get; private set; }
		public string Name { get; private set; }
		public int ExtraSlots { get; private set; }
		public int ExtraWeight { get; private set; }
		public double SpeedBonus { get; private set; }
		public double ExpBonus { get; private set; }
		public double GoldBonus { get; private set; }
		public double ProfitBonus { get; private set; }
		public double MerchantRatingBonus { get; private set; }
		public List<int> TransportationBlacklist { get; private set; }
		public List<int> ConflictsWith { get; private set; }

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

		[JsonIgnore]
		public string EffectDescription { get; private set; }

		public event PropertyChangedEventHandler? PropertyChanged;
		private void RaisePropertyChanged([CallerMemberName] string caller = "")
		{
			RaisePropertyChangedExplicit(caller);
		}

		private void RaisePropertyChangedExplicit(string name)
		{
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

		public Modifier(int id, string name, int extraSlots, int extraWeight,
			double speedBonus, double expBonus, double goldBonus, double profitBonus,
			double merchantRatingBonus, List<int> transportationBlacklist, List<int> conflictsWith)
		{
			Id = id;
			Name = name;

			ExtraSlots = extraSlots;
			ExtraWeight = extraWeight;
			SpeedBonus = speedBonus;
			ExpBonus = expBonus;
			GoldBonus = goldBonus;
			ProfitBonus = profitBonus;
			MerchantRatingBonus = merchantRatingBonus;

			ConflictsWith = conflictsWith ?? new List<int>();
			TransportationBlacklist = transportationBlacklist ?? new List<int>();

			const string intFormat = "+#,###;-#,###";
			const string percentFormat = "+#,##0.##%;-#,##0.##%";

			var effects = new StringBuilder();
			if (extraSlots != 0)
				effects.AppendLine(string.Format("{0} Slots", extraSlots.ToString(intFormat)));
			if (extraWeight != 0)
				effects.AppendLine(string.Format("{0} Weight", extraWeight.ToString(intFormat)));
			if (speedBonus != 0.0)
				effects.AppendLine(string.Format("{0} Speed", speedBonus.ToString(percentFormat)));
			if (profitBonus != 0.0)
				effects.AppendLine(string.Format("{0} Profit", profitBonus.ToString(percentFormat)));
			if (goldBonus != 0.0)
				effects.AppendLine(string.Format("{0} Gold", goldBonus.ToString(percentFormat)));
			if (expBonus != 0.0)
				effects.AppendLine(string.Format("{0} Exp", expBonus.ToString(percentFormat)));
			if (merchantRatingBonus != 0.0)
				effects.AppendLine(string.Format("{0} Merch. Rating", merchantRatingBonus.ToString(percentFormat)));

			EffectDescription = effects.ToString().Trim();
		}
	}
}
