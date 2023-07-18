using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Permissions;
using System.Windows;
using Microsoft.Win32;
using WebRadio.Common;

namespace WebRadio
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow
    {
        public static List<Radio> RadioList => ConfigManager.Config.RadioList;

        public SettingsWindow()
        {
            InitializeComponent();
            Console.WriteLine("Debug console initialized.");

            AutoStartSwitch.IsChecked = ConfigManager.Config.AutoStart;
            DiscordSwitch.IsChecked = ConfigManager.Config.DiscordRPC;
            DataContext = this;
        }

        private void AddRadioBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(NameTextBox.Text) || string.IsNullOrEmpty(AddressTextBox.Text))
            {
                Console.WriteLine("Cannot save radio: Fields are empty.");
                return;
            }

            if (ConfigManager.Config.RadioList.Exists(x => x.Address == AddressTextBox.Text))
            {
                Console.WriteLine("Cannot save radio: Address already added.");
                return;
            }

            if (ConfigManager.Config.RadioList.Exists(x => x.Name == NameTextBox.Text))
            {
                Console.WriteLine("Cannot save radio: Name already added.");
                return;
            }

            ConfigManager.Config.RadioList.Add(new Radio{Address = AddressTextBox.Text, Name = NameTextBox.Text});
            Application.Current.Dispatcher.Invoke(() =>
            {
                RadioListBox.Items.Refresh();
            });
            NameTextBox.Clear();
            AddressTextBox.Clear();

            ConfigManager.SaveConfig();
        }

        private void RemoveRadioBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = (Radio)RadioListBox.SelectedItem;
            if (selectedItem == null) return;
            ConfigManager.Config.RadioList.RemoveAll(x => x.Name == selectedItem.Name);
            Application.Current.Dispatcher.Invoke(() =>
            {
                RadioListBox.Items.Refresh();
            });
            NameTextBox.Clear();
            AddressTextBox.Clear();

            ConfigManager.SaveConfig();
        }

        private void DiscordSwitch_OnClick(object sender, RoutedEventArgs e)
        {
            ConfigManager.Config.DiscordRPC = (bool)DiscordSwitch.IsChecked!;
            ConfigManager.SaveConfig();
        }

        private void AutoStartSwitch_OnClick(object sender, RoutedEventArgs e)
        {
            ConfigManager.Config.AutoStart = (bool)AutoStartSwitch.IsChecked!;

            try
            {
                var rk = Registry.CurrentUser.OpenSubKey
                    ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                if ((bool)AutoStartSwitch.IsChecked)
                    rk!.SetValue("WebRadio", Environment.ProcessPath!);
                else
                    rk!.DeleteValue("WebRadio", false);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to set autostart key: " + ex.Message);
            }


            ConfigManager.SaveConfig();
        }
    }
}
