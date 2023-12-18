using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using MabiCommerce.Domain.Mapping;
using MabiCommerce.Domain.Trading;
using MabiCommerce.UI;
using QuikGraph;
using QuikGraph.Algorithms;

namespace MabiCommerce.Domain
{
    public class Erinn : INotifyPropertyChanged
	{
		public ObservableCollection<Transportation> Transports { get; private set; }
		public ObservableCollection<TradingPost> Posts { get; private set; }
		public ObservableCollection<Trade> Trades { get; private set; }
		public ObservableCollection<Modifier> Modifiers { get; private set; }
		public List<CommerceMasteryRank> CommerceMasteryRanks { get; private set; }
		public List<Region> Regions { get; private set; }
		public List<Portal> Portals { get; private set; }
		public List<MerchantLevel> MerchantLevels { get; private set; }
		public AdjacencyGraph<Waypoint, Connection> World { get; private set; }

		private long _ducats = 1000;
		public long Ducats
		{
			get { return _ducats; }
			set
			{
				_ducats = value;
				RaisePropertyChanged();
			}
		}

		private CommerceMasteryRank _cmRank;
		public CommerceMasteryRank CmRank
		{
			get { return _cmRank; }
			set
			{
				_cmRank = value;
				RaisePropertyChanged();
			}
		}

		private readonly ConcurrentDictionary<Waypoint, ConcurrentDictionary<Waypoint, Route>> _routeCache = new();

		public Erinn(string dataDir, Action<double, string>? progress = null)
		{
			Trades = new ObservableCollection<Trade>();
            progress ??= (d, s) => { };

			var total = 6.0;
			var done = 0;

			var opts = new JsonSerializerOptions()
			{
				AllowTrailingCommas = true,
				ReadCommentHandling = JsonCommentHandling.Skip
            };

            opts.Converters.Add(new Json.PointConverter());
			opts.Converters.Add(new Json.SizeConverter());
			opts.Converters.Add(new Json.ProfitCollectionConverter());

			progress(done / total, "Loading transports...");
			string data = File.ReadAllText(Path.Combine(dataDir, "db/transports.json"));
			var transportList = JsonSerializer.Deserialize<List<Transportation>>(data, opts)
				?? throw new Exception("Failed to load transports.");
            Transports = new ObservableCollection<Transportation>(transportList);
            done++;

			progress(done / total, "Loading modifiers...");
			var modifiers = File.ReadAllText(Path.Combine(dataDir, "db/modifiers.json"));
			var modifierList = JsonSerializer.Deserialize<List<Modifier>>(modifiers, opts)
                ?? throw new Exception("Failed to load modifiers.");
            Modifiers = new ObservableCollection<Modifier>(modifierList);
			done++;

			progress(done / total, "Loading commerce mastery ranks...");
			var masteryRanks = File.ReadAllText(Path.Combine(dataDir, "db/commerce_mastery_ranks.json"));
            CommerceMasteryRanks = JsonSerializer.Deserialize<List<CommerceMasteryRank>>(masteryRanks, opts)
                ?? throw new Exception("Failed to load commerce mastery ranks.");
            done++;

			progress(done / total, "Loading posts...");
			var posts = File.ReadAllText(Path.Combine(dataDir, "db/posts.json"));
			var postList = JsonSerializer.Deserialize<List<TradingPost>>(posts, opts)
				?? throw new Exception("Failed to load posts.");
            Posts = new ObservableCollection<TradingPost>(postList);
            done++;

			progress(done / total, "Loading regions...");
			var regions = File.ReadAllText(Path.Combine(dataDir, "db/regions.json"));
            Regions = JsonSerializer.Deserialize<List<Region>>(regions, opts)
                ?? throw new Exception("Failed to load regions.");
			done++;

			progress(done / total, "Loading portals...");
			var portals = File.ReadAllText(Path.Combine(dataDir, "db/portals.json"));
			Portals = JsonSerializer.Deserialize<List<Portal>>(portals, opts)
                ?? throw new Exception("Failed to load portals.");
			done++;

			progress(done / total, "Loading merchant levels...");
			var merchantLevels = File.ReadAllText(Path.Combine(dataDir, "db/merchant_levels.json"));
            MerchantLevels = JsonSerializer.Deserialize<List<MerchantLevel>>(merchantLevels, opts)
                ?? throw new Exception("Failed to load merchant levels.");
			done++;

			progress(0, "Initializing data...");
			World = new AdjacencyGraph<Waypoint, Connection>();
			CmRank = CommerceMasteryRanks.First();

			var lowestMerch = MerchantLevels.OrderBy(m => m.Level).First();

			foreach (var p in Posts)
				p.MerchantLevel = lowestMerch;

            LoadSaveData(opts, out List<ItemData>? itemDatas);
            InitializeProfits(itemDatas);
			MapWorld(progress);
		}

