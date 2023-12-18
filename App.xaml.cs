using MabiCommerce.Domain;
using MabiCommerce.UI;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MabiCommerce
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public static Splash Splash;

        public static MessageBoxResult MessageBox(string message, string caption = "MabiCommerce",
            MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.Information,
            MessageBoxResult defaultResult = MessageBoxResult.OK)
        {
            return System.Windows.MessageBox.Show(message, caption, buttons, image, defaultResult, MessageBoxOptions.DefaultDesktopOnly);
        }

        private const string EngData = @"https://raw.githubusercontent.com/tesseract-ocr/tessdata/main/eng.traineddata";
        private const string VersionData = @"https://raw.githubusercontent.com/Xcelled/mabicommerce/master/latest";

        private ManualResetEvent? _resetSplashCreated;
        private Thread? _splashThread;
        protected override void OnStartup(StartupEventArgs e)
        {
#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
#endif

            // ManualResetEvent acts as a block.  It waits for a signal to be set.
            _resetSplashCreated = new ManualResetEvent(false);

            // Create a new thread for the splash screen to run on
            _splashThread = new Thread(ShowSplash);
            _splashThread.SetApartmentState(ApartmentState.STA);
            _splashThread.IsBackground = true;
            _splashThread.Start();

            // Wait for the blocker to be signaled before continuing. This is essentially the same as: while(ResetSplashCreated.NotSet) {}
            _resetSplashCreated.WaitOne();

            base.OnStartup(e);

            // Download Tessdata
            if (!Directory.Exists("./tessdata"))
                Directory.CreateDirectory("./tessdata");

            if (File.Exists("./tessdata/eng.traineddata"))
            {
                FileInfo info = new("./tessdata/eng.traineddata");
                if (info.Length < 1024 * 1024 * 20)
                    File.Delete("./tessdata/eng.traineddata");
            }

            Task<Task>? download = null;
            if (!File.Exists("./tessdata/eng.traineddata"))
            {
                using var file = File.Create("./tessdata/eng.traineddata");
                using HttpClient wc = new();
                try
                {
                    var data = wc.GetAsync(EngData);
                    data.Wait();
                    data.Result.EnsureSuccessStatusCode();

                    download = data.Result.Content.ReadAsStreamAsync()
                        .ContinueWith(stream => stream.Result.CopyToAsync(file));
                } catch (Exception ex)
                {
                    MessageBox("Could not download Tessdata. Please download it manually from " + EngData
                        + " and place it in the tessdata folder.\n\n" + ex.Message, "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // Environment.CurrentDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);

            if (MabiCommerce.Properties.Settings.Default.UpdateCheck)
                Task.Factory.StartNew(CheckForUpdates);

            download?.Wait();

            Erinn? erinn;
            try
            {
                if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                {
                    if (!Directory.Exists("data"))
                        Environment.CurrentDirectory = Path.GetFullPath("MabiCommerce");

                    erinn = new("data");
                } else
                    erinn = new(@"Data", Splash.ReportProgress);

                Splash.ReportProgress(1.0, "Loading main window...");
                var mw = new MainWindow(erinn);
                mw.Show();
            } catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is not Exception ex)
                return;

            var w = new UnhandledExceptionWindow(ex);
            w.ShowDialog();
            Shutdown();
            Environment.Exit(1);
        }

        private static async void CheckForUpdates()
        {
            await Task.Delay(5000);

            using var wc = new HttpClient();

            try
            {
                var version = await wc.GetAsync(VersionData)
                    .ContinueWith(x => x.Result.Content.ReadAsStringAsync())
                    .Unwrap();
                var latest = Version.Parse(version);
                var current = typeof(Settings).Assembly.GetName().Version;
                if (current < latest)
                {
                    if (MessageBox(string.Format(@"There is a new version of MabiCommerce available!

You're running: {0}
Latest: {1}

Would you like to download the new version?", current, latest), "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    {
                        Process.Start(@"https://github.com/Xcelled/mabicommerce");
                    }
                }
            } catch
            {

            }

        }

        private void ShowSplash()
        {
            // Create the window
            Splash = new Splash();

            // Show it
            Splash.Show();

            // Now that the window is created, allow the rest of the startup to run
            _resetSplashCreated?.Set();
            System.Windows.Threading.Dispatcher.Run();
        }
    }
}
