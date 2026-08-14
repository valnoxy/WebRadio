using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using WebRadio.Common;
using Wpf.Ui.Controls;

namespace WebRadio
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow
    {
        public static ObservableCollection<Radio> RadioList => ConfigManager.Config.RadioList;

        private Radio? _editingRadio;

        public SettingsWindow()
        {
            InitializeComponent();
            Console.WriteLine("Debug console initialized.");

            AutoStartSwitch.IsChecked = ConfigManager.Config.AutoStart;
            DiscordSwitch.IsChecked = ConfigManager.Config.DiscordRPC;
            DataContext = this;

            RadioList.CollectionChanged += RadioList_CollectionChanged;
            Closed += (_, _) => RadioList.CollectionChanged -= RadioList_CollectionChanged;
            UpdateListState();
        }

        private void RadioList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            => UpdateListState();

        private void UpdateListState()
        {
            StreamCountText.Text = RadioList.Count switch
            {
                0 => "No stations yet.",
                1 => "1 station: the order below is used in the tray menu.",
                _ => $"{RadioList.Count} stations: the order below is used in the tray menu."
            };
            EmptyListPanel.Visibility = RadioList.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ShowMessage(InfoBarSeverity severity, string title, string message)
        {
            StatusInfoBar.Severity = severity;
            StatusInfoBar.Title = title;
            StatusInfoBar.Message = message;
            StatusInfoBar.IsOpen = true;
        }

        #region Station editor

        private void EditorField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            SaveRadioBtn_Click(sender, e);
        }

        private void RadioListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RadioListBox.SelectedItem is Radio radio)
                BeginEdit(radio);
        }

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).Tag is Radio radio)
                BeginEdit(radio);
        }

        private void BeginEdit(Radio radio)
        {
            _editingRadio = radio;
            NameTextBox.Text = radio.Name;
            AddressTextBox.Text = radio.Address;
            RadioListBox.SelectedItem = radio;

            EditorTitle.Text = "Edit station";
            EditorSubtitle.Text = $"You are editing \"{radio.Name}\".";
            SaveRadioBtn.Content = "Save changes";
            SaveRadioBtn.Icon = new SymbolIcon(SymbolRegular.Save24);
            CancelEditBtn.Visibility = Visibility.Visible;
            NameTextBox.Focus();
        }

        private void CancelEditBtn_Click(object sender, RoutedEventArgs e) => ResetEditor();

        private void ResetEditor()
        {
            _editingRadio = null;
            NameTextBox.Clear();
            AddressTextBox.Clear();

            EditorTitle.Text = "Add a station";
            EditorSubtitle.Text = "Enter a name and the direct address of the stream.";
            SaveRadioBtn.Content = "Add station";
            SaveRadioBtn.Icon = new SymbolIcon(SymbolRegular.Add24);
            CancelEditBtn.Visibility = Visibility.Collapsed;
        }

        private void SaveRadioBtn_Click(object sender, RoutedEventArgs e)
        {
            var name = NameTextBox.Text.Trim();
            var address = AddressTextBox.Text.Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(address))
            {
                ShowMessage(InfoBarSeverity.Warning, "Missing information",
                    "Please enter both a name and a stream address.");
                return;
            }

            if (!IsValidStreamAddress(address))
            {
                ShowMessage(InfoBarSeverity.Error, "Invalid address",
                    "The address must be a complete URL starting with http:// or https://");
                return;
            }

            if (RadioList.Any(x => x != _editingRadio && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                ShowMessage(InfoBarSeverity.Error, "Name already in use",
                    $"A station called \"{name}\" already exists.");
                return;
            }

            if (RadioList.Any(x => x != _editingRadio && string.Equals(x.Address, address, StringComparison.OrdinalIgnoreCase)))
            {
                ShowMessage(InfoBarSeverity.Error, "Address already in use",
                    "Another station already uses this address.");
                return;
            }

            if (_editingRadio != null)
            {
                _editingRadio.Name = name;
                _editingRadio.Address = address;
                ShowMessage(InfoBarSeverity.Success, "Station updated", $"\"{name}\" has been saved.");
            }
            else
            {
                RadioList.Add(new Radio { Name = name, Address = address });
                ShowMessage(InfoBarSeverity.Success, "Station added", $"\"{name}\" is now available in the tray menu.");
            }

            ResetEditor();
            ConfigManager.SaveConfig();
        }

        private static bool IsValidStreamAddress(string address)
            => Uri.TryCreate(address, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        #endregion

        #region List actions

        private void RemoveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).Tag is not Radio radio) return;

            var confirm = new MessageUi("Remove station",
                $"Do you really want to remove \"{radio.Name}\" from your stream list?", "Remove", "Cancel");
            confirm.ShowDialog();
            if (confirm.Summary != "Btn1") return;

            RadioList.Remove(radio);
            if (_editingRadio == radio) ResetEditor();

            ShowMessage(InfoBarSeverity.Informational, "Station removed", $"\"{radio.Name}\" has been removed.");
            ConfigManager.SaveConfig();
        }

        private void MoveUpBtn_Click(object sender, RoutedEventArgs e) => Move(sender, -1);

        private void MoveDownBtn_Click(object sender, RoutedEventArgs e) => Move(sender, 1);

        private void Move(object sender, int offset)
        {
            if (((FrameworkElement)sender).Tag is not Radio radio) return;

            var index = RadioList.IndexOf(radio);
            var target = index + offset;
            if (index < 0 || target < 0 || target >= RadioList.Count) return;

            RadioList.Move(index, target);
            RadioListBox.SelectedItem = radio;
            ConfigManager.SaveConfig();
        }

        #endregion

        #region Import / Export

        private void ExportBtn_Click(object sender, RoutedEventArgs e)
        {
            if (RadioList.Count == 0)
            {
                ShowMessage(InfoBarSeverity.Warning, "Nothing to export", "Your stream list is empty.");
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "JSON file (*.json)|*.json",
                FileName = "webradio-streams.json",
                AddExtension = true
            };
            if (dialog.ShowDialog(this) != true) return;

            try
            {
                System.IO.File.WriteAllText(dialog.FileName,
                    JsonConvert.SerializeObject(RadioList, Formatting.Indented));
                ShowMessage(InfoBarSeverity.Success, "Export finished",
                    $"{RadioList.Count} station(s) were written to {dialog.FileName}.");
            }
            catch (Exception ex)
            {
                ShowMessage(InfoBarSeverity.Error, "Export failed", ex.Message);
            }
        }

        private void ImportBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON file (*.json)|*.json",
                CheckFileExists = true
            };
            if (dialog.ShowDialog(this) != true) return;

            List<Radio>? imported;
            try
            {
                imported = JsonConvert.DeserializeObject<List<Radio>>(System.IO.File.ReadAllText(dialog.FileName));
            }
            catch (Exception ex)
            {
                ShowMessage(InfoBarSeverity.Error, "Import failed", ex.Message);
                return;
            }

            if (imported == null || imported.Count == 0)
            {
                ShowMessage(InfoBarSeverity.Warning, "Nothing imported", "The selected file does not contain any stations.");
                return;
            }

            var added = 0;
            var skipped = 0;
            foreach (var radio in imported)
            {
                if (string.IsNullOrWhiteSpace(radio.Name) || !IsValidStreamAddress(radio.Address) ||
                    RadioList.Any(x => string.Equals(x.Name, radio.Name, StringComparison.OrdinalIgnoreCase)
                                       || string.Equals(x.Address, radio.Address, StringComparison.OrdinalIgnoreCase)))
                {
                    skipped++;
                    continue;
                }

                RadioList.Add(radio);
                added++;
            }

            ShowMessage(added > 0 ? InfoBarSeverity.Success : InfoBarSeverity.Warning, "Import finished",
                $"{added} station(s) added, {skipped} skipped (duplicates or invalid entries).");

            if (added > 0) ConfigManager.SaveConfig();
        }

        #endregion

        #region General settings

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
                ShowMessage(InfoBarSeverity.Error, "Autostart could not be changed", ex.Message);
            }

            ConfigManager.SaveConfig();
        }

        #endregion
    }
}
