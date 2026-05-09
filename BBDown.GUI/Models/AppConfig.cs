namespace BBDown.GUI.Models;

public class AppConfig
{
    // 常规
    public string DownloadSource { get; set; } = "";
    public string EncodingPriority { get; set; } = "";
    public string FfmpegPath { get; set; } = "";
    public string WorkDir { get; set; } = "";

    // 下载选项
    public bool AudioOnly { get; set; } = false;
    public bool VideoOnly { get; set; } = false;
    public bool SubOnly { get; set; } = false;
    public bool DownloadDanmaku { get; set; } = false;

    // 交互选项
    public bool Interactive { get; set; } = false;
    public bool OnlyShowInfo { get; set; } = false;
    public bool HideStreams { get; set; } = false;
    public bool Debug { get; set; } = false;

    // 跳过选项
    public bool SkipSubtitle { get; set; } = false;
    public bool SkipCover { get; set; } = false;
    public bool SkipMux { get; set; } = false;
    public bool SkipAi { get; set; } = true;

    // MP4Box
    public bool UseMp4box { get; set; } = false;
    public string Mp4boxPath { get; set; } = "";

    // Cookies
    public string AccessToken { get; set; } = "";
    public string Cookie { get; set; } = "";

    // 文件名
    public string FilePattern { get; set; } = "";
    public string MultiFilePattern { get; set; } = "";

    // 分P
    public bool ShowAll { get; set; } = false;
    public string SelectPage { get; set; } = "";
    public string DelayPerPage { get; set; } = "0";

    // aria2c
    public bool UseAria2c { get; set; } = false;
    public string Aria2cPath { get; set; } = "";
    public string Aria2cProxy { get; set; } = "";
    public string Aria2cArgs { get; set; } = "";

    // 其他
    public bool MultiThread { get; set; } = true;
    public bool ForceHttp { get; set; } = true;
    public string Language { get; set; } = "";
    public string UserAgent { get; set; } = "";

    // 代理
    public bool EnableProxy { get; set; } = false;
    public string Host { get; set; } = "";
    public string EpHost { get; set; } = "";
    public string Area { get; set; } = "";

    // 弹幕格式
    public string DanmakuFormats { get; set; } = "";

    // 主题
    public string Theme { get; set; } = "System";
}
