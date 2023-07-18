using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace WebRadio.Common
{
    public class Configuration
    {
        public bool AutoStart { get; set; }
        public bool DiscordRPC { get; set; }
        public List<Radio> RadioList { get; set; }
    }

    public class Radio
    {
        public string Name { get; set; }
        public string Address { get; set; }
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
                    ConfigManager.Config.RadioList = new List<Radio>();
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