		private void LoadSaveData(JsonSerializerOptions opts, out List<ItemData>? itemDatas)
        {
            itemDatas = null;
			if (!File.Exists("save.json"))
				return;

            var saveData = File.ReadAllText("save.json");
            var save = JsonSerializer.Deserialize<SaveData>(saveData, opts)
                ?? throw new Exception("Failed to load save data.");

            itemDatas = save.Items;
            Ducats = save.Ducats;

            foreach (var transport in Transports)
				transport.Enabled = transport.IsRequired || save.EnabledVehicles?.Contains(transport.Id) == true;

			foreach (var modifier in Modifiers)
				modifier.Enabled = save.EnabledModifiers?.Contains(modifier.Id) == true;

			foreach (var post in Posts) {
				// Set the saved item price
				foreach (var item in post.Items)
					item.Price = itemDatas?.FirstOrDefault(x => x.Id == item.Id)?.Price ?? item.Price;

				// Set the saved merchant level
				var savedPost = save.Posts.FirstOrDefault(p => p.Id == post.Id);
				if (savedPost == null)
					continue;

				post.MerchantLevel = MerchantLevels.FirstOrDefault(m => m.Level == savedPost.Level) ?? post.MerchantLevel;
			}
        }

		private void MapWorld(Action<double, string> progress)
		{
			foreach (var region in Regions)
				World.AddVerticesAndEdgeRange(region.RegionGraph.Edges);

			foreach (var portal in Portals)
			{
				var startRegion = Regions.FirstOrDefault(r => r.Id.Equals(portal.StartRegionId, StringComparison.OrdinalIgnoreCase));
				var endRegion = Regions.FirstOrDefault(r => r.Id.Equals(portal.EndRegionId, StringComparison.OrdinalIgnoreCase));

				var startWp = startRegion.Waypoints[portal.StartWaypointId.ToLowerInvariant()];
				var endWp = endRegion.Waypoints[portal.EndWaypointId.ToLowerInvariant()];

				World.AddEdge(new Connection(startWp, endWp, portal.Time));
			}

			foreach (var p in Posts)
			{
				var region = Regions.FirstOrDefault(r => r.Id.Equals(p.WaypointRegion, StringComparison.OrdinalIgnoreCase))
					?? throw new Exception("Cannot find region for post " + p);
				p.Waypoint = region.Waypoints[p.WaypointId.ToLowerInvariant()];
			}

			double total = Posts.Count * (Posts.Count - 1);
			var done = 0;

			foreach (var p in Posts)
			{
				foreach (var p2 in Posts)
				{
					if (p2 == p)
						continue;

					progress(done / total, "Caching route data...");
					Route(p.Waypoint, p2.Waypoint);
					done++;
				}
			}
		}

