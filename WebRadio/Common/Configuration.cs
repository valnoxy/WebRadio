using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace WebRadio.Common
{
    public class Configuration
    {
        public bool AutoStart { get; set; }
        public bool DiscordRPC { get; set; }
        public ObservableCollection<Radio> RadioList { get; set; } = new();
        public double Volume { get; set; } = 100;
    }

    public class Radio : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _address = string.Empty;

        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }

        public string Address
        {
            get => _address;
            set => SetField(ref _address, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SetField(ref string field, string value, [CallerMemberName] string? propertyName = null)
        {
            if (field == value) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ConfigManager
    {
        private static readonly string AppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private static readonly string ConfigDirectory = Path.Combine(AppData, "valnoxy", "WebRadio");
        private static readonly string ConfigFile = Path.Combine(ConfigDirectory, "config.json");
        public static Configuration Config = new();
        private static bool _isDiscordRpcRunning;
        public static bool _DiscordRpcFirstRun;

        public static void Initialize()
        {
            try
            {
                Directory.CreateDirectory(ConfigDirectory);
                if (File.Exists(ConfigFile))
                {
                    try
                    {
                        var jsonContent = File.ReadAllText(ConfigFile);
                        Config = JsonConvert.DeserializeObject<Configuration>(jsonContent)!;
                        Config.RadioList ??= new ObservableCollection<Radio>();

                        if (Config.DiscordRPC)
                        {
                            DiscordRpc.Initialize();
                            _isDiscordRpcRunning = true;
                        }
                    }
                    catch
                    {
                        Console.WriteLine("Failed to import existing config.");
                    }
                }
                else
                {
                    Config.RadioList = new ObservableCollection<Radio>();
                }
            }
            catch
            {
                Console.WriteLine("Failed to initialize config.");
            }
        }

        public static void SaveConfig()
        {
            var jsonConfig = JsonConvert.SerializeObject(Config);
            try
            {
                File.WriteAllText(ConfigFile, jsonConfig);
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    App.controlWindow.ReloadRadioList();
                });
                if (Config.DiscordRPC)
                {
                    Console.WriteLine("Enabling Discord RPC ...");
                    if (!_isDiscordRpcRunning)
                    {
                        DiscordRpc.Initialize();
                        _DiscordRpcFirstRun = true;
                        _isDiscordRpcRunning = true;
                    }
                }
                else
                {
                    Console.WriteLine("Disabling Discord RPC ...");
                    if (_isDiscordRpcRunning)
                    {
                        DiscordRpc.Dispose();
                        _isDiscordRpcRunning = false;
                    }
                }
            }
            catch
            {
                Console.WriteLine("Failed to save config.");
            }
        }
    }
}
