using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Parsing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBDown
{
    internal class CommandLineInvoker
    {
        private readonly static Argument<string> Url = new("url", description: "视频地址 或 av|bv|BV|ep|ss");
        private readonly static Option<bool> UseTvApi = new(new string[] { "--use-tv-api", "-tv" }, "使用TV端解析模式");
        private readonly static Option<bool> UseAppApi = new(new string[] { "--use-app-api", "-app" }, "使用APP端解析模式");
        private readonly static Option<bool> UseIntlApi = new(new string[] { "--use-intl-api", "-intl" }, "使用国际版(东南亚视频)解析模式");
        private readonly static Option<bool> UseMP4box = new(new string[] { "--use-mp4box" }, "使用MP4Box来混流");
        private readonly static Option<string> EncodingPriority = new(new string[] { "--encoding-priority", "-e" }, "视频编码的选择优先级, 用逗号分割 例: \"hevc,av1,avc\"");
        private readonly static Option<string> DfnPriority = new(new string[] { "--dfn-priority", "-q" }, "画质优先级,用逗号分隔 例: \"8K 超高清, 1080P 高码率, HDR 真彩, 杜比视界\"");
        private readonly static Option<bool> OnlyShowInfo = new(new string[] { "--only-show-info", "-info" }, "仅解析而不进行下载");
        private readonly static Option<bool> HideStreams = new(new string[] { "--hide-streams", "-hs" }, "不要显示所有可用音视频流");
        private readonly static Option<bool> Interactive = new(new string[] { "--interactive", "-ia" }, "交互式选择清晰度");
        private readonly static Option<bool> ShowAll = new(new string[] { "--show-all" }, "展示所有分P标题");
        private readonly static Option<bool> UseAria2c = new(new string[] { "--use-aria2c" }, "调用aria2c进行下载(你需要自行准备好二进制可执行文件)");
        private readonly static Option<string> Aria2cArgs = new(new string[] { "--aria2c-args" }, "调用aria2c的附加参数(默认参数包含\"-x16 -s16 -j16 -k 5M\", 使用时注意字符串转义)");
        private readonly static Option<bool> MultiThread = new(new string[] { "--multi-thread", "-mt" }, "使用多线程下载(默认开启)");
        private readonly static Option<string> SelectPage = new(new string[] { "--select-page", "-p" }, "选择指定分p或分p范围: (-p 8 或 -p 1,2 或 -p 3-5 或 -p ALL 或 -p LAST)");
        private readonly static Option<bool> AudioOnly = new(new string[] { "--audio-only" }, "仅下载音频");
        private readonly static Option<bool> VideoOnly = new(new string[] { "--video-only" }, "仅下载视频");
        private readonly static Option<bool> DanmakuOnly = new(new string[] { "--danmaku-only" }, "仅下载弹幕");
        private readonly static Option<bool> CoverOnly = new(new string[] { "--cover-only" }, "仅下载封面");
        private readonly static Option<bool> SubOnly = new(new string[] { "--sub-only" }, "仅下载字幕");
        private readonly static Option<bool> Debug = new(new string[] { "--debug" }, "输出调试日志");
        private readonly static Option<bool> SkipMux = new(new string[] { "--skip-mux" }, "跳过混流步骤");
        private readonly static Option<bool> SkipSubtitle = new(new string[] { "--skip-subtitle" }, "跳过字幕下载");
        private readonly static Option<bool> SkipCover = new(new string[] { "--skip-cover" }, "跳过封面下载");
        private readonly static Option<bool> ForceHttp = new(new string[] { "--force-http" }, "下载音视频时强制使用HTTP协议替换HTTPS(默认开启)");
        private readonly static Option<bool> DownloadDanmaku = new(new string[] { "--download-danmaku", "-dd" }, "下载弹幕");
        private readonly static Option<bool> SkipAi = new(new string[] { "--skip-ai" }, description: "跳过AI字幕下载(默认开启)");
        private readonly static Option<bool> VideoAscending = new(new string[] { "--video-ascending" }, "视频升序(最小体积优先)");
        private readonly static Option<bool> AudioAscending = new(new string[] { "--audio-ascending" }, "音频升序(最小体积优先)");
        private readonly static Option<bool> AllowPcdn = new(new string[] { "--allow-pcdn" }, "不替换PCDN域名, 仅在正常情况与--upos-host均无法下载时使用");
        private readonly static Option<string> Language = new(new string[] { "--language" }, "设置混流的音频语言(代码), 如chi, jpn等");
        private readonly static Option<string> Cookie = new(new string[] { "--cookie", "-c" }, "设置字符串cookie用以下载网页接口的会员内容");
        private readonly static Option<string> AccessToken = new(new string[] { "--access-token", "-token" }, "设置access_token用以下载TV/APP接口的会员内容");
        private readonly static Option<string> WorkDir = new(new string[] { "--work-dir" }, "设置程序的工作目录");
        private readonly static Option<string> FFmpegPath = new(new string[] { "--ffmpeg-path" }, "设置ffmpeg的路径");
        private readonly static Option<string> Mp4boxPath = new(new string[] { "--mp4box-path" }, "设置mp4box的路径");
        private readonly static Option<string> Aria2cPath = new(new string[] { "--aria2c-path" }, "设置aria2c的路径");
        private readonly static Option<string> UposHost = new(new string[] { "--upos-host" }, "自定义upos服务器");
        private readonly static Option<bool> ForceReplaceHost = new(new string[] { "--force-replace-host" }, "强制替换下载服务器host(默认开启)");
        private readonly static Option<string> DelayPerPage = new(new string[] { "--delay-per-page" }, "设置下载合集分P之间的下载间隔时间(单位: 秒, 默认无间隔)");
        private readonly static Option<string> FilePattern = new(new string[] { "--file-pattern", "-F" }, $"使用内置变量自定义单P存储文件名:\r\n\r\n" + $"<videoTitle>: 视频主标题\r\n" + $"<pageNumber>: 视频分P序号\r\n" + $"<pageNumberWithZero>: 视频分P序号(前缀补零)\r\n" + $"<pageTitle>: 视频分P标题\r\n" + $"<bvid>: 视频BV号\r\n" + $"<aid>: 视频aid\r\n" + $"<cid>: 视频cid\r\n" + $"<dfn>: 视频清晰度\r\n" + $"<res>: 视频分辨率\r\n" + $"<fps>: 视频帧率\r\n" + $"<videoCodecs>: 视频编码\r\n" + $"<videoBandwidth>: 视频码率\r\n" + $"<audioCodecs>: 音频编码\r\n" + $"<audioBandwidth>: 音频码率\r\n" + $"<ownerName>: 上传者名称\r\n" + $"<ownerMid>: 上传者mid\r\n" + $"<publishDate>: 发布时间\r\n" + $"<apiType>: API类型(TV/APP/INTL/WEB)\r\n\r\n" + $"默认为: {Program.SinglePageDefaultSavePath}\r\n");
        private readonly static Option<string> MultiFilePattern = new(new string[] { "--multi-file-pattern", "-M" }, $"使用内置变量自定义多P存储文件名:\r\n\r\n" + $"默认为: {Program.MultiPageDefaultSavePath}\r\n");
        private readonly static Option<string> Host = new(new string[] { "--host" }, "指定BiliPlus host(使用BiliPlus需要access_token, 不需要cookie, 解析服务器能够获取你账号的大部分权限!)");
        private readonly static Option<string> EpHost = new(new string[] { "--ep-host" }, "指定BiliPlus EP host(用于代理api.bilibili.com/pgc/view/web/season, 大部分解析服务器不支持代理该接口)");
        private readonly static Option<string> Area = new(new string[] { "--area" }, "(hk|tw|th) 使用BiliPlus时必选, 指定BiliPlus area");
        private readonly static Option<string> ConfigFile = new(new string[] { "--config-file" }, "读取指定的BBDown本地配置文件(默认为: BBDown.config)");//以下仅为兼容旧版本命令行, 不建议使用
        private readonly static Option<string> Aria2cProxy = new(new string[] { "--aria2c-proxy" }, "调用aria2c进行下载时的代理地址配置") { IsHidden = true };
        private readonly static Option<bool> OnlyHevc = new(new string[] { "--only-hevc", "-hevc" }, "只下载hevc编码") { IsHidden = true };
        private readonly static Option<bool> OnlyAvc = new(new string[] { "--only-avc", "-avc" }, "只下载avc编码") { IsHidden = true };
        private readonly static Option<bool> OnlyAv1 = new(new string[] { "--only-av1", "-av1" }, "只下载av1编码") { IsHidden = true };
        private readonly static Option<bool> AddDfnSubfix = new(new string[] { "--add-dfn-subfix" }, "为文件加入清晰度后缀, 如XXX[1080P 高码率]") { IsHidden = true };
        private readonly static Option<bool> NoPaddingPageNum = new(new string[] { "--no-padding-page-num" }, "不给分P序号补零") { IsHidden = true };
        private readonly static Option<bool> BandwithAscending = new(new string[] { "--bandwith-ascending" }, "比特率升序(最小体积优先)") { IsHidden = true };


        class MyOptionBinder : BinderBase<MyOption>
        {
            protected override MyOption GetBoundValue(BindingContext bindingContext)
            {
                var option = new MyOption
                {
                    Url = bindingContext.ParseResult.GetValueForArgument(Url)
                };

                if (bindingContext.ParseResult.HasOption(UseTvApi)) option.UseTvApi = bindingContext.ParseResult.GetValueForOption(UseTvApi)!;
                if (bindingContext.ParseResult.HasOption(UseAppApi)) option.UseAppApi = bindingContext.ParseResult.GetValueForOption(UseAppApi)!;
                if (bindingContext.ParseResult.HasOption(UseIntlApi)) option.UseIntlApi = bindingContext.ParseResult.GetValueForOption(UseIntlApi)!;
                if (bindingContext.ParseResult.HasOption(UseMP4box)) option.UseMP4box = bindingContext.ParseResult.GetValueForOption(UseMP4box)!;
                if (bindingContext.ParseResult.HasOption(EncodingPriority)) option.EncodingPriority = bindingContext.ParseResult.GetValueForOption(EncodingPriority)!;
                if (bindingContext.ParseResult.HasOption(DfnPriority)) option.DfnPriority = bindingContext.ParseResult.GetValueForOption(DfnPriority)!;
                if (bindingContext.ParseResult.HasOption(OnlyShowInfo)) option.OnlyShowInfo = bindingContext.ParseResult.GetValueForOption(OnlyShowInfo)!;
                if (bindingContext.ParseResult.HasOption(ShowAll)) option.ShowAll = bindingContext.ParseResult.GetValueForOption(ShowAll)!;
                if (bindingContext.ParseResult.HasOption(UseAria2c)) option.UseAria2c = bindingContext.ParseResult.GetValueForOption(UseAria2c)!;
                if (bindingContext.ParseResult.HasOption(Interactive)) option.Interactive = bindingContext.ParseResult.GetValueForOption(Interactive)!;
                if (bindingContext.ParseResult.HasOption(HideStreams)) option.HideStreams = bindingContext.ParseResult.GetValueForOption(HideStreams)!;
                if (bindingContext.ParseResult.HasOption(MultiThread)) option.MultiThread = bindingContext.ParseResult.GetValueForOption(MultiThread)!;
                if (bindingContext.ParseResult.HasOption(VideoOnly)) option.VideoOnly = bindingContext.ParseResult.GetValueForOption(VideoOnly)!;
                if (bindingContext.ParseResult.HasOption(AudioOnly)) option.AudioOnly = bindingContext.ParseResult.GetValueForOption(AudioOnly)!;
                if (bindingContext.ParseResult.HasOption(DanmakuOnly)) option.DanmakuOnly = bindingContext.ParseResult.GetValueForOption(DanmakuOnly)!;
                if (bindingContext.ParseResult.HasOption(CoverOnly)) option.CoverOnly = bindingContext.ParseResult.GetValueForOption(CoverOnly)!;
                if (bindingContext.ParseResult.HasOption(SubOnly)) option.SubOnly = bindingContext.ParseResult.GetValueForOption(SubOnly)!;
                if (bindingContext.ParseResult.HasOption(Debug)) option.Debug = bindingContext.ParseResult.GetValueForOption(Debug)!;
                if (bindingContext.ParseResult.HasOption(SkipMux)) option.SkipMux = bindingContext.ParseResult.GetValueForOption(SkipMux)!;
                if (bindingContext.ParseResult.HasOption(SkipSubtitle)) option.SkipSubtitle = bindingContext.ParseResult.GetValueForOption(SkipSubtitle)!;
                if (bindingContext.ParseResult.HasOption(SkipCover)) option.SkipCover = bindingContext.ParseResult.GetValueForOption(SkipCover)!;
                if (bindingContext.ParseResult.HasOption(ForceHttp)) option.ForceHttp = bindingContext.ParseResult.GetValueForOption(ForceHttp)!;
                if (bindingContext.ParseResult.HasOption(DownloadDanmaku)) option.DownloadDanmaku = bindingContext.ParseResult.GetValueForOption(DownloadDanmaku)!;
                if (bindingContext.ParseResult.HasOption(SkipAi)) option.SkipAi = bindingContext.ParseResult.GetValueForOption(SkipAi)!;
                if (bindingContext.ParseResult.HasOption(VideoAscending)) option.VideoAscending = bindingContext.ParseResult.GetValueForOption(VideoAscending)!;
                if (bindingContext.ParseResult.HasOption(AudioAscending)) option.AudioAscending = bindingContext.ParseResult.GetValueForOption(AudioAscending)!;
                if (bindingContext.ParseResult.HasOption(AllowPcdn)) option.AllowPcdn = bindingContext.ParseResult.GetValueForOption(AllowPcdn)!;
                if (bindingContext.ParseResult.HasOption(FilePattern)) option.FilePattern = bindingContext.ParseResult.GetValueForOption(FilePattern)!;
                if (bindingContext.ParseResult.HasOption(MultiFilePattern)) option.MultiFilePattern = bindingContext.ParseResult.GetValueForOption(MultiFilePattern)!;
                if (bindingContext.ParseResult.HasOption(SelectPage)) option.SelectPage = bindingContext.ParseResult.GetValueForOption(SelectPage)!;
                if (bindingContext.ParseResult.HasOption(Language)) option.Language = bindingContext.ParseResult.GetValueForOption(Language)!;
                if (bindingContext.ParseResult.HasOption(Cookie)) option.Cookie = bindingContext.ParseResult.GetValueForOption(Cookie)!;
                if (bindingContext.ParseResult.HasOption(AccessToken)) option.AccessToken = bindingContext.ParseResult.GetValueForOption(AccessToken)!;
                if (bindingContext.ParseResult.HasOption(Aria2cArgs)) option.Aria2cArgs = bindingContext.ParseResult.GetValueForOption(Aria2cArgs)!;
                if (bindingContext.ParseResult.HasOption(WorkDir)) option.WorkDir = bindingContext.ParseResult.GetValueForOption(WorkDir)!;
                if (bindingContext.ParseResult.HasOption(FFmpegPath)) option.FFmpegPath = bindingContext.ParseResult.GetValueForOption(FFmpegPath)!;
                if (bindingContext.ParseResult.HasOption(Mp4boxPath)) option.Mp4boxPath = bindingContext.ParseResult.GetValueForOption(Mp4boxPath)!;
                if (bindingContext.ParseResult.HasOption(Aria2cPath)) option.Aria2cPath = bindingContext.ParseResult.GetValueForOption(Aria2cPath)!;
                if (bindingContext.ParseResult.HasOption(UposHost)) option.UposHost = bindingContext.ParseResult.GetValueForOption(UposHost)!;
                if (bindingContext.ParseResult.HasOption(ForceReplaceHost)) option.ForceReplaceHost = bindingContext.ParseResult.GetValueForOption(ForceReplaceHost)!;
                if (bindingContext.ParseResult.HasOption(DelayPerPage)) option.DelayPerPage = bindingContext.ParseResult.GetValueForOption(DelayPerPage)!;
                if (bindingContext.ParseResult.HasOption(Host)) option.Host = bindingContext.ParseResult.GetValueForOption(Host)!;
                if (bindingContext.ParseResult.HasOption(EpHost)) option.EpHost = bindingContext.ParseResult.GetValueForOption(EpHost)!;
                if (bindingContext.ParseResult.HasOption(Area)) option.Area = bindingContext.ParseResult.GetValueForOption(Area)!;
                if (bindingContext.ParseResult.HasOption(ConfigFile)) option.ConfigFile = bindingContext.ParseResult.GetValueForOption(ConfigFile)!;
                if (bindingContext.ParseResult.HasOption(Aria2cProxy)) option.Aria2cProxy = bindingContext.ParseResult.GetValueForOption(Aria2cProxy)!;
                if (bindingContext.ParseResult.HasOption(OnlyHevc)) option.OnlyHevc = bindingContext.ParseResult.GetValueForOption(OnlyHevc)!;
                if (bindingContext.ParseResult.HasOption(OnlyAvc)) option.OnlyAvc = bindingContext.ParseResult.GetValueForOption(OnlyAvc)!;
                if (bindingContext.ParseResult.HasOption(OnlyAv1)) option.OnlyAv1 = bindingContext.ParseResult.GetValueForOption(OnlyAv1)!;
                if (bindingContext.ParseResult.HasOption(AddDfnSubfix)) option.AddDfnSubfix = bindingContext.ParseResult.GetValueForOption(AddDfnSubfix)!;
                if (bindingContext.ParseResult.HasOption(NoPaddingPageNum)) option.NoPaddingPageNum = bindingContext.ParseResult.GetValueForOption(NoPaddingPageNum)!;
                if (bindingContext.ParseResult.HasOption(BandwithAscending)) option.BandwithAscending = bindingContext.ParseResult.GetValueForOption(BandwithAscending)!;
                return option;
            }
        }

        public static RootCommand GetRootCommand(Func<MyOption, Task> action)
        {
            var rootCommand = new RootCommand
            {
                Url,
                UseTvApi,
                UseAppApi,
                UseIntlApi,
                UseMP4box,
                EncodingPriority,
                DfnPriority,
                OnlyShowInfo,
                ShowAll,
                UseAria2c,
                Interactive,
                HideStreams,
                MultiThread,
                VideoOnly,
                AudioOnly,
                DanmakuOnly,
                SubOnly,
                CoverOnly,
                Debug,
                SkipMux,
                SkipSubtitle,
                SkipCover,
                ForceHttp,
                DownloadDanmaku,
                SkipAi,
                VideoAscending,
                AudioAscending,
                AllowPcdn,
                FilePattern,
                MultiFilePattern,
                SelectPage,
                Language,
                Cookie,
                AccessToken,
                Aria2cArgs,
                WorkDir,
                FFmpegPath,
                Mp4boxPath,
                Aria2cPath,
                UposHost,
                ForceReplaceHost,
                DelayPerPage,
                Host,
                EpHost,
                Area,
                ConfigFile,
                Aria2cProxy,
                OnlyHevc,
                OnlyAvc,
                OnlyAv1,
                AddDfnSubfix,
                NoPaddingPageNum,
                BandwithAscending
            };

            rootCommand.SetHandler(async (myOption) => await action(myOption), new MyOptionBinder());

            return rootCommand;
        }
    }
}
