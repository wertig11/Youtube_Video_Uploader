using System.Windows;
using YouTubeVideoUploader.UI.ViewModels;

namespace YouTubeVideoUploader.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the MainWindow.
    /// </summary>
    /// <param name="viewModel">The root MainViewModel to bind to.</param>
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}