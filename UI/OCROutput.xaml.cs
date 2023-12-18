using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace MabiCommerce.UI
{
    /// <summary>
    /// Interaction logic for OCROutput.xaml
    /// </summary>
    public partial class OCROutput : Window
    {
        public OCROutput(Bitmap captured, string text)
        {
            InitializeComponent();

            using (MemoryStream memory = new())
            {
                captured.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                CaptureGray.Source = bitmapimage;
            }

            IdentifiedText.Text = text;
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            // Dispose of the bitmap
        }
    }
}
