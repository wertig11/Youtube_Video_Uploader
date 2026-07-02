using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YouTubeVideoUploader.Domain.Entities;
using YouTubeVideoUploader.Domain.Interfaces;
using YouTubeVideoUploader.Domain.ValueObjects;
using YouTubeVideoUploader.UI.Resources;

namespace YouTubeVideoUploader.UI.ViewModels;

public partial class UploadViewModel : ObservableObject
{
    private readonly IUploadOrchestrator _uploadOrchestrator;
    private readonly IAuthenticationService _authService;
    private readonly IPresetStore _presetStore;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private string _folderPath = string.Empty;

    [ObservableProperty]
    private string _gameName = string.Empty;

    [ObservableProperty]
    private string _playlistId = string.Empty;

    [ObservableProperty]
    private string _tagsText = string.Empty;

    [ObservableProperty]
    private string _descriptionTemplate = string.Empty;

    [ObservableProperty]
    private string _categoryId = "20"; // Gaming

    [ObservableProperty]
    private DateTime _startDate = DateTime.Today;

    [ObservableProperty]
    private string _publishTimeText = string.Empty;

    [ObservableProperty]
    private int _intervalDays = 1;

    [ObservableProperty]
    private bool _isUploading;

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    private int _overallPercent;

    [ObservableProperty]
    private string _progressText = string.Empty;

    [ObservableProperty]
    private double _currentVideoPercent;

    [ObservableProperty]
    private string _currentVideoTitle = string.Empty;

    [ObservableProperty]
    private string _presetName = string.Empty;

    public ObservableCollection<UploadJob> UploadJobs { get; } = new();
    public ObservableCollection<Preset> Presets { get; } = new();

    public LanguageManager L => LanguageManager.Instance;

    public UploadViewModel(
        IUploadOrchestrator uploadOrchestrator,
        IAuthenticationService authService,
        IPresetStore presetStore)
    {
        _uploadOrchestrator = uploadOrchestrator ?? throw new ArgumentNullException(nameof(uploadOrchestrator));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _presetStore = presetStore ?? throw new ArgumentNullException(nameof(presetStore));

        IsAuthenticated = _authService.IsAuthenticated;
        LoadPresets();
    }

    private void LoadPresets()
    {
        Presets.Clear();
        foreach (var preset in _presetStore.GetAll())
        {
            if (preset.UploadConfig != null)
            {
                Presets.Add(preset);
            }
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
    private async Task Authenticate()
    {
        // Try locating client_secret.json in app directory
        string secretPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "client_secret.json");
        if (!File.Exists(secretPath))
        {
            // Open file dialog
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                Title = "Select client_secret.json"
            };
            if (dialog.ShowDialog() == true)
            {
                secretPath = dialog.FileName;
            }
            else
            {
                return;
            }
        }

        try
        {
            bool success = await _authService.AuthenticateAsync(secretPath, CancellationToken.None);
            IsAuthenticated = success;
            if (success)
            {
                MessageBox.Show(L.Success, L.AppTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(L.Error, L.AppTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, L.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void PrepareBatch()
    {
        if (string.IsNullOrWhiteSpace(FolderPath))
        {
            MessageBox.Show(L.FolderPath, L.Error, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var staticTags = TagsText.Split(',')
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList();

            if (!TimeSpan.TryParse(PublishTimeText, out var timeOfDay))
            {
                timeOfDay = new TimeSpan(6, 45, 0); // Default fallback
            }

            var schedule = new PublishSchedule
            {
                StartDate = DateOnly.FromDateTime(StartDate),
                PublishTime = TimeOnly.FromTimeSpan(timeOfDay),
                IntervalDays = IntervalDays
            };

            var config = new UploadConfiguration
            {
                GameName = GameName,
                PlaylistId = PlaylistId,
                StaticTags = staticTags,
                DescriptionTemplate = DescriptionTemplate,
                CategoryId = CategoryId,
                Schedule = schedule
            };

            var jobs = _uploadOrchestrator.PrepareUploadBatch(FolderPath, config);

            UploadJobs.Clear();
            foreach (var job in jobs)
            {
                UploadJobs.Add(job);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, L.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task StartUpload()
    {
        if (UploadJobs.Count == 0)
        {
            MessageBox.Show("No videos prepared for upload. Please select a folder and click 'Prepare Batch' first.", L.Error, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!IsAuthenticated)
        {
            MessageBox.Show(L.AuthStatusDisconnected, L.Error, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsUploading = true;
        _cts = new CancellationTokenSource();

        var progress = new Progress<BatchProgress>(p =>
        {
            OverallPercent = (int)((double)p.CompletedCount / p.TotalCount * 100);
            ProgressText = $"{p.CompletedCount} / {p.TotalCount}";
            CurrentVideoTitle = p.CurrentVideoTitle;
            
            if (p.CurrentVideoTotalBytes > 0)
            {
                CurrentVideoPercent = (double)p.CurrentVideoBytesSent / p.CurrentVideoTotalBytes * 100;
            }
        });

        try
        {
            var jobs = UploadJobs.ToList();
            await _uploadOrchestrator.ExecuteUploadBatchAsync(jobs, progress, _cts.Token);
            MessageBox.Show(L.Done, L.Success, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            MessageBox.Show(L.Cancel, L.AppTitle, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, L.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsUploading = false;
            _cts = null;
        }
    }

    [RelayCommand]
    private void CancelUpload()
    {
        _cts?.Cancel();
    }

    [RelayCommand]
    private void SaveAsPreset()
    {
        if (string.IsNullOrWhiteSpace(PresetName))
        {
            MessageBox.Show(L.PresetNamePrompt, L.Error, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var staticTags = TagsText.Split(',')
            .Select(t => t.Trim())
            .ToList();

        if (!TimeSpan.TryParse(PublishTimeText, out var timeOfDay))
        {
            timeOfDay = new TimeSpan(6, 45, 0);
        }

        var schedule = new PublishSchedule
        {
            StartDate = DateOnly.FromDateTime(StartDate),
            PublishTime = TimeOnly.FromTimeSpan(timeOfDay),
            IntervalDays = IntervalDays
        };

        var config = new UploadConfiguration
        {
            GameName = GameName,
            PlaylistId = PlaylistId,
            StaticTags = staticTags,
            DescriptionTemplate = DescriptionTemplate,
            CategoryId = CategoryId,
            Schedule = schedule
        };

        var preset = new Preset
        {
            Name = PresetName,
            UploadConfig = config
        };

        _presetStore.Save(preset);
        PresetName = string.Empty;
        LoadPresets();
    }

    [RelayCommand]
    private void ApplyPreset(Preset? preset)
    {
        if (preset == null || preset.UploadConfig == null) return;
        
        var config = preset.UploadConfig;
        GameName = config.GameName;
        PlaylistId = config.PlaylistId;
        TagsText = string.Join(", ", config.StaticTags);
        DescriptionTemplate = config.DescriptionTemplate;
        CategoryId = config.CategoryId;
        
        var sched = config.Schedule;
        StartDate = sched.StartDate.ToDateTime(new TimeOnly(0, 0));
        PublishTimeText = sched.PublishTime.ToString(@"hh\:mm");
        IntervalDays = sched.IntervalDays;
    }
}
