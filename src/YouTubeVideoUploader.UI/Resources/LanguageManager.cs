using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace YouTubeVideoUploader.UI.Resources;

/// <summary>
/// Singleton manager that handles localized strings for English and Ukrainian,
/// triggering property changes so the UI updates instantly when language is switched.
/// </summary>
public class LanguageManager : INotifyPropertyChanged
{
    private static readonly Lazy<LanguageManager> _instance = new(() => new LanguageManager());
    public static LanguageManager Instance => _instance.Value;

    private string _currentLanguage = "en"; // "en" or "uk"

    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                OnPropertyChanged(string.Empty); // Notify all properties changed
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // Helper to select translation
    private string Get(string en, string uk) => _currentLanguage == "uk" ? uk : en;

    // Localized Strings
    public string AppTitle => Get("YouTube Video Uploader", "Завантажувач Відео на YouTube");
    public string TabRename => Get("Rename Files", "Перейменувати Файли");
    public string TabUpload => Get("Upload Videos", "Завантажити Відео");
    public string TabSettings => Get("Settings", "Налаштування");

    public string FolderPath => Get("Folder Path:", "Шлях до папки:");
    public string Browse => Get("Browse...", "Огляд...");
    public string RenameStrategy => Get("Rename Strategy:", "Стратегія перейменування:");
    public string Template => Get("Rename Template:", "Шаблон імені:");
    public string NameListPlaceholder => Get("Level Name List (one per line, e.g. level names)", "Список назв рівнів (одна на рядок)");
    public string NameListLabel => Get("Level Names:", "Назви рівнів:");
    public string TotalNames => Get("Total Names:", "Всього назв:");

    public string Preview => Get("Preview", "Попередній перегляд");
    public string Rename => Get("Rename", "Перейменувати");
    public string Undo => Get("Undo", "Скасувати");

    public string PreviewTitle => Get("Preview of Renaming:", "Попередній перегляд перейменування:");
    public string HeaderOriginalName => Get("Original Name", "Оригінальне ім'я");
    public string HeaderNewName => Get("New Name", "Нове ім'я");

    public string GameName => Get("Game Name:", "Назва гри:");
    public string PlaylistId => Get("Playlist ID:", "ID плейлиста:");
    public string StaticTags => Get("Static Tags (comma separated):", "Статичні теги (через кому):");
    public string DescriptionTemplate => Get("Description Template:", "Шаблон опису:");
    
    public string StartDate => Get("Start Date:", "Дата старту:");
    public string PublishTime => Get("Publish Time:", "Час публікації:");
    public string PublishInterval => Get("Publish Interval (Days):", "Інтервал публікації (днів):");
    
    public string QuotaWarning => Get("Note: Daily API limit is 10,000 units. Video upload costs 100 units.", "Примітка: Щоденний ліміт API 10000. Завантаження відео коштує 100.");
    
    public string StartUpload => Get("Start Upload", "Почати завантаження");
    public string Cancel => Get("Cancel", "Скасувати");
    public string Authenticate => Get("Authenticate", "Авторизуватися");
    public string AuthStatusConnected => Get("YouTube Status: Connected", "Статус YouTube: Підключено");
    public string AuthStatusDisconnected => Get("YouTube Status: Disconnected", "Статус YouTube: Відключено");

    public string PresetsTitle => Get("Configuration Presets", "Пресет конфігурації");
    public string SavePreset => Get("Save Preset", "Зберегти пресет");
    public string PresetNamePrompt => Get("Preset Name:", "Назва пресету:");
    public string LoadPreset => Get("Load", "Завантажити");
    public string Delete => Get("Delete", "Видалити");

    public string HeaderFileName => Get("File Name", "Ім'я файлу");
    public string HeaderTitle => Get("Title", "Назва");
    public string HeaderPublishDate => Get("Publish Date", "Дата публікації");
    public string HeaderStatus => Get("Status", "Статус");

    public string Success => Get("Success", "Успішно");
    public string Error => Get("Error", "Помилка");
    public string ConfirmRenamePrompt => Get("Are you sure you want to rename these files?", "Ви впевнені, що хочете перейменувати ці файли?");
    public string ConfirmUndoPrompt => Get("Are you sure you want to undo the renaming?", "Ви впевнені, що хочете скасувати перейменування?");
    public string NoFilesToRename => Get("No video files found to rename.", "Відеофайлів для перейменування не знайдено.");
    public string Done => Get("Done", "Готово");
    public string RenamedSuccessMsg => Get("Successfully renamed {0} files.", "Успішно перейменовано {0} файлів.");
    
    public string ClientSecretPath => Get("Client Secret File Path:", "Шлях до файлу client_secret.json:");
    public string Language => Get("Language:", "Мова:");
    public string ExportPreset => Get("Export Presets...", "Експортувати пресети...");
    public string ImportPreset => Get("Import Presets...", "Імпортувати пресети...");
}
