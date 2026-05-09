using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BBDown.GUI.Models;
using BBDown.GUI.Services;

namespace BBDown.GUI.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    // ---- 常规设置 ----
    private int _downloadSourceIndex;
    private int _encodingPriorityIndex;
    private string _ffmpegPath = "";
    private string _workDir = "";

    // ---- 下载选项 ----
    private bool _audioOnly;
    private bool _videoOnly;
    private bool _subOnly;
    private bool _downloadDanmaku;

    // ---- 交互选项 ----
    private bool _interactive;
    private bool _onlyShowInfo;
    private bool _hideStreams;
    private bool _debug;

    // ---- 跳过选项 ----
    private bool _skipSubtitle;
    private bool _skipCover;
    private bool _skipMux;
    private bool _skipAi = true;

    // ---- MP4Box ----
    private bool _useMp4box;
    private string _mp4boxPath = "";

    // ---- 登录凭证 ----
    private string _accessToken = "";
    private string _cookie = "";

    // ---- 文件名选项 ----
    private string _filePattern = "";
    private string _multiFilePattern = "";

    // ---- 分P选项 ----
    private bool _showAll;
    private string _selectPage = "";
    private string _delayPerPage = "0";

    // ---- aria2c ----
    private bool _useAria2c;
    private string _aria2cPath = "";
    private string _aria2cProxy = "";
    private string _aria2cArgs = "";

    // ---- 其他 ----
    private bool _multiThread = true;
    private bool _forceHttp = true;
    private string _language = "";
    private string _userAgent = "";

    // ---- 代理 ----
    private bool _enableProxy;
    private string _host = "";
    private string _epHost = "";
    private string _area = "";

    // ---- 常规设置属性 ----
    public int DownloadSourceIndex
    {
        get => _downloadSourceIndex;
        set
        {
            if (_downloadSourceIndex != value)
            {
                _downloadSourceIndex = value;
                OnPropertyChanged();
            }
        }
    }

    public int EncodingPriorityIndex
    {
        get => _encodingPriorityIndex;
        set
        {
            if (_encodingPriorityIndex != value)
            {
                _encodingPriorityIndex = value;
                OnPropertyChanged();
            }
        }
    }

    public string FfmpegPath
    {
        get => _ffmpegPath;
        set
        {
            if (_ffmpegPath != value)
            {
                _ffmpegPath = value;
                OnPropertyChanged();
            }
        }
    }

    public string WorkDir
    {
        get => _workDir;
        set
        {
            if (_workDir != value)
            {
                _workDir = value;
                OnPropertyChanged();
            }
        }
    }

    // ---- 下载选项属性 ----
    public bool AudioOnly
    {
        get => _audioOnly;
        set
        {
            if (_audioOnly != value)
            {
                _audioOnly = value;
                OnPropertyChanged();
            }
        }
    }

    public bool VideoOnly
    {
        get => _videoOnly;
        set
        {
            if (_videoOnly != value)
            {
                _videoOnly = value;
                OnPropertyChanged();
            }
        }
    }

    public bool SubOnly
    {
        get => _subOnly;
        set
        {
            if (_subOnly != value)
            {
                _subOnly = value;
                OnPropertyChanged();
            }
        }
    }

    public bool DownloadDanmaku
    {
        get => _downloadDanmaku;
        set
        {
            if (_downloadDanmaku != value)
            {
                _downloadDanmaku = value;
                OnPropertyChanged();
            }
        }
    }

    // ---- 交互选项属性 ----
    public bool Interactive
    {
        get => _interactive;
        set
        {
            if (_interactive != value)
            {
                _interactive = value;
                OnPropertyChanged();
            }
        }
    }

    public bool OnlyShowInfo
    {
        get => _onlyShowInfo;
        set
        {
            if (_onlyShowInfo != value)
            {
                _onlyShowInfo = value;
                OnPropertyChanged();
            }
        }
    }

    public bool HideStreams
    {
        get => _hideStreams;
        set
        {
            if (_hideStreams != value)
            {
                _hideStreams = value;
                OnPropertyChanged();
            }
        }
    }

    public bool Debug
    {
        get => _debug;
        set
        {
            if (_debug != value)
            {
                _debug = value;
                OnPropertyChanged();
            }
        }
    }

    // ---- 跳过选项属性 ----
    public bool SkipSubtitle
    {
        get => _skipSubtitle;
        set
        {
            if (_skipSubtitle != value)
            {
                _skipSubtitle = value;
                OnPropertyChanged();
            }
        }
    }

    public bool SkipCover
    {
        get => _skipCover;
        set
        {
            if (_skipCover != value)
            {
                _skipCover = value;
                OnPropertyChanged();
            }
        }
    }

    public bool SkipMux
    {
        get => _skipMux;
        set
        {
            if (_skipMux != value)
            {
                _skipMux = value;
                OnPropertyChanged();
            }
        }
    }

    public bool SkipAi
    {
        get => _skipAi;
        set
        {
            if (_skipAi != value)
            {
                _skipAi = value;
                OnPropertyChanged();
            }
        }
    }

    // ---- MP4Box属性 ----
    public bool UseMp4box
    {
        get => _useMp4box;
        set
        {
            if (_useMp4box != value)
            {
                _useMp4box = value;
                OnPropertyChanged();
            }
        }
    }

    public string Mp4boxPath
    {
        get => _mp4boxPath;
        set
        {
            if (_mp4boxPath != value)
            {
                _mp4boxPath = value;
                OnPropertyChanged();
            }
        }
    }

    // ---- 登录凭证属性 ----
    public string AccessToken
    {
        get => _accessToken;
        set
        {
            if (_accessToken != value)
            {
                _accessToken = value;
                OnPropertyChanged();
            }
        }
    }

    public string Cookie
    {
        get => _cookie;
        set
        {
            if (_cookie != value)
            {
                _cookie = value;
                OnPropertyChanged();
            }
        }
    }

    // ---- 文件名选项属性 ----
    public string FilePattern
    {
        get => _filePattern;
        set
        {
            if (_filePattern != value)
            {
                _filePattern = value;
                OnPropertyChanged();
            }
        }
    }

    public string MultiFilePattern
    {
        get => _multiFilePattern;
        set
        {
            if (_multiFilePattern != value)
            {
                _multiFilePattern = value;
                OnPropertyChanged();
            }
        }
    }

    public string FilePatternReference => "<videoTitle>: 视频主标题\n" +
        "<pageNumber>: 视频分P序号\n" +
        "<pageCount>: 视频分P总数\n" +
        "<quality>: 视频画质\n" +
        "<videoCodec>: 视频编码\n" +
        "<audioCodec>: 音频编码\n" +
        "<id>: 视频AV/BV号\n" +
        "<cid>: 视频分P CID\n" +
        "<date>: 视频发布日期\n" +
        "<time>: 下载时间\n" +
        "<duration>: 视频时长\n" +
        "<res>: 视频分辨率\n" +
        "<fps>: 视频帧率\n" +
        "<desc>: 视频简介\n" +
        "<uploader>: UP主名称\n" +
        "<uploaderId>: UP主UID";

    // ---- 分P选项属性 ----
    public bool ShowAll
    {
        get => _showAll;
        set
        {
            if (_showAll != value)
            {
                _showAll = value;
                OnPropertyChanged();
            }
        }
    }

    public string SelectPage
    {
        get => _selectPage;
        set
        {
            if (_selectPage != value)
            {
                _selectPage = value;
                OnPropertyChanged();
            }
        }
    }

    public string DelayPerPage
    {
        get => _delayPerPage;
        set
        {
            if (_delayPerPage != value)
            {
                _delayPerPage = value;
                OnPropertyChanged();
            }
        }
    }

    // ---- aria2c属性 ----
    public bool UseAria2c
    {
        get => _useAria2c;
        set
        {
            if (_useAria2c != value)
            {
                _useAria2c = value;
                OnPropertyChanged();
            }
        }
    }

    public string Aria2cPath
    {
        get => _aria2cPath;
        set
        {
            if (_aria2cPath != value)
            {
                _aria2cPath = value;
                OnPropertyChanged();
            }
        }
    }

    public string Aria2cProxy
    {
        get => _aria2cProxy;
        set
        {
            if (_aria2cProxy != value)
            {
                _aria2cProxy = value;
                OnPropertyChanged();
            }
        }
    }

    public string Aria2cArgs
    {
        get => _aria2cArgs;
        set
        {
            if (_aria2cArgs != value)
            {
                _aria2cArgs = value;
                OnPropertyChanged();
            }
        }
    }

    // ---- 其他属性 ----
    public bool MultiThread
    {
        get => _multiThread;
        set
        {
            if (_multiThread != value)
            {
                _multiThread = value;
                OnPropertyChanged();
            }
        }
    }

    public bool ForceHttp
    {
        get => _forceHttp;
        set
        {
            if (_forceHttp != value)
            {
                _forceHttp = value;
                OnPropertyChanged();
            }
        }
    }

    public string Language
    {
        get => _language;
        set
        {
            if (_language != value)
            {
                _language = value;
                OnPropertyChanged();
            }
        }
    }

    public string UserAgent
    {
        get => _userAgent;
        set
        {
            if (_userAgent != value)
            {
                _userAgent = value;
                OnPropertyChanged();
            }
        }
    }

    // ---- 代理属性 ----
    public bool EnableProxy
    {
        get => _enableProxy;
        set
        {
            if (_enableProxy != value)
            {
                _enableProxy = value;
                OnPropertyChanged();
            }
        }
    }

    public string Host
    {
        get => _host;
        set
        {
            if (_host != value)
            {
                _host = value;
                OnPropertyChanged();
            }
        }
    }

    public string EpHost
    {
        get => _epHost;
        set
        {
            if (_epHost != value)
            {
                _epHost = value;
                OnPropertyChanged();
            }
        }
    }

    public string Area
    {
        get => _area;
        set
        {
            if (_area != value)
            {
                _area = value;
                OnPropertyChanged();
            }
        }
    }

    // ---- 选项列表 ----

    /// <summary>
    /// Download source display options: 网页端, TV端, APP端, 国际端
    /// </summary>
    public static List<string> DownloadSourceOptions { get; } =
    [
        "网页端",
        "TV端",
        "APP端",
        "国际端"
    ];

    /// <summary>
    /// Encoding priority display options: 优先可用编码, AVC, AV1, HEVC
    /// </summary>
    public static List<string> EncodingOptions { get; } =
    [
        "优先可用编码",
        "AVC",
        "AV1",
        "HEVC"
    ];

    // ---- Commands ----

    public ICommand SaveCommand { get; }
    public ICommand BrowseFfmpegCommand { get; }
    public ICommand BrowseWorkDirCommand { get; }

    public SettingsViewModel()
    {
        LoadFromConfig();

        SaveCommand = new RelayCommand(Save);
        BrowseFfmpegCommand = new RelayCommand(_ => BrowseFfmpeg());
        BrowseWorkDirCommand = new RelayCommand(_ => BrowseWorkDir());
    }

    /// <summary>
    /// Populate all properties from ConfigService.Instance.Config.
    /// </summary>
    public void LoadFromConfig()
    {
        var cfg = ConfigService.Instance.Config;

        // Map string -> index
        DownloadSourceIndex = cfg.DownloadSource switch
        {
            "tv" => 1,
            "app" => 2,
            "intl" => 3,
            _ => 0
        };

        EncodingPriorityIndex = cfg.EncodingPriority switch
        {
            "AVC" => 1,
            "AV1" => 2,
            "HEVC" => 3,
            _ => 0
        };

        // Directly copy all other properties
        FfmpegPath = cfg.FfmpegPath;
        WorkDir = cfg.WorkDir;

        AudioOnly = cfg.AudioOnly;
        VideoOnly = cfg.VideoOnly;
        SubOnly = cfg.SubOnly;
        DownloadDanmaku = cfg.DownloadDanmaku;

        Interactive = cfg.Interactive;
        OnlyShowInfo = cfg.OnlyShowInfo;
        HideStreams = cfg.HideStreams;
        Debug = cfg.Debug;

        SkipSubtitle = cfg.SkipSubtitle;
        SkipCover = cfg.SkipCover;
        SkipMux = cfg.SkipMux;
        SkipAi = cfg.SkipAi;

        UseMp4box = cfg.UseMp4box;
        Mp4boxPath = cfg.Mp4boxPath;

        AccessToken = cfg.AccessToken;
        Cookie = cfg.Cookie;

        FilePattern = cfg.FilePattern;
        MultiFilePattern = cfg.MultiFilePattern;

        ShowAll = cfg.ShowAll;
        SelectPage = cfg.SelectPage;
        DelayPerPage = cfg.DelayPerPage;

        UseAria2c = cfg.UseAria2c;
        Aria2cPath = cfg.Aria2cPath;
        Aria2cProxy = cfg.Aria2cProxy;
        Aria2cArgs = cfg.Aria2cArgs;

        MultiThread = cfg.MultiThread;
        ForceHttp = cfg.ForceHttp;
        Language = cfg.Language;
        UserAgent = cfg.UserAgent;

        EnableProxy = cfg.EnableProxy;
        Host = cfg.Host;
        EpHost = cfg.EpHost;
        Area = cfg.Area;
    }

    /// <summary>
    /// Write all ViewModel properties back to ConfigService and persist.
    /// </summary>
    private void Save(object? parameter)
    {
        var cfg = ConfigService.Instance.Config;

        // Map index -> string
        cfg.DownloadSource = DownloadSourceIndex switch
        {
            1 => "tv",
            2 => "app",
            3 => "intl",
            _ => ""
        };

        cfg.EncodingPriority = EncodingPriorityIndex switch
        {
            1 => "AVC",
            2 => "AV1",
            3 => "HEVC",
            _ => ""
        };

        // Copy all other properties back to config
        cfg.FfmpegPath = FfmpegPath;
        cfg.WorkDir = WorkDir;

        cfg.AudioOnly = AudioOnly;
        cfg.VideoOnly = VideoOnly;
        cfg.SubOnly = SubOnly;
        cfg.DownloadDanmaku = DownloadDanmaku;

        cfg.Interactive = Interactive;
        cfg.OnlyShowInfo = OnlyShowInfo;
        cfg.HideStreams = HideStreams;
        cfg.Debug = Debug;

        cfg.SkipSubtitle = SkipSubtitle;
        cfg.SkipCover = SkipCover;
        cfg.SkipMux = SkipMux;
        cfg.SkipAi = SkipAi;

        cfg.UseMp4box = UseMp4box;
        cfg.Mp4boxPath = Mp4boxPath;

        cfg.AccessToken = AccessToken;
        cfg.Cookie = Cookie;

        cfg.FilePattern = FilePattern;
        cfg.MultiFilePattern = MultiFilePattern;

        cfg.ShowAll = ShowAll;
        cfg.SelectPage = SelectPage;
        cfg.DelayPerPage = DelayPerPage;

        cfg.UseAria2c = UseAria2c;
        cfg.Aria2cPath = Aria2cPath;
        cfg.Aria2cProxy = Aria2cProxy;
        cfg.Aria2cArgs = Aria2cArgs;

        cfg.MultiThread = MultiThread;
        cfg.ForceHttp = ForceHttp;
        cfg.Language = Language;
        cfg.UserAgent = UserAgent;

        cfg.EnableProxy = EnableProxy;
        cfg.Host = Host;
        cfg.EpHost = EpHost;
        cfg.Area = Area;

        ConfigService.Instance.Save();
        System.Diagnostics.Debug.WriteLine("设置已保存");
    }

    private void BrowseFfmpeg()
    {
        System.Diagnostics.Debug.WriteLine("Browse FFmpeg");
    }

    private void BrowseWorkDir()
    {
        System.Diagnostics.Debug.WriteLine("Browse WorkDir");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
