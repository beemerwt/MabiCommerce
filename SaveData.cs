using MabiCommerce.Domain;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MabiCommerce
{
    [JsonSerializable(typeof(SaveData))]
    class SaveData
    {
        public long Ducats { get; set; }
        public List<int> EnabledVehicles { get; set; } = new();
        public List<int> EnabledModifiers { get; set; } = new();
        public List<ItemData> Items { get; set; } = new();
        public List<PostData> Posts { get; set; } = new();
    }

    [JsonSerializable(typeof(ItemData))]
    class ItemData
    {
        public int Id { get; set; }

        public int Price { get; set; }

        public List<ProfitData> Profits { get; set; } = new();
    }

    [JsonSerializable(typeof(PostData))]
    class PostData
    {
        public int Id { get; set; }
        public int Level { get; set; }
    }

    [JsonSerializable(typeof(ProfitData))]
    class ProfitData
    {
        public int Destination { get; set; }
        public int Amount { get; set; }
    }}
