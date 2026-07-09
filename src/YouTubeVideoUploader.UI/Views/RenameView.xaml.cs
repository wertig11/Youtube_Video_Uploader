using System.Windows;
using System.Windows.Controls;
using YouTubeVideoUploader.Domain.ValueObjects;
using YouTubeVideoUploader.UI.ViewModels;

namespace YouTubeVideoUploader.UI.Views;

/// <summary>
/// Interaction logic for RenameView.xaml
/// </summary>
public partial class RenameView : System.Windows.Controls.UserControl
{
    public RenameView()
    {
        InitializeComponent();
    }

    private void LoadPreset_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is RenameViewModel vm && PresetsList.SelectedItem is Preset preset)
        {
            // Call ApplyPreset manually from code-behind since WPF ComboBox selection change doesn't bind as command cleanly
            // without interactive behaviors
            var applyMethod = vm.GetType().GetMethod("ApplyPreset");
            applyMethod?.Invoke(vm, new object[] { preset });
        }
    }
}
