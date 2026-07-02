using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using YouTubeVideoUploader.Domain.Interfaces;
using YouTubeVideoUploader.Domain.ValueObjects;
using YouTubeVideoUploader.UI.Resources;

namespace YouTubeVideoUploader.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IPresetStore _presetStore;
    private readonly IFileSystemRepository _fileSystemRepository;

    [ObservableProperty]
    private string _clientSecretPath = "client_secret.json";

    [ObservableProperty]
    private string _selectedLanguage = "en";

    public ObservableCollection<string> AvailableLanguages { get; } = new() { "en", "uk" };
    public ObservableCollection<Preset> Presets { get; } = new();

    public LanguageManager L => LanguageManager.Instance;

    public SettingsViewModel(
        IPresetStore presetStore,
        IFileSystemRepository fileSystemRepository)
    {
        _presetStore = presetStore ?? throw new ArgumentNullException(nameof(presetStore));
        _fileSystemRepository = fileSystemRepository ?? throw new ArgumentNullException(nameof(fileSystemRepository));

        SelectedLanguage = LanguageManager.Instance.CurrentLanguage;
        
        // Try locating client_secret.json in app directory as default
        string defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "client_secret.json");
        if (File.Exists(defaultPath))
        {
            ClientSecretPath = defaultPath;
        }

        LoadPresets();
    }

    private void LoadPresets()
    {
        Presets.Clear();
        foreach (var preset in _presetStore.GetAll())
        {
            Presets.Add(preset);
        }
    }

    [RelayCommand]
    private void BrowseClientSecret()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "JSON Files (*.json)|*.json",
            Title = "Select client_secret.json"
        };
        if (dialog.ShowDialog() == true)
        {
            ClientSecretPath = dialog.FileName;
            
            // Try copy to app dir for convenience
            try
            {
                string dest = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "client_secret.json");
                if (dialog.FileName != dest)
                {
                    File.Copy(dialog.FileName, dest, overwrite: true);
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to copy client_secret.json to local directory.");
            }
        }
    }

    [RelayCommand]
    private void ChangeLanguage(string? lang)
    {
        if (string.IsNullOrEmpty(lang)) return;
        SelectedLanguage = lang;
        LanguageManager.Instance.CurrentLanguage = lang;
    }

    [RelayCommand]
    private void DeletePreset(Preset? preset)
    {
        if (preset == null) return;
        
        var result = MessageBox.Show($"{L.Delete} {preset.Name}?", L.AppTitle, MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            _presetStore.Delete(preset.Id);
            LoadPresets();
        }
    }

    [RelayCommand]
    private void ExportPresets()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "JSON Files (*.json)|*.json",
            FileName = "presets_backup.json",
            Title = L.ExportPreset
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var presets = _presetStore.GetAll();
                string json = JsonSerializer.Serialize(presets, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(dialog.FileName, json);
                MessageBox.Show(L.Success, L.AppTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, L.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private void ImportPresets()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "JSON Files (*.json)|*.json",
            Title = L.ImportPreset
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                string json = File.ReadAllText(dialog.FileName);
                var imported = JsonSerializer.Deserialize<List<Preset>>(json);
                if (imported != null)
                {
                    foreach (var preset in imported)
                    {
                        // Save each preset, generating new GUIDs or keeping existing
                        _presetStore.Save(preset);
                    }
                    LoadPresets();
                    MessageBox.Show(L.Success, L.AppTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, L.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
