using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BBDown.GUI.Models;
using BBDown.GUI.Services;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace BBDown.GUI.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private NavItem? _selectedNavItem;
    private object? _currentPage;
    private int _selectedThemeIndex;

    private static readonly string[] ThemeNames = ["System", "Light", "Dark"];

    public ObservableCollection<NavItem> NavigationItems { get; }

    public NavItem? SelectedNavItem
    {
        get => _selectedNavItem;
        set
        {
            if (_selectedNavItem != value)
            {
                _selectedNavItem = value;
                OnPropertyChanged();
                OnSelectedNavItemChanged();
            }
        }
    }

    public object? CurrentPage
    {
        get => _currentPage;
        set
        {
            if (_currentPage != value)
            {
                _currentPage = value;
                OnPropertyChanged();
            }
        }
    }

    public int SelectedThemeIndex
    {
        get => _selectedThemeIndex;
        set
        {
            if (_selectedThemeIndex != value)
            {
                _selectedThemeIndex = value;
                OnPropertyChanged();
                ApplyTheme();
                SaveConfig();
            }
        }
    }

    public MainViewModel()
    {
        NavigationItems =
        [
            new NavItem("首页", SymbolRegular.Home24),
            new NavItem("下载队列", SymbolRegular.ArrowDownload24),
            new NavItem("设置", SymbolRegular.Settings24),
            new NavItem("登录", SymbolRegular.Person24),
            new NavItem("关于", SymbolRegular.Info24),
        ];

        _selectedNavItem = NavigationItems[0];
        LoadConfig();
    }

    private void OnSelectedNavItemChanged()
    {
        // Page navigation is handled by the Frame in MainWindow
        // which observes SelectedNavItem changes via binding.
        OnPropertyChanged(nameof(CurrentPage));
    }

    public void LoadConfig()
    {
        var cfg = ConfigService.Instance.Config;
        SelectedThemeIndex = cfg.Theme switch
        {
            "Light" => 1,
            "Dark" => 2,
            _ => 0
        };
        ApplyTheme();
    }

    public void SaveConfig()
    {
        ConfigService.Instance.Config.Theme = ThemeNames[SelectedThemeIndex];
        ConfigService.Instance.Save();
    }

    private void ApplyTheme()
    {
        var theme = ThemeNames[SelectedThemeIndex] switch
        {
            "Light" => ApplicationTheme.Light,
            "Dark" => ApplicationTheme.Dark,
            _ => ApplicationTheme.Unknown
        };

        ApplicationThemeManager.Apply(theme, WindowBackdropType.Mica, true);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class NavItem(string name, SymbolRegular icon)
    {
        public string Name { get; } = name;
        public SymbolRegular Icon { get; } = icon;
    }
}
