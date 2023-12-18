using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Emgu.CV.Structure;
using MabiCommerce.Domain;
using MabiCommerce.Domain.Trading;
using Emgu.CV;
using System.Collections.Generic;
using Emgu.CV.CvEnum;
using System.Text.Json.Serialization;
using System.Text.Json;
using Emgu.CV.OCR;

using Point = System.Drawing.Point;
using System.Windows.Interop;
using System.Diagnostics;
using MabiCommerce.Hotkey;
using MabiCommerce.Screen;

namespace MabiCommerce.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
	{
		public static RoutedCommand SaveCommand = new();

		// "Inner" window bounds of the trade window (default)
		// We will change these after detecting each corner of the window
		// The width and height will remain the same
		private static Rectangle WindowBounds = new(486, 325, 944, 444);

        // These are offsets relative to the window bounds
        private static readonly Rectangle PostInfoBounds = new(11, 12, 345, 49);
		private static readonly Rectangle PostEmblemBounds = new(11, 12, 49, 49);

        private static readonly Rectangle ListBounds = new(600, 104, 67, 169);
		private static readonly Rectangle ItemBounds = new(10, 0, 342, 57);

		public Erinn Erinn { get; private set; }
		private HotkeyHelper? HotkeyHelper;
		private IScreenParser ScreenParser;

		public bool AutoDetectSupport
		{
			get
			{
#if AUTODETECT
				return true;
#else
				return false;
#endif
			}
		}

		public MainWindow(Erinn e)
		{
			
			InitializeComponent();

#if GAME_FONT
			ScreenParser = new GameFontParser();
#else
			ScreenParser = new BitmapFontParser();
#endif

			HotkeysManager.SetupSystemHook();
			HotkeysManager.AddHotkey(new GlobalHotkey(ModifierKeys.Control, Key.Up,
				() => ItemSelect.SelectedIndex = Math.Max(0, ItemSelect.SelectedIndex - 1)));
			HotkeysManager.AddHotkey(new GlobalHotkey(ModifierKeys.Control, Key.Down,
				() => ItemSelect.SelectedIndex = Math.Min(ItemSelect.Items.Count - 1, ItemSelect.SelectedIndex + 1)));
			HotkeysManager.AddHotkey(new GlobalHotkey(ModifierKeys.Control, Key.End,
				() => RefreshBtn_Click(null, null)));

			DataContext = Erinn = e;
			SaveCommand.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));

            WinApi.Instance.OnForegroundWindowChanged += Instance_OnForegroundWindowChanged;
            Connected.Fill = System.Windows.Media.Brushes.Red;
        }

        private void Instance_OnForegroundWindowChanged()
        {
			if (WinApi.IsMabiActive)
				Connected.Fill = System.Windows.Media.Brushes.Green;
			else
				Connected.Fill = System.Windows.Media.Brushes.Red;
        }

        private void WindowBar_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				DragMove();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
			=> Close();

		private void MinimizeButton_Click(object sender, RoutedEventArgs e)
			=> WindowState = WindowState.Minimized;

		private void CalculateTrades_Click(object sender, RoutedEventArgs e)
			=> CalculateTrades();

		private void MapItButton_Click(object sender, RoutedEventArgs e)
		{
			if (TradeSelect.SelectedItem == null)
				return;

			new WorldMapWindow(TradeSelect.SelectedItem as Trade).Show();
		}

		public void CalculateTrades()
		{
			if (!CalculateTradesBtn.IsEnabled)
				return;

			CalculateTradesBtn.IsEnabled = false;

			var post = PostSelect.SelectedItem as TradingPost;

			Task.Factory.StartNew(delegate
			{
				var trades = Erinn.CalculateTrades(post);

				Dispatcher.Invoke(delegate
				{
					Erinn.Trades.Clear();

					foreach (var t in trades)
						Erinn.Trades.Add(t);

					CalculateTradesBtn.IsEnabled = true;

					if (trades.Any())
						TradeSelect.ScrollIntoView(TradeSelect.Items[0]);
				});
			});
        }

		// Must be disposed of afterwards
        private Bitmap CaptureScreen(Rectangle bounds)
		{
			Bitmap bitmap = new(bounds.Width, bounds.Height);
            using (Graphics g = Graphics.FromImage(bitmap))
                g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);
            return bitmap;
        }

        // Must be disposed of afterwards
        // TODO: Get trade window bounds within WinApi.Instance.ClientBounds
        // until then we use the default...
        private Bitmap CaptureTradeWindow()
			=> CaptureScreen(WindowBounds);


		// Seems to be working correctly for bitmap font...
		private TradingPost? GetPostFromEmblem(Image<Bgr, byte> img)
		{
            img.ROI = PostEmblemBounds;

			for (int i = 0; i < Erinn.Posts.Count; i++)
			{
                using Bitmap bitmap = new(Erinn.Posts[i].Image);
                using var postEmblem = bitmap.ToImage<Bgr, byte>();

				using var result = img.MatchTemplate(postEmblem, TemplateMatchingType.SqdiffNormed);
				float[,,] matches = result.Data;
				for (int y = 0; y < matches.GetLength(0); y++)
					for (int x = 0; x < matches.GetLength(1); x++)
						if (matches[y, x, 0] < 0.1)
							return Erinn.Posts[i];
            }

			return null;
		}

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
		{
			// Take screenshot and parse the text with opencv
			if (!WinApi.IsMabiActive)
            {
                MessageBox.Show("Mabinogi window has not been found. Re-focus the window and try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
			}

            using var bitmap = CaptureTradeWindow();
            using var img = bitmap.ToImage<Bgr, byte>();
			
			var post = GetPostFromEmblem(img);
			if (post == null)
			{
				MessageBox.Show("Failed to detect trading post. Make sure you are in the trade window.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
			}

			PostSelect.SelectedItem = post;

            img.ROI = ListBounds;
            using var gray = img.Convert<Gray, byte>();
			using var thresholded = gray.ThresholdBinaryInv(new Gray(150), new Gray(255));

			for (int i = 0; i < 7; i++)
			{
				var profit = ScreenParser.ParseProfit(thresholded, new Rectangle(0, i * 16 + 1, 64, 7));
				post.Items[ItemSelect.SelectedIndex].Profits[i].Amount = profit;
			}
        }

		private void SettingsBtn_Click(object sender, RoutedEventArgs e)
			=> new Settings().ShowDialog();

		private void ItemSelect_TargetUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
			=> ItemSelect.SelectedIndex = 0;

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
#if AUTODETECT
			Window_Network_Loaded(sender, e);
#endif

			var newCm = Erinn.CommerceMasteryRanks.FirstOrDefault(r => r.Id == Properties.Settings.Default.CommerceMasteryRankId);

			if (newCm != null)
				Erinn.CmRank = newCm;

			App.Splash.Shutdown();

			// Bring into main view
			App.Current.MainWindow = this;
			App.Current.MainWindow.Activate();
		}

        void Window_Closing(object sender, CancelEventArgs e)
		{
#if AUTODETECT
			Window_Network_Closing(sender, e);
#endif

			Properties.Settings.Default.CommerceMasteryRankId = Erinn.CmRank.Id;
			Properties.Settings.Default.Save();
			Save(null, null);
		}

		private void PriceBox_Focused(object sender, KeyboardFocusChangedEventArgs e)
			=> (e.NewFocus as TextBox)?.SelectAll();

        private void Save(object sender, ExecutedRoutedEventArgs e)
        {
			List<ItemData> itemData = Erinn.Posts
				.SelectMany(post => post.Items, (post, item) => new ItemData {
                    Id = item.Id,
                    Price = item.Price,
                    Profits = item.Profits.Select(profit => new ProfitData {
                        Destination = profit.Destination.Id,
                        Amount = profit.Amount
                    }).ToList()
                }).ToList();

			var vehicles = Erinn.Transports.Where(v => v.Enabled).Select(selector => selector.Id).ToList();
			var modifiers = Erinn.Modifiers.Where(m => m.Enabled).Select(selector => selector.Id).ToList();
			var postData = Erinn.Posts.Select(post => new PostData { Id = post.Id, Level = post.MerchantLevel.Level }).ToList();

            var saveData = new SaveData {
				Ducats = Erinn.Ducats,
				EnabledVehicles = vehicles,
				EnabledModifiers = modifiers,
				Items = itemData,
				Posts = postData,
			};

			var json = JsonSerializer.Serialize(saveData, new JsonSerializerOptions { WriteIndented = true });
			File.WriteAllText("save.json", json);
        }

        private void PriceBox_Focused(object sender, RoutedEventArgs e)
        {
			var textbox = (TextBox)sender;
			textbox.SelectAll();
        }
    }
}
