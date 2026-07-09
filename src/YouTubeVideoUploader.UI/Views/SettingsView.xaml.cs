using System.Windows;
using System.Windows.Controls;
using YouTubeVideoUploader.UI.ViewModels;

namespace YouTubeVideoUploader.UI.Views;

/// <summary>
/// Interaction logic for SettingsView.xaml
/// </summary>
public partial class SettingsView : System.Windows.Controls.UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && sender is System.Windows.Controls.ComboBox comboBox && comboBox.SelectedItem is string lang)
        {
            vm.ChangeLanguageCommand.Execute(lang);
        }
    }
}
