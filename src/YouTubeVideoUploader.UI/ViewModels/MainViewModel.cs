using System;
using CommunityToolkit.Mvvm.ComponentModel;
using YouTubeVideoUploader.UI.Resources;

namespace YouTubeVideoUploader.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public RenameViewModel RenameVM { get; }
    public UploadViewModel UploadVM { get; }
    public SettingsViewModel SettingsVM { get; }

    public LanguageManager L => LanguageManager.Instance;

    public MainViewModel(
        RenameViewModel renameVM,
        UploadViewModel uploadVM,
        SettingsViewModel settingsVM)
    {
        RenameVM = renameVM ?? throw new ArgumentNullException(nameof(renameVM));
        UploadVM = uploadVM ?? throw new ArgumentNullException(nameof(uploadVM));
        SettingsVM = settingsVM ?? throw new ArgumentNullException(nameof(settingsVM));
    }
}
