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
    private readonly IAppStateService _appStateService;
    private List<string> _selectedFilePaths = new();
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

    [ObservableProperty]
    private string _currentSpeedText = string.Empty;

    [ObservableProperty]
    private string _currentVideoEtaText = string.Empty;

    [ObservableProperty]
    private string _overallEtaText = string.Empty;

    private DateTime _lastProgressTime;
    private long _lastBytesSent;
    private double _uploadSpeedBps;
    private string _lastVideoTitle = string.Empty;

    public ObservableCollection<UploadJob> UploadJobs { get; } = new();
    public ObservableCollection<Preset> Presets { get; } = new();

    public LanguageManager L => LanguageManager.Instance;

    public UploadViewModel(
        IUploadOrchestrator uploadOrchestrator,
        IAuthenticationService authService,
        IPresetStore presetStore,
        IAppStateService appStateService)
    {
        _uploadOrchestrator = uploadOrchestrator ?? throw new ArgumentNullException(nameof(uploadOrchestrator));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _presetStore = presetStore ?? throw new ArgumentNullException(nameof(presetStore));
        _appStateService = appStateService ?? throw new ArgumentNullException(nameof(appStateService));

        IsAuthenticated = _authService.IsAuthenticated;
        LoadPresets();

        // Load saved state
        var state = _appStateService.LoadState();
        FolderPath = state.UploadFolderPath;
        GameName = state.GameName;
        PlaylistId = state.PlaylistId;
        TagsText = state.TagsText;
        DescriptionTemplate = state.DescriptionTemplate;
        CategoryId = state.CategoryId;
        StartDate = state.StartDate;
        PublishTimeText = state.PublishTimeText;
        IntervalDays = state.IntervalDays;
        
        if (!string.IsNullOrEmpty(state.SelectedUploadFilePaths))
        {
            _selectedFilePaths = state.SelectedUploadFilePaths.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        if (state.ActiveUploadJobs != null && state.ActiveUploadJobs.Count > 0)
        {
            UploadJobs.Clear();
            foreach (var stateJob in state.ActiveUploadJobs)
            {
                var file = new VideoFile(stateJob.FilePath, Path.GetFileName(stateJob.FilePath), Path.GetExtension(stateJob.FilePath), DateTime.Now, 0);
                if (File.Exists(stateJob.FilePath))
                {
                    var fi = new FileInfo(stateJob.FilePath);
                    file = new VideoFile(fi.FullName, fi.Name, fi.Extension, fi.LastWriteTime, fi.Length);
                }
                
                var job = new UploadJob(file, stateJob.Title)
                {
                    Description = stateJob.Description,
                    Tags = stateJob.Tags,
                    CategoryId = stateJob.CategoryId,
                    PrivacyStatus = Enum.TryParse<Domain.Enums.PrivacyStatus>(stateJob.PrivacyStatus, out var privacy) ? privacy : Domain.Enums.PrivacyStatus.Private,
                    ScheduledPublishDate = stateJob.ScheduledPublishDate,
                    PlaylistId = stateJob.PlaylistId
                };
                
                if (stateJob.Status == "Completed")
                {
                    job.MarkCompleted(stateJob.YouTubeVideoId);
                }
                else if (stateJob.Status == "Failed")
                {
                    job.MarkFailed(stateJob.ErrorMessage);
                }
                else if (stateJob.Status == "Skipped")
                {
                    job.MarkSkipped();
                }
                
                UploadJobs.Add(job);
            }
        }

        // Auto-authenticate on startup in the background if credentials exist
        Task.Run(async () =>
        {
            try
            {
                string storedPath = state.ClientSecretPath;
                string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "client_secret.json");
                string pathToUse = File.Exists(storedPath) ? storedPath : (File.Exists(localPath) ? localPath : string.Empty);
                
                if (!string.IsNullOrEmpty(pathToUse))
                {
                    bool success = await _authService.AuthenticateAsync(pathToUse, CancellationToken.None);
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsAuthenticated = success;
                    });
                }
            }
            catch
            {
                // Ignore background auto-auth failures
            }
        });
    }

    partial void OnFolderPathChanged(string value) => SaveCurrentState();
    partial void OnGameNameChanged(string value) => SaveCurrentState();
    partial void OnPlaylistIdChanged(string value) => SaveCurrentState();
    partial void OnTagsTextChanged(string value) => SaveCurrentState();
    partial void OnDescriptionTemplateChanged(string value) => SaveCurrentState();
    partial void OnCategoryIdChanged(string value) => SaveCurrentState();
    partial void OnStartDateChanged(DateTime value) => SaveCurrentState();
    partial void OnPublishTimeTextChanged(string value) => SaveCurrentState();
    partial void OnIntervalDaysChanged(int value) => SaveCurrentState();

    private void SaveCurrentState()
    {
        try
        {
            var state = _appStateService.LoadState();
            state.UploadFolderPath = FolderPath;
            state.GameName = GameName;
            state.PlaylistId = PlaylistId;
            state.TagsText = TagsText;
            state.DescriptionTemplate = DescriptionTemplate;
            state.CategoryId = CategoryId;
            state.StartDate = StartDate;
            state.PublishTimeText = PublishTimeText;
            state.IntervalDays = IntervalDays;
            state.SelectedUploadFilePaths = string.Join(";", _selectedFilePaths);

            state.ActiveUploadJobs.Clear();
            foreach (var job in UploadJobs)
            {
                state.ActiveUploadJobs.Add(new AppStateJob
                {
                    FilePath = job.VideoFile.FilePath,
                    Title = job.Title,
                    Description = job.Description,
                    Tags = job.Tags?.ToList() ?? new List<string>(),
                    CategoryId = job.CategoryId,
                    PrivacyStatus = job.PrivacyStatus.ToString(),
                    ScheduledPublishDate = job.ScheduledPublishDate,
                    PlaylistId = job.PlaylistId,
                    Status = job.Status.ToString(),
                    YouTubeVideoId = job.YouTubeVideoId ?? string.Empty,
                    ErrorMessage = job.ErrorMessage ?? string.Empty
                });
            }
            _appStateService.SaveState(state);
        }
        catch
        {
            // Background save fail silent
        }
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
    private void SelectFiles()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Multiselect = true,
            Filter = "Video Files|*.mp4;*.mkv;*.mov|All Files|*.*"
        };
        
        if (dialog.ShowDialog() == true)
        {
            _selectedFilePaths = dialog.FileNames.ToList();
            if (_selectedFilePaths.Count > 0)
            {
                FolderPath = Path.GetDirectoryName(_selectedFilePaths[0]) ?? string.Empty;
                PrepareBatch();
            }
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

            IReadOnlyList<UploadJob> jobs;
            if (_selectedFilePaths != null && _selectedFilePaths.Count > 0)
            {
                jobs = _uploadOrchestrator.PrepareUploadBatchSelected(FolderPath, _selectedFilePaths, config);
            }
            else
            {
                jobs = _uploadOrchestrator.PrepareUploadBatch(FolderPath, config);
            }

            UploadJobs.Clear();
            foreach (var job in jobs)
            {
                UploadJobs.Add(job);
            }
            SaveCurrentState();
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

        // Initialize tracking variables
        _lastVideoTitle = string.Empty;
        _uploadSpeedBps = 0;
        CurrentSpeedText = string.Empty;
        CurrentVideoEtaText = string.Empty;
        OverallEtaText = string.Empty;

        var progress = new Progress<BatchProgress>(p =>
        {
            OverallPercent = (int)((double)p.CompletedCount / p.TotalCount * 100);
            ProgressText = $"{p.CompletedCount} / {p.TotalCount}";
            CurrentVideoTitle = p.CurrentVideoTitle;
            
            if (p.CurrentVideoTotalBytes > 0)
            {
                CurrentVideoPercent = (double)p.CurrentVideoBytesSent / p.CurrentVideoTotalBytes * 100;
            }

            var now = DateTime.UtcNow;
            if (_lastVideoTitle != p.CurrentVideoTitle)
            {
                _lastVideoTitle = p.CurrentVideoTitle;
                _lastBytesSent = p.CurrentVideoBytesSent;
                _lastProgressTime = now;
                _uploadSpeedBps = 0;
                CurrentSpeedText = string.Empty;
                CurrentVideoEtaText = string.Empty;
                OverallEtaText = string.Empty;
            }
            else
            {
                double elapsed = (now - _lastProgressTime).TotalSeconds;
                if (elapsed >= 0.5) // update every half-second for stability
                {
                    long bytesSentSinceLast = p.CurrentVideoBytesSent - _lastBytesSent;
                    double currentSpeed = bytesSentSinceLast / elapsed;
                    
                    // Smooth with moving average
                    _uploadSpeedBps = _uploadSpeedBps == 0 ? currentSpeed : (_uploadSpeedBps * 0.7) + (currentSpeed * 0.3);
                    
                    _lastBytesSent = p.CurrentVideoBytesSent;
                    _lastProgressTime = now;
                    
                    // Update speed text
                    if (_uploadSpeedBps < 1024)
                    {
                        CurrentSpeedText = $"{_uploadSpeedBps:F0} B/s";
                    }
                    else if (_uploadSpeedBps < 1024 * 1024)
                    {
                        CurrentSpeedText = $"{_uploadSpeedBps / 1024:F1} KB/s";
                    }
                    else
                    {
                        CurrentSpeedText = $"{_uploadSpeedBps / (1024 * 1024):F1} MB/s";
                    }
                        
                    // Update current video ETA
                    long currentVideoRemainingBytes = p.CurrentVideoTotalBytes - p.CurrentVideoBytesSent;
                    if (_uploadSpeedBps > 0 && currentVideoRemainingBytes > 0)
                    {
                        var etaSpan = TimeSpan.FromSeconds(currentVideoRemainingBytes / _uploadSpeedBps);
                        CurrentVideoEtaText = etaSpan.TotalHours >= 1 
                            ? etaSpan.ToString(@"hh\:mm\:ss") 
                            : etaSpan.ToString(@"mm\:ss");
                    }
                    else
                    {
                        CurrentVideoEtaText = "--:--";
                    }

                    // Update overall queue ETA
                    var jobsList = UploadJobs.ToList();
                    long totalQueueBytes = jobsList.Sum(j => j.VideoFile.FileSize);
                    long completedBytes = jobsList.Take(p.CompletedCount).Sum(j => j.VideoFile.FileSize);
                    long totalUploadedBytes = completedBytes + p.CurrentVideoBytesSent;
                    long queueRemainingBytes = totalQueueBytes - totalUploadedBytes;
                    
                    if (_uploadSpeedBps > 0 && queueRemainingBytes > 0)
                    {
                        var overallEtaSpan = TimeSpan.FromSeconds(queueRemainingBytes / _uploadSpeedBps);
                        OverallEtaText = overallEtaSpan.TotalHours >= 1 
                            ? overallEtaSpan.ToString(@"hh\:mm\:ss") 
                            : overallEtaSpan.ToString(@"mm\:ss");
                    }
                    else
                    {
                        OverallEtaText = "--:--";
                    }
                }
            }
            SaveCurrentState();
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
