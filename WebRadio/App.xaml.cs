using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
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
                var errorMessage = "WebRadio is already running!";
                var messageUi = new MessageUi("WebRadio", errorMessage, "OK");
                messageUi.ShowDialog();
                Current.Shutdown();
            }

            ConfigManager.Initialize();

            // Create ControlWindow
            tbIcon = new TaskbarIcon
            {
                IconSource = new BitmapImage(new Uri("pack://application:,,,/Assets/WebRadio.ico")),
                ToolTipText = "WebRadio",
            };
            controlWindow = new ControlWindow
            {
                MinWidth = 330,
                MaxWidth = 330
            };
            tbIcon.TrayPopup = controlWindow;
            tbIcon.PopupActivation = PopupActivationMode.LeftOrRightClick;
            tbIcon.PreviewTrayPopupOpen += TrayIcon_PreviewTrayPopupOpen;
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        private static void TrayIcon_PreviewTrayPopupOpen(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            ShowPopupNextToTray();
        }

        private static void ShowPopupNextToTray()
        {
            if (tbIcon.TrayPopupResolved is not { Child: FrameworkElement content } popup) return;

            content.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var width = content.DesiredSize.Width > 0 ? content.DesiredSize.Width : content.ActualWidth;
            var height = content.DesiredSize.Height > 0 ? content.DesiredSize.Height : content.ActualHeight;
            var workArea = SystemParameters.WorkArea;
            const double margin = 12;
            var taskbarOnLeft = workArea.Left > 0;
            var taskbarOnTop = workArea.Top > 0;

            popup.Placement = PlacementMode.AbsolutePoint;
            popup.HorizontalOffset = taskbarOnLeft
                ? workArea.Left + margin
                : workArea.Right - width - margin;
            popup.VerticalOffset = taskbarOnTop
                ? workArea.Top + margin
                : workArea.Bottom - height - margin;
            popup.IsOpen = true;

            // Activate the popup so it closes again when the user clicks somewhere else.
            if (PresentationSource.FromVisual(popup.Child) is HwndSource source)
                SetForegroundWindow(source.Handle);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
