using System.ComponentModel;
using System.Windows;
using BBDown.GUI.ViewModels;
using BBDown.GUI.Views.Pages;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace BBDown.GUI;

public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Initialize with home page
        _viewModel.CurrentPage = new HomePage();

        Loaded += (s, e) => SystemThemeWatcher.Watch(this, WindowBackdropType.Mica, true);
        Closing += (s, e) => _viewModel.SaveConfig();
    }

    private void NavigationView_OnSelectionChanged(NavigationView sender, RoutedEventArgs args)
    {
        // Retrieve the NavItem from the selected NavigationViewItem container's DataContext.
        if (sender.SelectedItem is NavigationViewItem selectedItem
            && selectedItem.DataContext is ViewModels.MainViewModel.NavItem navItem)
        {
            _viewModel.SelectedNavItem = navItem;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(MainViewModel.SelectedNavItem))
        {
            var navItem = _viewModel.SelectedNavItem;
            if (navItem == null) return;

            _viewModel.CurrentPage = navItem.Name switch
            {
                "首页" => new HomePage(),
                "下载队列" => new DownloadsPage(),
                "设置" => new SettingsPage(),
                "登录" => new LoginPage(),
                "关于" => new AboutPage(),
                _ => null
            };
        }
    }
}
