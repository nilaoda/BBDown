using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BBDown.GUI.Models;
using BBDown.GUI.Services;

namespace BBDown.GUI.ViewModels;

public class HomeViewModel : INotifyPropertyChanged
{
    private string _url = "";
    private int _selectedQualityIndex;
    private int _selectedMoreQualityIndex;
    private bool _isMoreQualityVisible;
    private bool _isCurrentPart = true;
    private bool _isAllPart;
    private bool _isNewPart;

    public static List<string> QuickQualities { get; } =
    [
        "优先最高画质",
        "1080P 高清",
        "720P 高清",
        "480P 清晰",
        "360P 流畅",
        "更多..."
    ];

    public static List<string> MoreQualities { get; } =
    [
        "优先最高画质",
        "8K 超高清",
        "杜比视界",
        "HDR 真彩",
        "4K 超清",
        "1080P 高帧率",
        "1080P 高码率",
        "720P 高帧率"
    ];

    public string Url
    {
        get => _url;
        set
        {
            if (_url != value)
            {
                _url = value;
                OnPropertyChanged();
            }
        }
    }

    public int SelectedQualityIndex
    {
        get => _selectedQualityIndex;
        set
        {
            if (_selectedQualityIndex != value)
            {
                _selectedQualityIndex = value;
                OnPropertyChanged();
                IsMoreQualityVisible = value == 5;
            }
        }
    }

    public int SelectedMoreQualityIndex
    {
        get => _selectedMoreQualityIndex;
        set
        {
            if (_selectedMoreQualityIndex != value)
            {
                _selectedMoreQualityIndex = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsMoreQualityVisible
    {
        get => _isMoreQualityVisible;
        set
        {
            if (_isMoreQualityVisible != value)
            {
                _isMoreQualityVisible = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsCurrentPart
    {
        get => _isCurrentPart;
        set
        {
            if (_isCurrentPart != value)
            {
                _isCurrentPart = value;
                OnPropertyChanged();
                if (value)
                {
                    _isAllPart = false;
                    _isNewPart = false;
                    OnPropertyChanged(nameof(IsAllPart));
                    OnPropertyChanged(nameof(IsNewPart));
                }
            }
        }
    }

    public bool IsAllPart
    {
        get => _isAllPart;
        set
        {
            if (_isAllPart != value)
            {
                _isAllPart = value;
                OnPropertyChanged();
                if (value)
                {
                    _isCurrentPart = false;
                    _isNewPart = false;
                    OnPropertyChanged(nameof(IsCurrentPart));
                    OnPropertyChanged(nameof(IsNewPart));
                }
            }
        }
    }

    public bool IsNewPart
    {
        get => _isNewPart;
        set
        {
            if (_isNewPart != value)
            {
                _isNewPart = value;
                OnPropertyChanged();
                if (value)
                {
                    _isCurrentPart = false;
                    _isAllPart = false;
                    OnPropertyChanged(nameof(IsCurrentPart));
                    OnPropertyChanged(nameof(IsAllPart));
                }
            }
        }
    }

    public ICommand DownloadCommand { get; }
    public ICommand NavigateToSettingsCommand { get; }

    /// <summary>
    /// Set by MainViewModel to enable navigation to the settings page.
    /// </summary>
    public Action? NavigateToSettings { get; set; }

    /// <summary>
    /// Set by MainViewModel to enable navigation to the downloads page after enqueuing.
    /// </summary>
    public Action? NavigateToDownloads { get; set; }

    public HomeViewModel()
    {
        DownloadCommand = new RelayCommand(Download);
        NavigateToSettingsCommand = new RelayCommand(_ => NavigateToSettings?.Invoke());
    }

    private void Download(object? parameter)
    {
        if (string.IsNullOrWhiteSpace(Url))
            return;

        var options = new Dictionary<string, object>();
        var cfg = ConfigService.Instance.Config;

        // Merge all config properties first (baseline)
        MergeAdvancedOptions(options, cfg);

        // Download source mapping
        options["UseTvApi"] = cfg.DownloadSource == "tv";
        options["UseAppApi"] = cfg.DownloadSource == "app";
        options["UseIntlApi"] = cfg.DownloadSource == "intl";

        // Quality selection overrides
        if (SelectedQualityIndex > 0 && SelectedQualityIndex < 5)
        {
            options["DfnPriority"] = QuickQualities[SelectedQualityIndex];
        }
        if (IsMoreQualityVisible && SelectedMoreQualityIndex > 0)
        {
            options["DfnPriority"] = MoreQualities[SelectedMoreQualityIndex];
        }

        // Part selection overrides
        if (IsAllPart)
            options["SelectPage"] = "ALL";
        else if (IsNewPart)
            options["SelectPage"] = "NEW";

        DownloadService.Instance.Enqueue(Url, options);
        NavigateToDownloads?.Invoke();
    }

    private static void MergeAdvancedOptions(Dictionary<string, object> options, AppConfig cfg)
    {
        var properties = typeof(AppConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            // Skip Theme (UI-only) and DownloadSource (mapped to UseTvApi/UseAppApi/UseIntlApi above)
            if (prop.Name is nameof(AppConfig.Theme) or nameof(AppConfig.DownloadSource))
                continue;

            var value = prop.GetValue(cfg);
            if (value == null)
                continue;

            options[prop.Name] = value;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

/// <summary>
/// Minimal ICommand implementation for binding buttons and menu items to ViewModel actions.
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);
}
