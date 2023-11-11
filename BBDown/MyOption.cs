using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBDown
{
    internal class MyOption
    {
        public string Url { get; set; } = default!;
        public bool UseTvApi { get; set; }
        public bool UseAppApi { get; set; }
        public bool UseIntlApi { get; set; }
        public bool UseMP4box { get; set; }
        public string? EncodingPriority { get; set; }
        public string? DfnPriority { get; set; }
        public bool OnlyShowInfo { get; set; }
        public bool ShowAll { get; set; }
        public bool UseAria2c { get; set; }
        public bool Interactive { get; set; }
        public bool HideStreams { get; set; }
        public bool MultiThread { get; set; } = true;
        public bool SimplyMux {  get; set; } = false;
        public bool VideoOnly { get; set; }
        public bool AudioOnly { get; set; }
        public bool DanmakuOnly { get; set; }
        public bool CoverOnly { get; set; }
        public bool SubOnly { get; set; }
        public bool Debug { get; set; }
        public bool SkipMux { get; set; }
        public bool SkipSubtitle { get; set; }
        public bool SkipCover { get; set; }
        public bool ForceHttp { get; set; } = true;
        public bool DownloadDanmaku { get; set; } = false;
        public bool SkipAi { get; set; } = true;
        public bool VideoAscending { get; set; } = false;
        public bool AudioAscending { get; set; } = false;
        public bool AllowPcdn { get; set; } = false;
        public bool ForceReplaceHost { get; set; } = true;
        public bool SaveArchivesToFile { get; set; } = false;
        public string FilePattern { get; set; } = "";
        public string MultiFilePattern { get; set; } = "";
        public string SelectPage { get; set; } = "";
        public string Language { get; set; } = "";
        public string UserAgent { get; set; } = "";
        public string Cookie { get; set; } = "";
        public string AccessToken { get; set; } = "";
        public string Aria2cArgs { get; set; } = "";
        public string WorkDir { get; set; } = "";
        public string FFmpegPath { get; set; } = "";
        public string Mp4boxPath { get; set; } = "";
        public string Aria2cPath { get; set; } = "";
        public string UposHost { get; set; } = "";
        public string DelayPerPage { get; set; } = "0";
        public string Host { get; set; } = "api.bilibili.com";
        public string EpHost { get; set; } = "api.bilibili.com";
        public string Area { get; set; } = "";
        public string? ConfigFile { get; set; }
        //以下仅为兼容旧版本命令行，不建议使用
        public string Aria2cProxy { get; set; } = "";
        public bool OnlyHevc { get; set; }
        public bool OnlyAvc { get; set; }
        public bool OnlyAv1 { get; set; }
        public bool AddDfnSubfix { get; set; }
        public bool NoPaddingPageNum { get; set; }
        public bool BandwithAscending { get; set; }
    }
}
