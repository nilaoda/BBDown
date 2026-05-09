using System.Windows.Controls;
using BBDown.GUI.ViewModels;

namespace BBDown.GUI.Views.Pages;

/// <summary>
/// Interaction logic for DownloadsPage.xaml
/// </summary>
public partial class DownloadsPage : Page
{
    public DownloadsPage()
    {
        InitializeComponent();
        DataContext = new DownloadsViewModel();
    }
}
