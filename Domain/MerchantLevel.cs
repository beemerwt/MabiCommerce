using System.Text.Json.Serialization;

namespace MabiCommerce.Domain
{
    [JsonSerializable(typeof(MerchantLevel))]
	public class MerchantLevel
	{
		public int Level { get; private set; }
		public int Exp { get; private set; }
		public double Discount { get; private set; }

		public MerchantLevel(int level, int exp, double discount)
		{
			Level = level;
			Exp = exp;
			Discount = discount;
		}
	}
}
