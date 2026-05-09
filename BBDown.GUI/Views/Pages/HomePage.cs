using System.Windows.Controls;
using BBDown.GUI.ViewModels;

namespace BBDown.GUI.Views.Pages;

/// <summary>
/// Interaction logic for HomePage.xaml
/// </summary>
public partial class HomePage : Page
{
    public HomePage()
    {
        InitializeComponent();
        DataContext = new HomeViewModel();
    }
}
