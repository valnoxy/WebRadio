using DiscordRPC;
using DiscordRPC.Logging;
using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using WebRadio.Common;

namespace WebRadio
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public static ControlWindow controlWindow;
        public static TaskbarIcon tbIcon;
        public static Mutex mutex;

        public void Application_Startup(object sender, StartupEventArgs e)
        {
            mutex = new Mutex(true, "valnoxyWebRadio", out var createdNew);
            if (!createdNew)
            {
                MessageBox.Show("WebRadio is already open.");
                return;
            }

            ConfigManager.Initialize();

            // Create ControlWindow
            tbIcon = new TaskbarIcon
            {
                IconSource = new BitmapImage(new Uri("pack://application:,,,/Assets/WebRadio.ico")),
                ToolTipText = "WebRadio"
            };
            controlWindow = new ControlWindow
            {
                MinWidth = 400
            };
            tbIcon.TrayPopup = controlWindow;
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }
    }
}
