using DiscordRPC;
using ManagedBass;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using WebRadio.Common;
using Wpf.Ui.Controls;
using Button = System.Windows.Controls.Button;
using Timer = System.Timers.Timer;

namespace WebRadio
{
    /// <summary>
    /// Interaktionslogik für ControlWindow.xaml
    /// </summary>
    public partial class ControlWindow
    {
        private int _streamHandle;
        private int _startGeneration;
        public readonly Timer? MetadataTimer;
        private string _title = string.Empty;
        private string _streamUrl = string.Empty;
        private bool _isPlaying;
        private double Volume { get; set; }

        public ControlWindow()
        {
            InitializeComponent();
            var bassInitialized = Bass.Init();
            if (!bassInitialized)
            {
                Console.WriteLine("Error while initializing ManagedBass");
                return;
            }
            Bass.NetPlaylist = 1;

            MetadataTimer = new Timer(3000);
            MetadataTimer.Elapsed += MetadataTimer_Elapsed!;
            
            // Set volume
            Volume = Math.Clamp(ConfigManager.Config.Volume, 0, 100);
            VolumeSlider.Value = Volume;
            VolumeText.Text = $"{Math.Round(Volume)}%";

            // Load radio list
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
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Console.WriteLine("Stream Title: " + _title);
                        PlayingButton.Content = _title;
                        PlayingButton.Icon = new SymbolIcon(SymbolRegular.Speaker224);
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
                         Icon = new SymbolIcon(SymbolRegular.MusicNote2Play20),
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

            UpdateActiveStation();
        }

        private void UpdatePlaybackState()
        {
            PlayPauseButton.IsEnabled = !string.IsNullOrEmpty(_streamUrl);
            PlayPauseButton.Icon = new SymbolIcon(_isPlaying ? SymbolRegular.Pause24 : SymbolRegular.Play24);
            PlayPauseButton.ToolTip = _isPlaying ? "Pause" : "Play";
            PlayingButton.Icon = new SymbolIcon(_isPlaying ? SymbolRegular.Speaker224 : SymbolRegular.Info24);

            UpdateActiveStation();
        }

        private void UpdateActiveStation()
        {
            foreach (var button in PlayList.Children.OfType<Wpf.Ui.Controls.Button>())
            {
                var isActive = button.Tag as string == _streamUrl && !string.IsNullOrEmpty(_streamUrl);

                button.Appearance = isActive && _isPlaying
                    ? ControlAppearance.Primary
                    : ControlAppearance.Secondary;
                button.Icon = new SymbolIcon(isActive
                    ? _isPlaying ? SymbolRegular.Speaker224 : SymbolRegular.PauseCircle20
                    : SymbolRegular.MusicNote2Play20);
                button.FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal;
            }
        }

        private void StopStream()
        {
            Console.WriteLine("Stopping stream");
            Bass.ChannelPause(_streamHandle);
            MetadataTimer!.Stop();

            _title = string.Empty;
            PlayingButton.Content = "Not playing";

            Console.WriteLine("Stream paused.");
            _isPlaying = false;
            UpdatePlaybackState();

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

        private void FreeCurrentStream()
        {
            if (_streamHandle == 0) return;

            MetadataTimer!.Stop();
            Bass.StreamFree(_streamHandle);
            _streamHandle = 0;
            _isPlaying = false;
            _title = string.Empty;
        }

        private void StartStream()
        {
            var generation = Interlocked.Increment(ref _startGeneration);
            FreeCurrentStream();

            var url = _streamUrl;
            Console.WriteLine("Playing stream: " + url);
            PlayingButton.Content = "Connecting ...";
            UpdatePlaybackState();

            Task.Factory.StartNew(() =>
            {
                var handle = Bass.CreateStream(url, 0,
                    BassFlags.StreamDownloadBlocks | BassFlags.StreamStatus | BassFlags.AutoFree, null,
                    new IntPtr(0));
                var error = Bass.LastError;

                if (generation != Volatile.Read(ref _startGeneration))
                {
                    Console.WriteLine("Discarding superseded stream request: " + url);
                    if (handle != 0) Bass.StreamFree(handle);
                    return;
                }

                if (handle != 0)
                {
                    _streamHandle = handle;
                    Bass.ChannelPlay(handle);
                    Bass.ChannelSetSync(handle, SyncFlags.MetadataReceived, 0, MetadataSyncCallback);
                    MetadataTimer!.Start();
                    Console.WriteLine("Stream started.");
                    _isPlaying = true;

                    // Set current volume
                    ApplyVolume();

                    var station = ConfigManager.Config.RadioList.FirstOrDefault(r => r.Address == url);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Show the station until the first metadata of the stream arrives.
                        if (string.IsNullOrEmpty(_title))
                            PlayingButton.Content = station?.Name ?? "Playing";
                        UpdatePlaybackState();
                    });

                    if (ConfigManager.Config.DiscordRPC)
                        DiscordRpc._currentSenderName = station?.Name ?? "Unknown station";
                }
                else
                {
                    Console.WriteLine("Failed to start the stream: " + error);
                    _isPlaying = false;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        PlayingButton.Content = "Not playing";
                        UpdatePlaybackState();

                        var errorMessage = error switch
                        {
                            Errors.FileFormat => "Failed to fetch stream: The format of this stream is not supported.",
                            Errors.FileOpen or Errors.Timeout => "Failed to fetch stream: The server could not be reached.",
                            Errors.SSL => "Failed to fetch stream: The secure connection to the server failed.",
                            _ => $"Failed to fetch stream: This stream is not supported ({error})."
                        };
                        var messageUi = new MessageUi("WebRadio", errorMessage, "OK");
                        messageUi.ShowDialog();
                    });
                }
            });
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Store the current volume in the config
            ConfigManager.Config.Volume = Volume;
            ConfigManager.SaveConfig();

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

        private void VolumeSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Volume = e.NewValue;
            VolumeText.Text = $"{Math.Round(Volume)}%";
            ApplyVolume();
        }

        private void ApplyVolume()
        {
            if (_streamHandle == 0) return;
            Bass.ChannelSetAttribute(_streamHandle, ChannelAttribute.Volume, Volume / 100.0);
        }
    }
}
