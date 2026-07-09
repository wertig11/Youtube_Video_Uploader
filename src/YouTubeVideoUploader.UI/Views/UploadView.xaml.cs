using System.Windows;
using System.Windows.Controls;
using YouTubeVideoUploader.Domain.ValueObjects;
using YouTubeVideoUploader.UI.ViewModels;

namespace YouTubeVideoUploader.UI.Views;

/// <summary>
/// Interaction logic for UploadView.xaml
/// </summary>
public partial class UploadView : System.Windows.Controls.UserControl
{
    public UploadView()
    {
        InitializeComponent();
    }

    private void LoadPreset_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is UploadViewModel vm && PresetsListUpload.SelectedItem is Preset preset)
        {
            var applyMethod = vm.GetType().GetMethod("ApplyPreset");
            applyMethod?.Invoke(vm, new object[] { preset });
        }
    }
}
