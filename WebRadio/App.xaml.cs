using DiscordRPC;
using DiscordRPC.Logging;
using Hardcodet.Wpf.TaskbarNotification;
using System;
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

        public void Application_Startup(object sender, StartupEventArgs e)
        {
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