		private void InitializeProfits(List<ItemData>? itemDatas)
		{
            foreach (var p in Posts)
			{
				var otherPosts = Posts.Where(x => x != p).ToList();

				foreach (var i in p.Items)
                {
					var profitData = itemDatas?.FirstOrDefault(x => x.Id == i.Id)?.Profits;
                    
					foreach (var o in otherPosts)
                    {
						var profit = profitData?.FirstOrDefault(x => x.Destination == o.Id)?.Amount ?? 0;
						i.Profits.Add(new Profit(o, profit));
					}
				}
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;
		private void RaisePropertyChanged([CallerMemberName] string caller = "")
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(caller));

		public Route Route(Waypoint start, Waypoint end)
		{
			if (!_routeCache.ContainsKey(start))
				_routeCache[start] = new ConcurrentDictionary<Waypoint, Route>();

			if (_routeCache[start].ContainsKey(end))
				return _routeCache[start][end];

			return (_routeCache[start][end] = CalculateRoute(start, end));
		}

		private Route CalculateRoute(Waypoint start, Waypoint end)
		{
            if (!World.ShortestPathsDijkstra(e => e.Time.TotalSeconds, start)(end, out IEnumerable<Connection> shortestPathResult))
                throw new Exception("Cannot locate a path from " + start + " to " + end);

            var waypoints = shortestPathResult.ToList();
			return new Route(waypoints);
		}

		private void GetLoads(ConcurrentBag<Load> loads, TradingPost post, Load currentLoad, long currentDucts)
		{
			if (currentLoad.RemainingSlots == 0)
				return;

			var candidates = post.Items.Where(i => i.Status == ItemStatus.Available && !currentLoad.Slots.ContainsKey(i)).ToList();
			Parallel.ForEach(candidates, i => FillLoadWithItem(loads, post, currentLoad, currentDucts, i));
		}

		private void FillLoadWithItem(ConcurrentBag<Load> loads, TradingPost post, Load currentLoad, long currentDucats, Item item)
		{
			var canAdd = (int)(currentDucats / item.Price);
			var added = AddItemToLoad(loads, post, currentLoad, currentDucats, item, canAdd);
			var excess = added % item.QuantityPerSlot;

			if (added < item.QuantityPerSlot || excess == 0)
				return;

			AddItemToLoad(loads, post, currentLoad, currentDucats, item, added - excess);
		}

		private int AddItemToLoad(ConcurrentBag<Load> loads, TradingPost post, Load currentLoad, long currentDucats, Item item, int requested)
		{
			var loadCopy = currentLoad.Copy();
			var added = loadCopy.AddItem(item, Math.Min(item.Stock, requested));

			if (added != 0)
			{
				loads.Add(loadCopy);
				GetLoads(loads, post, loadCopy, currentDucats - item.Price * added);
			}

			return added;
		}

		public ConcurrentBag<Trade> CalculateTrades(TradingPost post)
		{
			var newTrades = new ConcurrentBag<Trade>();

			var s = new System.Diagnostics.Stopwatch();
			s.Start();

			var mods = GetModifierCombinations(Modifiers);

			var cmMod = new Modifier(-1, "Commerce Mastery", 0, 0, 0,
					CmRank.Bonus, CmRank.Bonus, CmRank.Bonus, CmRank.Bonus, new List<int>(), new List<int>());

			foreach (var m in mods)
				m.Add(cmMod);

			mods.Add(new List<Modifier> { cmMod });

			Parallel.ForEach(Transports.Where(t => t.Enabled), t =>
				{
					var allowedMods = mods.Where(combination =>
						!combination.Any(m => 
							m.TransportationBlacklist.Contains(t.Id))
						);

					Parallel.ForEach(allowedMods, m =>
					{
						var baseLoad = new Load(t.Slots + m.Sum(i => i.ExtraSlots),
							t.Weight + m.Sum(i => i.ExtraWeight));

						var loads = new ConcurrentBag<Load>();
						GetLoads(loads, post, baseLoad, Ducats);

						foreach (var load in loads)
							foreach (var dst in Posts.Where(p => p != post))
							{
								newTrades.Add(new Trade(t, Route(post.Waypoint, dst.Waypoint), load, post, dst, m));
							}
					});
				});

			s.Stop();

			System.Diagnostics.Debug.WriteLine("Calculated {0} possible trades ({1} items, {2} destinations, {3} means of transport, {4} modifier combinations) in {5}", newTrades.Count,
				post.Items.Count(i => i.Status == ItemStatus.Available), Posts.Count - 1, Transports.Count(t => t.Enabled), mods.Count, s.Elapsed);

			return newTrades;
		}

		// Given A, B, C
		// Produces A, AB, B, AC, ABC, BC, C
		private static List<List<Modifier>> GetModifierCombinations(IList<Modifier> modifiers)
		{
			var combinations = new List<List<Modifier>>();

			for (var append = 1; append < modifiers.Count; append++)
			{
				// Add another "base" element
				if (modifiers[append - 1].Enabled)
					combinations.Add(new List<Modifier>(modifiers.Count) { modifiers[append - 1] });

				var toAdd = modifiers[append];
				if (!toAdd.Enabled)
					continue;

				for (int i = 0, count = combinations.Count; i < count; i++)
				{
					var existing = combinations[i];

					// If we'd produce a conflict (illegal state), skip adding it.
					if (existing.Any(m => toAdd.ConflictsWith.Contains(m.Id)))
						continue;

					List<Modifier> newSet = new(existing) {
						toAdd
					};

					combinations.Add(newSet);
				}
			}

			// Add the last modifier by itself (since for loop is 1 based)
			if (modifiers.Last().Enabled)
				combinations.Add(new List<Modifier> { modifiers.Last() });

			return combinations;
		}
	}
}
