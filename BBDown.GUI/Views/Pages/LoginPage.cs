using System.Windows.Controls;
using BBDown.GUI.ViewModels;

namespace BBDown.GUI.Views.Pages;

/// <summary>
/// Interaction logic for LoginPage.xaml
/// </summary>
public partial class LoginPage : Page
{
    public LoginPage()
    {
        InitializeComponent();
        DataContext = new LoginViewModel();
    }
}
