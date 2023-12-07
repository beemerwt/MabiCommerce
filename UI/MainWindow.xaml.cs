using System;
using System.ComponentModel;
using System.Drawing.Imaging;
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
using Emgu.CV.Text;
using System.Collections.Generic;
using Emgu.CV.CvEnum;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace MabiCommerce.UI
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public static RoutedCommand SaveCommand = new RoutedCommand();

		public Erinn Erinn { get; private set; }

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

			DataContext = Erinn = e;
			SaveCommand.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
		}

		private void WindowBar_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				DragMove();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void MinimizeButton_Click(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState.Minimized;
		}

		private void CalculateTrades_Click(object sender, RoutedEventArgs e)
		{
			CalculateTrades();
		}

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

        /*

        private Rectangle ListBounds = new Rectangle(1095, 429, 45, 178);
        private Bitmap CaptureGame()
		{
            Rectangle bounds = Screen.GetBounds(System.Drawing.Point.Empty);
			Bitmap bitmap = new Bitmap(ListBounds.Width, ListBounds.Height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(ListBounds.Left, ListBounds.Top, 0, 0, ListBounds.Size);
            }
			return bitmap;
        }

        public List<Rectangle> DetectLetters(Image<Bgr, byte> img)
        {
            List<Rectangle> rects = new List<Rectangle>();
            using Image<Gray, byte> img_gray = img.Convert<Gray, byte>();
            using Image<Gray, float> img_sobel = img_gray.Sobel(1, 0, 3);
            img_threshold = new Image<Gray, byte>(img_sobel.Size);
            CvInvoke.Threshold(img_sobel.Convert<Gray, byte>(), img_threshold, 0, 255, ThresholdType.Otsu);

			System.Drawing.Point anchor = new System.Drawing.Point(1, 0);
            Mat element = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new System.Drawing.Size(6, 8), anchor);
			CvInvoke.MorphologyEx(img_threshold, img_threshold, MorphOp.Close, element, anchor, 1, BorderType.NegativeOne, new MCvScalar(1));

            Emgu.CV.Util.VectorOfVectorOfPoint contours = new Emgu.CV.Util.VectorOfVectorOfPoint();
			Mat hier = new Mat();
			CvInvoke.FindContours(img_threshold, contours, hier, RetrType.External, ChainApproxMethod.ChainApproxSimple);

            for (Contour<System.Drawing.Point> contours = img_threshold.FindContours(); contours != null; contours = contours.HNext)
            {
                if (contours.Area > 100)
                {
                    Contour<System.Drawing.Point> contours_poly = contours.ApproxPoly(3);
                    rects.Add(new Rectangle(contours_poly.BoundingRectangle.X, contours_poly.BoundingRectangle.Y, contours_poly.BoundingRectangle.Width, contours_poly.BoundingRectangle.Height));
                }
            }
            return rects;
        }
		*/

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
		{
			// Take screenshot and parse the text with opencv
			// if (!WinApi.IsMabiActive)
               // return;
        }

		private void SettingsBtn_Click(object sender, RoutedEventArgs e)
		{
			new Settings().ShowDialog();
		}

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
			var profitData = new List<ProfitData>();
			foreach (var post in Erinn.Posts)
			{
				foreach (var item in post.Items)
				{
                    foreach (var profit in item.Profits)
					{
                        profitData.Add(new ProfitData
						{
                            ItemId = item.Id,
                            DestinationId = profit.Destination.Id,
                            Profit = profit.Amount
                        });
                    }
                }
			}

			var json = JsonSerializer.Serialize(profitData, new JsonSerializerOptions { WriteIndented = true });
			File.WriteAllText("profits.json", json);
        }
    }

	[JsonSerializable(typeof(ProfitData))]
	class ProfitData {
		public int ItemId { get; set; }
        public int DestinationId { get; set; }
        public int Profit { get; set; }
	};
}
