using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using VirtualController.Profiles;

namespace VirtualController.App.Views
{
    public partial class ProfilesView : UserControl
    {
        private readonly ProfileManager _profileManager;
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public ProfilesView(ProfileManager profileManager)
        {
            InitializeComponent();
            _profileManager = profileManager;

            _profileManager.ActiveProfileChanged += (s, p) => Dispatcher.Invoke(RefreshUI);
            Loaded += (s, e) => RefreshUI();
        }

        private bool _isUpdatingUi;

        public void RefreshUI()
        {
            if (_isUpdatingUi) return;
            _isUpdatingUi = true;
            try
            {
                ProfilesListBox.ItemsSource = _profileManager.Profiles;
                ProfilesListBox.SelectedItem = _profileManager.ActiveProfile;

                UpdatePreview();
            }
            finally
            {
                _isUpdatingUi = false;
            }
        }

        private void ProfilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (ProfilesListBox.SelectedItem is Profile selected)
            {
                TxtJsonPreview.Text = JsonSerializer.Serialize(selected, JsonOptions);
            }
            else
            {
                TxtJsonPreview.Text = "";
            }
        }

        private void BtnSetActive_Click(object sender, RoutedEventArgs e)
        {
            if (ProfilesListBox.SelectedItem is Profile selected)
            {
                _profileManager.SetActiveProfile(selected);
                MessageBox.Show($"Active profile set to '{selected.Name}'.", "Profile Activated", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnNewProfile_Click(object sender, RoutedEventArgs e)
        {
            string name = $"Custom Profile {_profileManager.Profiles.Count + 1}";
            var profile = _profileManager.CreateProfile(name);
            RefreshUI();
            ProfilesListBox.SelectedItem = profile;
        }

        private void BtnDuplicate_Click(object sender, RoutedEventArgs e)
        {
            if (ProfilesListBox.SelectedItem is Profile selected)
            {
                string dupName = $"{selected.Name} (Copy)";
                var dup = _profileManager.DuplicateProfile(selected, dupName);
                RefreshUI();
                ProfilesListBox.SelectedItem = dup;
            }
        }

        private void BtnRename_Click(object sender, RoutedEventArgs e)
        {
            if (ProfilesListBox.SelectedItem is Profile selected)
            {
                var window = Window.GetWindow(this);
                string? newName = InputBoxDialog.Show(window, "Enter new profile name:", "Rename Profile", selected.Name);
                if (!string.IsNullOrWhiteSpace(newName))
                {
                    _profileManager.RenameProfile(selected, newName);
                    RefreshUI();
                }
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (ProfilesListBox.SelectedItem is Profile selected)
            {
                if (_profileManager.Profiles.Count <= 1)
                {
                    MessageBox.Show("Cannot delete the only remaining profile.", "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show($"Are you sure you want to delete profile '{selected.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _profileManager.DeleteProfile(selected);
                    RefreshUI();
                }
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (ProfilesListBox.SelectedItem is Profile selected)
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "JSON Profile (*.json)|*.json",
                    FileName = $"{selected.Name}.json"
                };

                if (dialog.ShowDialog() == true)
                {
                    _profileManager.ExportProfile(selected, dialog.FileName);
                    MessageBox.Show($"Exported successfully to {dialog.FileName}", "Exported", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON Profile (*.json)|*.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var imported = _profileManager.ImportProfile(dialog.FileName);
                    RefreshUI();
                    ProfilesListBox.SelectedItem = imported;
                    MessageBox.Show($"Imported profile '{imported.Name}' successfully.", "Imported", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to import profile: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
