using DiscordRPC;
using ManagedBass;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using WebRadio.Common;
using Wpf.Ui.Common;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Button = System.Windows.Controls.Button;

namespace WebRadio
{
    /// <summary>
    /// Interaktionslogik für ControlWindow.xaml
    /// </summary>
    public partial class ControlWindow
    {
        private int _streamHandle;
        public readonly System.Timers.Timer? MetadataTimer;
        private string _title = string.Empty;
        private string _streamUrl = string.Empty;
        private bool _isPlaying;

        public ControlWindow()
        {
            InitializeComponent();
            var bassInitialized = Bass.Init();
            if (!bassInitialized)
            {
                Console.WriteLine("Error while initializing ManagedBass");
                return;
            }
            MetadataTimer = new System.Timers.Timer(3000);
            MetadataTimer.Elapsed += MetadataTimer_Elapsed!;
            ReloadRadioList();
        }

        private void SetStreamUrl(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;
            var data = button.Tag as string;
            var result = Uri.TryCreate(data, UriKind.Absolute, out var uriResult)
                         && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            if (!result)
            {
                Console.WriteLine("Parsed URL is invalid!\nURL: " + data);
                return;
            }

            if (_isPlaying)
                StopStream();

            _streamUrl = data!;
            StartStream();
        }

        private void MetadataTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_streamHandle != 0)
            {
                var meta = Bass.ChannelGetTags(_streamHandle, TagType.META);
                if (meta != IntPtr.Zero)
                {
                    var data = Marshal.PtrToStringAnsi(meta);
                    var i = data!.IndexOf("StreamTitle='", StringComparison.Ordinal); // locate the title
                    if (i == -1) return;

                    var j = data.IndexOf("';", i, StringComparison.Ordinal); // locate the end of it
                    if (j == -1) return;

                    var title = data.Substring(i + 13, j - i - 13);
                    if (title == _title && ConfigManager._DiscordRpcFirstRun == false) return;

                    _title = title;
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        Console.WriteLine("Stream Title: " + _title);
                        PlayingButton.Content = _title;
                        PlayingButton.Icon = SymbolRegular.Pause24;
                        App.tbIcon.ToolTipText = _title;
                    });

                    if (ConfigManager.Config.DiscordRPC)
                    {
                        DiscordRpc.client.SetPresence(new RichPresence
                        {
                            Details = _title,
                            State = DiscordRpc._currentSenderName,
                            Assets = new Assets
                            {
                                LargeImageKey = "webradio",
                                SmallImageKey = "valnoxy"
                            }
                        });
                        ConfigManager._DiscordRpcFirstRun = false;
                    }
                }
                else
                {
                    Console.WriteLine("No metadata available for the stream.");
                }
            }
            else
            {
                Console.WriteLine("The stream is not currently playing.");
            }
        }

        private void MetadataSyncCallback(int handle, int channel, int data, IntPtr user)
        {
            MetadataTimer_Elapsed(null!, null!);
        }

        private void PlayingButton_Click(object sender, RoutedEventArgs e)
        {
            switch (_isPlaying)
            {
                case false:
                {
                    if (string.IsNullOrEmpty(_streamUrl)) return;
                    StartStream();
                    return;
                }
                case true:
                {
                    if (_streamHandle == 0) return;
                    StopStream();
                    return;
                }
            }
        }

        public void ReloadRadioList()
        {
            var playList = this.PlayList;
            playList.Children.Clear();
            foreach (var button in ConfigManager.Config.RadioList.Select(sender => new Wpf.Ui.Controls.Button()
                     {
                         Icon = SymbolRegular.MusicNote2Play20,
                         Content = sender.Name,
                         HorizontalAlignment = HorizontalAlignment.Stretch,
                         Margin = new Thickness(0, 0, 0, 5),
                         HorizontalContentAlignment = HorizontalAlignment.Left,
                         Tag = sender.Address
                     }))
            {
                button.Click += SetStreamUrl;
                playList.Children.Add(button);
            }
        }

        private void StopStream()
        {
            Console.WriteLine("Stopping stream");
            Bass.ChannelPause(_streamHandle);
            MetadataTimer!.Stop();

            _title = string.Empty;
            PlayingButton.Content = "Not playing";
            PlayingButton.Icon = SymbolRegular.Info24;

            Console.WriteLine("Stream paused.");
            _isPlaying = false;

            if (ConfigManager.Config.DiscordRPC)
            {
                DiscordRpc.client.SetPresence(new RichPresence
                {
                    Details = "Idle",
                    State = "No sender currently playing.",
                    Assets = new Assets
                    {
                        LargeImageKey = "webradio",
                        SmallImageKey = "valnoxy"
                    }
                });
                App.tbIcon.ToolTipText = "WebRadio";
            }
        }

        private void StartStream()
        {
            Console.WriteLine("Playing stream: " + _streamUrl);
            Task.Factory.StartNew(() =>
            {
                _streamHandle = Bass.CreateStream(_streamUrl, 0, BassFlags.Default, null, IntPtr.Zero);

                if (_streamHandle != 0)
                {
                    Bass.ChannelPlay(_streamHandle);
                    Bass.ChannelSetSync(_streamHandle, SyncFlags.MetadataReceived, 0, MetadataSyncCallback);
                    MetadataTimer!.Start();
                    Console.WriteLine("Stream started.");
                    _isPlaying = true;

                    if (ConfigManager.Config.DiscordRPC)
                    {
                        try
                        {
                            var senderObj = ConfigManager.Config.RadioList.FirstOrDefault(r => r.Address == _streamUrl);
                            DiscordRpc._currentSenderName = senderObj!.Name;
                        }
                        catch
                        {
                            Console.WriteLine("Failed to get sender name from list.");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Failed to start the stream.");
                    _isPlaying = false;
                }
            });
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            App.mutex.ReleaseMutex();
            Application.Current.Shutdown();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        }
        
        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        }
    }
}
