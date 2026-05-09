using System.ComponentModel;
using BBDown.GUI.ViewModels;

namespace BBDown.GUI;

public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Defer initial navigation to when ViewModel fully loads
        NavigateToPage(_viewModel.SelectedNavItem);

        Closing += (s, e) => _viewModel.SaveConfig();
    }

    private void NavigationView_OnSelectionChanged(
        Wpf.Ui.Controls.NavigationView sender, System.Windows.RoutedEventArgs args)
    {
        if (sender.SelectedItem is MainViewModel.NavItem navItem)
        {
            _viewModel.SelectedNavItem = navItem;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(MainViewModel.SelectedNavItem))
        {
            NavigateToPage(_viewModel.SelectedNavItem);
        }
    }

    private void NavigateToPage(MainViewModel.NavItem? navItem)
    {
        if (navItem?.PageType == null) return;

        if (Activator.CreateInstance(navItem.PageType) is not System.Windows.Controls.Page page)
            return;

        _viewModel.CurrentPage = page;
    }
}
