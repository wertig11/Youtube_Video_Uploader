using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using YouTubeVideoUploader.Domain.Interfaces;
using YouTubeVideoUploader.Domain.ValueObjects;
using YouTubeVideoUploader.UI.Resources;

namespace YouTubeVideoUploader.UI.ViewModels;

public partial class RenameViewModel : ObservableObject
{
    private readonly IRenameService _renameService;
    private readonly IPresetStore _presetStore;
    private readonly IEnumerable<IRenameStrategy> _strategies;

    [ObservableProperty]
    private string _folderPath = string.Empty;

    [ObservableProperty]
    private string _templatePattern = string.Empty;

    [ObservableProperty]
    private string _namesText = string.Empty;

    [ObservableProperty]
    private IRenameStrategy? _selectedStrategy;

    [ObservableProperty]
    private string _presetName = string.Empty;

    public ObservableCollection<IRenameStrategy> AvailableStrategies { get; } = new();
    public ObservableCollection<RenamePair> PreviewItems { get; } = new();
    public ObservableCollection<Preset> Presets { get; } = new();

    private IReadOnlyList<RenamePair> _lastRenamedPairs = Array.Empty<RenamePair>();

    [ObservableProperty]
    private bool _canUndo;

    public LanguageManager L => LanguageManager.Instance;

    public RenameViewModel(
        IRenameService renameService,
        IPresetStore presetStore,
        IEnumerable<IRenameStrategy> strategies)
    {
        _renameService = renameService ?? throw new ArgumentNullException(nameof(renameService));
        _presetStore = presetStore ?? throw new ArgumentNullException(nameof(presetStore));
        _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));

        foreach (var strategy in _strategies)
        {
            AvailableStrategies.Add(strategy);
        }

        SelectedStrategy = AvailableStrategies.FirstOrDefault();
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
    private void BrowseFolder()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            FolderPath = dialog.SelectedPath;
        }
    }

    [RelayCommand]
    private void PreviewRenames()
    {
        PreviewItems.Clear();

        if (string.IsNullOrWhiteSpace(FolderPath))
        {
            MessageBox.Show(L.FolderPath, L.Error, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (SelectedStrategy == null)
        {
            return;
        }

        try
        {
            var names = NamesText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(n => n.Trim())
                .ToList();

            var template = new RenameTemplate
            {
                Pattern = TemplatePattern,
                Names = names
            };

            var pairs = _renameService.PreviewRenames(FolderPath, template, SelectedStrategy);
            
            foreach (var pair in pairs)
            {
                PreviewItems.Add(pair);
            }

            if (pairs.Count == 0)
            {
                MessageBox.Show(L.NoFilesToRename, L.AppTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, L.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ExecuteRenames()
    {
        PreviewRenames();

        if (PreviewItems.Count == 0)
        {
            return;
        }

        var result = MessageBox.Show(L.ConfirmRenamePrompt, L.AppTitle, MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            var pairs = PreviewItems.ToList();
            _renameService.ExecuteRenames(pairs);
            
            _lastRenamedPairs = pairs;
            CanUndo = true;

            MessageBox.Show(string.Format(L.RenamedSuccessMsg, pairs.Count), L.Success, MessageBoxButton.OK, MessageBoxImage.Information);
            PreviewItems.Clear();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, L.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void UndoRenames()
    {
        if (!CanUndo || _lastRenamedPairs.Count == 0) return;

        var result = MessageBox.Show(L.ConfirmUndoPrompt, L.AppTitle, MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            _renameService.UndoRenames(_lastRenamedPairs);
            MessageBox.Show(L.Done, L.Success, MessageBoxButton.OK, MessageBoxImage.Information);
            _lastRenamedPairs = Array.Empty<RenamePair>();
            CanUndo = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, L.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void SaveAsPreset()
    {
        if (string.IsNullOrWhiteSpace(PresetName))
        {
            MessageBox.Show(L.PresetNamePrompt, L.Error, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var names = NamesText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(n => n.Trim())
            .ToList();

        var template = new RenameTemplate
        {
            Pattern = TemplatePattern,
            Names = names
        };

        var preset = new Preset
        {
            Name = PresetName,
            RenameTemplate = template
        };

        _presetStore.Save(preset);
        PresetName = string.Empty;
        LoadPresets();
    }

    [RelayCommand]
    private void ApplyPreset(Preset? preset)
    {
        if (preset == null) return;
        TemplatePattern = preset.RenameTemplate.Pattern;
        NamesText = string.Join(Environment.NewLine, preset.RenameTemplate.Names);
    }
}
