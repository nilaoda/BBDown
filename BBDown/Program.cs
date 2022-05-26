using QRCoder;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using static BBDown.Core.Entity.Entity;
using static BBDown.BBDownUtil;
using static BBDown.Core.Parser;
using static BBDown.Core.Logger;
using static BBDown.BBDownMuxer;
using System.Text;
using System.Linq;
using System.Text.Json;
using System.Net.Http;
using System.Text.RegularExpressions;
using BBDown.Core;
using BBDown.Core.Util;
using BBDown.Core.Fetcher;

namespace BBDown
{
    class Program
    {
        private static string BACKUP_HOST = "upos-sz-mirrorcoso1.bilivideo.com";
        public static string SinglePageDefaultSavePath { get; set; } = "<videoTitle>";
        public static string MultiPageDefaultSavePath { get; set; } = "<videoTitle>/[P<pageNumberWithZero>]<pageTitle>";

        public static string APP_DIR = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);

        private static int Compare(Video r1, Video r2, Dictionary<string, byte> encodingPriority, Dictionary<string, int> dfnPriority)
        {
            if (r1.dfn != r2.dfn)
            {
                if (!dfnPriority.TryGetValue(r1.dfn, out int r1Priority)) { r1Priority = int.MaxValue; }
                if (!dfnPriority.TryGetValue(r2.dfn, out int r2Priority)) { r2Priority = int.MaxValue; }
                if (r1Priority != r2Priority) { return r1Priority < r2Priority ? -1 : 1; }
            }
            if (r1.codecs != r2.codecs)
            {
                if (!encodingPriority.TryGetValue(r1.codecs, out byte r1Priority)) { r1Priority = byte.MaxValue; }
                if (!encodingPriority.TryGetValue(r2.codecs, out byte r2Priority)) { r2Priority = byte.MaxValue; }
                if (r1Priority != r2Priority) { return r1Priority < r2Priority ? -1 : 1; }

            }
            return (Convert.ToInt32(r1.id) * 100000 + r1.bandwith) > (Convert.ToInt32(r2.id) * 100000 + r2.bandwith) ? -1 : 1;
        }

        private static int Compare(Audio r1, Audio r2)
        {
            return r1.bandwith - r2.bandwith > 0 ? -1 : 1;
        }

        class MyOption
        {
            public string Url { get; set; }
            public bool UseTvApi { get; set; }
            public bool UseAppApi { get; set; }
            public bool UseIntlApi { get; set; }
            public bool UseMP4box { get; set; }
            public string EncodingPriority { get; set; }
            public string DfnPriority { get; set; }
            public bool OnlyShowInfo { get; set; }
            public bool ShowAll { get; set; }
            public bool UseAria2c { get; set; }
            public bool Interactive { get; set; }
            public bool HideStreams { get; set; }
            public bool MultiThread { get; set; } = true;
            public bool VideoOnly { get; set; }
            public bool AudioOnly { get; set; }
            public bool SubOnly { get; set; }
            public bool Debug { get; set; }
            public bool SkipMux { get; set; }
            public bool SkipSubtitle { get; set; }
            public bool SkipCover { get; set; }
            public bool ForceHttp { get; set; } = true;
            public bool DownloadDanmaku { get; set; } = false;
            public string FilePattern { get; set; } = "";
            public string MultiFilePattern { get; set; } = "";
            public string SelectPage { get; set; } = "";
            public string Language { get; set; } = "";
            public string Cookie { get; set; } = "";
            public string AccessToken { get; set; } = "";
            public string Aria2cProxy { get; set; } = "";
            public string WorkDir { get; set; } = "";
            public string FFmpegPath { get; set; } = "";
            public string Mp4boxPath { get; set; } = "";
            public string Aria2cPath { get; set; } = "";
            public string DelayPerPage { get; set; } = "0";
            public string ConfigFile { get; set; }
            //以下仅为兼容旧版本命令行，不建议使用
            public bool OnlyHevc { get; set; }
            public bool OnlyAvc { get; set; }
            public bool OnlyAv1 { get; set; }
            public bool AddDfnSubfix { get; set; }
            public bool NoPaddingPageNum { get; set; }
        }

        public static async Task<int> Main(params string[] args)
        {
            ServicePointManager.DefaultConnectionLimit = 2048;

            var rootCommand = new RootCommand
            {
                new Argument<string>(
                    "url",
                    description: "视频地址 或 av|bv|BV|ep|ss"),
                new Option<bool>(
                    new string[]{ "--use-tv-api" ,"-tv"},
                    "使用TV端解析模式"),
                new Option<bool>(
                    new string[]{ "--use-app-api" ,"-app"},
                    "使用APP端解析模式"),
                new Option<bool>(
                    new string[]{ "--use-intl-api" ,"-intl"},
                    "使用国际版解析模式"),
                new Option<bool>(
                    new string[]{ "--use-mp4box"},
                    "使用MP4Box来混流"),
                new Option<string>(
                    new string[]{ "--encoding-priority" },
                    "视频编码的选择优先级,用逗号分割 例:\"hevc,av1,avc\""
                    ),
                new Option<string>(
                    new string[] { "--dfn-priority" },
                    "画质优先级,用逗号分隔 例:\"8K 超高清, 1080P 高码率, HDR 真彩, 杜比视界\""
                    ),
                new Option<bool>(
                    new string[]{ "--only-show-info" ,"-info"},
                    "仅解析而不进行下载"),
                new Option<bool>(
                    new string[]{ "--hide-streams", "-hs"},
                    "不要显示所有可用音视频流"),
                new Option<bool>(
                    new string[]{ "--interactive", "-ia"},
                    "交互式选择清晰度"),
                new Option<bool>(
                    new string[]{ "--show-all"},
                    "展示所有分P标题"),
                new Option<bool>(
                    new string[]{ "--use-aria2c"},
                    "调用aria2c进行下载(你需要自行准备好二进制可执行文件)"),
                new Option<string>(
                    new string[]{ "--aria2c-proxy"},
                    "调用aria2c进行下载时的代理地址配置"),
                new Option<bool>(
                    new string[]{ "--multi-thread", "-mt"},
                    "使用多线程下载(默认开启)"),
                new Option<string>(
                    new string[]{ "--select-page" ,"-p"},
                    "选择指定分p或分p范围：(-p 8 或 -p 1,2 或 -p 3-5 或 -p ALL)"),
                new Option<bool>(
                    new string[]{ "--audio-only"},
                    "仅下载音频"),
                new Option<bool>(
                    new string[]{ "--video-only"},
                    "仅下载视频"),
                new Option<bool>(
                    new string[]{ "--sub-only"},
                    "仅下载字幕"),
                new Option<bool>(
                    new string[]{ "--debug"},
                    "输出调试日志"),
                new Option<bool>(
                    new string[]{ "--skip-mux"},
                    "跳过混流步骤"),
                new Option<bool>(
                    new string[]{ "--skip-subtitle"},
                    "跳过字幕下载"),
                new Option<bool>(
                    new string[]{ "--skip-cover"},
                    "跳过封面下载"),
                new Option<bool>(
                    new string[]{ "--force-http"},
                    "下载音视频时强制使用HTTP协议替换HTTPS(默认开启)"),
                new Option<bool>(
                    new string[]{ "--download-danmaku", "-dd"},
                    "下载弹幕"),
                new Option<string>(
                    new string[]{ "--language"},
                    "设置混流的音频语言(代码)，如chi, jpn等"),
                new Option<string>(
                    new string[]{ "--cookie" ,"-c"},
                    "设置字符串cookie用以下载网页接口的会员内容"),
                new Option<string>(
                    new string[]{ "--access-token" ,"-token"},
                    "设置access_token用以下载TV/APP接口的会员内容"),
                new Option<string>(
                    new string[]{ "--work-dir"},
                    "设置程序的工作目录"),
                new Option<string>(
                    new string[]{ "--ffmpeg-path"},
                    "设置ffmpeg的路径"),
                new Option<string>(
                    new string[]{ "--mp4box-path"},
                    "设置mp4box的路径"),
                new Option<string>(
                    new string[]{ "--aria2c-path"},
                    "设置aria2c的路径"),
                new Option<string>(
                    new string[]{ "--delay-per-page"},
                    "设置下载合集分P之间的下载间隔时间(单位: 秒, 默认无间隔)"),
                new Option<string>(
                    new string[]{ "--file-pattern", "-F"},
                    $"使用内置变量自定义单P存储文件名:\r\n\r\n" +
                    $"<videoTitle>: 视频主标题\r\n" +
                    $"<pageNumber>: 视频分P序号\r\n" +
                    $"<pageNumberWithZero>: 视频分P序号(前缀补零)\r\n" +
                    $"<pageTitle>: 视频分P标题\r\n" +
                    $"<aid>: 视频aid\r\n" +
                    $"<cid>: 视频cid\r\n" +
                    $"<dfn>: 视频清晰度\r\n" +
                    $"<res>: 视频分辨率\r\n" +
                    $"<fps>: 视频帧率\r\n" +
                    $"<videoCodecs>: 视频编码\r\n" +
                    $"<videoBandwidth>: 视频码率\r\n" +
                    $"<audioCodecs>: 音频编码\r\n" +
                    $"<audioBandwidth>: 音频码率\r\n" +
                    $"<ownerName>: 上传者名称\r\n" +
                    $"<ownerMid>: 上传者mid\r\n\r\n" +
                    $"默认为: {SinglePageDefaultSavePath}\r\n"),
                new Option<string>(
                    new string[]{ "--multi-file-pattern", "-M"},
                    $"使用内置变量自定义多P存储文件名:\r\n\r\n" +
                    $"默认为: {MultiPageDefaultSavePath}\r\n"),
                new Option<string>(
                    new string[]{ "--config-file"},
                    "读取指定的BBDown本地配置文件(默认为: BBDown.config)"),
                //以下仅为兼容旧版本命令行，不建议使用
                new Option<bool>(
                    new string[]{ "--only-hevc" ,"-hevc"},
                    "只下载hevc编码")
                {
                    IsHidden = true
                },
                new Option<bool>(
                    new string[]{ "--only-avc" ,"-avc"},
                    "只下载avc编码")
                {
                    IsHidden = true
                },
                new Option<bool>(
                    new string[]{ "--only-av1" ,"-av1"},
                    "只下载av1编码")
                {
                    IsHidden = true
                },
                new Option<bool>(
                    new string[]{ "--add-dfn-subfix"},
                    "为文件加入清晰度后缀，如XXX[1080P 高码率]")
                {
                    IsHidden = true
                },
                new Option<bool>(
                    new string[]{ "--no-padding-page-num"},
                    "不给分P序号补零")
                {
                    IsHidden = true
                },
            };

            Command loginCommand = new Command(
                "login",
                "通过APP扫描二维码以登录您的WEB账号");
            rootCommand.AddCommand(loginCommand);
            Command loginTVCommand = new Command(
                "logintv",
                "通过APP扫描二维码以登录您的TV账号");
            rootCommand.AddCommand(loginTVCommand);
            rootCommand.Description = "BBDown是一个免费且便捷高效的哔哩哔哩下载/解析软件.";
            rootCommand.TreatUnmatchedTokensAsErrors = true;

            //WEB登录
            loginCommand.Handler = CommandHandler.Create(loginWEB);

            //TV登录
            loginTVCommand.Handler = CommandHandler.Create(loginTV);

            rootCommand.Handler = CommandHandler.Create<MyOption>(async (myOption) =>
            {
                await DoWorkAsync(myOption);
            });

            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;
            var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            Console.Write($"BBDown version {ver.Major}.{ver.Minor}.{ver.Build}, Bilibili Downloader.\r\n");
            Console.ResetColor();
            Console.Write("欢迎到讨论区交流：\r\n" +
                "https://github.com/nilaoda/BBDown/discussions\r\n");
            Console.WriteLine();

            var newArgsList = new List<string>();
            var commandLineResult = rootCommand.Parse(args);
            if (commandLineResult.CommandResult.Command.Name.ToLower() != "bbdown")
            {
                newArgsList.Add(commandLineResult.CommandResult.Command.Name);
                return await rootCommand.InvokeAsync(newArgsList.ToArray());
            }

            foreach (var item in commandLineResult.CommandResult.Children)
            {
                if (item is ArgumentResult)
                {
                    var a = (ArgumentResult)item;
                    newArgsList.Add(a.Tokens.First().Value);
                }
                else if (item is OptionResult)
                {
                    var o = (OptionResult)item;
                    newArgsList.Add("--" + o.Option.Name);
                    newArgsList.AddRange(o.Tokens.Select(t => t.Value));
                }
            }
            if (newArgsList.Contains("--debug")) Config.DEBUG_LOG = true;

            //处理配置文件
            try
            {
                var configPath = "";
                if (newArgsList.Contains("--config-file"))
                {
                    configPath = newArgsList.ElementAt(newArgsList.IndexOf("--config-file") + 1);
                }
                else
                {
                    configPath = Path.Combine(APP_DIR, "BBDown.config");
                }

                if (File.Exists(configPath))
                {
                    Log($"加载配置文件: {configPath}");
                    var configArgs = File
                        .ReadAllLines(configPath)
                        .Where(s => !string.IsNullOrEmpty(s) && !s.StartsWith("#"))
                        .SelectMany(s => {
                                var trimLine = s.Trim();
                                if (trimLine.IndexOf('-') == 0 && trimLine.IndexOf(' ') != -1) {  
                                    var spaceIndex = trimLine.IndexOf(' ');
                                    var paramsGroup = new String[] { trimLine.Substring(0,spaceIndex), trimLine.Substring(spaceIndex) };
                                    return paramsGroup.Where(s => !string.IsNullOrEmpty(s)).Select(s => s.Trim(' ').Trim('\"'));
                                } else {
                                    return new String[] {trimLine.Trim('\"')};
                                }                    
                            }
                        );
                    var configArgsResult = rootCommand.Parse(configArgs.ToArray());
                    foreach (var item in configArgsResult.CommandResult.Children)
                    {
                        if (item is OptionResult)
                        {
                            var o = (OptionResult)item;
                            if (!newArgsList.Contains("--" + o.Option.Name))
                            {
                                newArgsList.Add("--" + o.Option.Name);
                                newArgsList.AddRange(o.Tokens.Select(t => t.Value));
                            }
                        }
                    }

                    //命令行的优先级>配置文件优先级
                    LogDebug("新的命令行参数: " + string.Join(" ", newArgsList));
                }
            }
            catch (Exception)
            {
                LogError("配置文件读取异常，忽略");
            }

            return await rootCommand.InvokeAsync(newArgsList.ToArray());
        }

        private static async Task DoWorkAsync(MyOption myOption)
        {
            //检测更新
            new Thread(async () =>
            {
                await CheckUpdateAsync();
            }).Start();
            try
            {
                //兼容旧版本命令行参数并给出警告
                if (myOption.AddDfnSubfix)
                {
                    LogWarn("--add-dfn-subfix 已被弃用，建议使用 --file-pattern/-F 或 --multi-file-pattern/-M 来自定义输出文件名格式");
                    if (string.IsNullOrEmpty(myOption.FilePattern) && string.IsNullOrEmpty(myOption.MultiFilePattern))
                    {
                        SinglePageDefaultSavePath += "[<dfn>]";
                        MultiPageDefaultSavePath += "[<dfn>]";
                        LogWarn($"已切换至 -F \"{SinglePageDefaultSavePath}\" -M \"{MultiPageDefaultSavePath}\"");
                    }
                }
                if (myOption.OnlyHevc)
                {
                    LogWarn("--only-hevc/-hevc 已被弃用，请使用 --encoding-priority 来设置编码优先级，本次执行已将hevc设置为最高优先级");
                    myOption.EncodingPriority = "hevc";
                }
                if (myOption.OnlyAvc)
                {
                    LogWarn("--only-avc/-avc 已被弃用，请使用 --encoding-priority 来设置编码优先级，本次执行已将avc设置为最高优先级");
                    myOption.EncodingPriority = "avc";
                }
                if (myOption.OnlyAv1)
                {
                    LogWarn("--only-av1/-av1 已被弃用，请使用 --encoding-priority 来设置编码优先级，本次执行已将av1设置为最高优先级");
                    myOption.EncodingPriority = "av1";
                }
                if (myOption.NoPaddingPageNum)
                {
                    LogWarn("--no-padding-page-num 已被弃用，建议使用 --file-pattern/-F 或 --multi-file-pattern/-M 来自定义输出文件名格式");
                    if (string.IsNullOrEmpty(myOption.FilePattern) && string.IsNullOrEmpty(myOption.MultiFilePattern))
                    {
                        MultiPageDefaultSavePath = MultiPageDefaultSavePath.Replace("<pageNumberWithZero>", "<pageNumber>");
                        LogWarn($"已切换至 -M \"{MultiPageDefaultSavePath}\"");
                    }
                }

                bool interactMode = myOption.Interactive;
                bool infoMode = myOption.OnlyShowInfo;
                bool tvApi = myOption.UseTvApi;
                bool appApi = myOption.UseAppApi;
                bool intlApi = myOption.UseIntlApi;
                bool useMp4box = myOption.UseMP4box;
                
                var encodingPriority = new Dictionary<string, byte>();
                if (myOption.EncodingPriority != null)
                {
                    var encodingPriorityTemp = myOption.EncodingPriority.Replace("，", ",").Split(',').Select(s => s.ToUpper().Trim()).Where(s => !string.IsNullOrEmpty(s));
                    byte index = 0;
                    foreach (string encoding in encodingPriorityTemp)
                    {
                        if (encodingPriority.ContainsKey(encoding)) { continue; }
                        encodingPriority[encoding] = index;
                        index++;
                    }
                }
                var dfnPriority = new Dictionary<string, int>();
                if (myOption.DfnPriority != null)
				{
                    var dfnPriorityTemp = myOption.DfnPriority.Replace("，", ",").Split(',').Select(s => s.ToUpper().Trim()).Where(s => !string.IsNullOrEmpty(s));
                    int index = 0;
                    foreach (string dfn in dfnPriorityTemp)
					{
                        if (dfnPriority.ContainsKey(dfn)) { continue; }
                        dfnPriority[dfn] = index;
                        index++;
					}
				}

                bool hideStreams = myOption.HideStreams;
                bool multiThread = myOption.MultiThread;
                bool audioOnly = myOption.AudioOnly;
                bool videoOnly = myOption.VideoOnly;
                bool subOnly = myOption.SubOnly;
                bool skipMux = myOption.SkipMux;
                bool skipSubtitle = myOption.SkipSubtitle;
                bool skipCover = myOption.SkipCover;
                bool forceHttp = myOption.ForceHttp;
                bool downloadDanmaku = myOption.DownloadDanmaku;
                bool showAll = myOption.ShowAll;
                bool useAria2c = myOption.UseAria2c;
                string aria2cProxy = myOption.Aria2cProxy;
                Config.DEBUG_LOG = myOption.Debug;
                string input = myOption.Url;
                string savePathFormat = myOption.FilePattern;
                string lang = myOption.Language;
                string selectPage = myOption.SelectPage.ToUpper();
                string aidOri = ""; //原始aid
                int delay = Convert.ToInt32(myOption.DelayPerPage);
                Config.COOKIE = myOption.Cookie;
                Config.TOKEN = myOption.AccessToken.Replace("access_token=", "");

                if (!string.IsNullOrEmpty(myOption.WorkDir))
                {
                    var dir = Path.GetFullPath(myOption.WorkDir);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    //设置工作目录
                    Environment.CurrentDirectory = dir;
                    LogDebug("切换工作目录至：{0}", dir);
                }

                if (!string.IsNullOrEmpty(myOption.FFmpegPath) && File.Exists(myOption.FFmpegPath))
                {
                    BBDownMuxer.FFMPEG = myOption.FFmpegPath;
                }

                if (!string.IsNullOrEmpty(myOption.Mp4boxPath) && File.Exists(myOption.Mp4boxPath))
                {
                    BBDownMuxer.MP4BOX = myOption.Mp4boxPath;
                }

                if (!string.IsNullOrEmpty(myOption.Aria2cPath) && File.Exists(myOption.Aria2cPath))
                {
                    BBDownAria2c.ARIA2C = myOption.Aria2cPath;
                }
                //寻找ffmpeg或mp4box
                if (!skipMux)
                {
                    if (useMp4box)
                    {
                        if (string.IsNullOrEmpty(BBDownMuxer.MP4BOX))
                        {
                            var binPath = FindExecutable("mp4box");
                            if (string.IsNullOrEmpty(binPath))
                                throw new Exception("找不到可执行的mp4box文件");
                            BBDownMuxer.MP4BOX = binPath;
                        }
                    }
                    else if (string.IsNullOrEmpty(BBDownMuxer.FFMPEG))
                    {
                        var binPath = FindExecutable("ffmpeg");
                        if (string.IsNullOrEmpty(binPath))
                            throw new Exception("找不到可执行的ffmpeg文件");
                        BBDownMuxer.FFMPEG = binPath;
                    }
                }

                //寻找aria2c
                if (useAria2c)
                {
                    if (string.IsNullOrEmpty(BBDownAria2c.ARIA2C))
                    {
                        var binPath = FindExecutable("aria2c");
                        if (string.IsNullOrEmpty(binPath))
                            throw new Exception("找不到可执行的aria2c文件");
                        BBDownAria2c.ARIA2C = binPath;
                    }
                    
                }


                //audioOnly和videoOnly同时开启则全部忽视
                if (audioOnly && videoOnly)
                {
                    audioOnly = false;
                    videoOnly = false;
                }

                if (skipSubtitle)
                    subOnly = false;

                List<string> selectedPages = null;
                if (!string.IsNullOrEmpty(GetQueryString("p", input)))
                {
                    selectedPages = new List<string>();
                    selectedPages.Add(GetQueryString("p", input));
                }

                LogDebug("AppDirectory: {0}", APP_DIR);
                LogDebug("运行参数：{0}", JsonSerializer.Serialize(myOption));
                if (string.IsNullOrEmpty(Config.COOKIE) && File.Exists(Path.Combine(APP_DIR, "BBDown.data")))
                {
                    Log("加载本地cookie...");
                    LogDebug("文件路径：{0}", Path.Combine(APP_DIR, "BBDown.data"));
                    Config.COOKIE = File.ReadAllText(Path.Combine(APP_DIR, "BBDown.data"));
                }
                if (string.IsNullOrEmpty(Config.TOKEN) && File.Exists(Path.Combine(APP_DIR, "BBDownTV.data")) && tvApi)
                {
                    Log("加载本地token...");
                    LogDebug("文件路径：{0}", Path.Combine(APP_DIR, "BBDownTV.data"));
                    Config.TOKEN = File.ReadAllText(Path.Combine(APP_DIR, "BBDownTV.data"));
                    Config.TOKEN = Config.TOKEN.Replace("access_token=", "");
                }
                if (string.IsNullOrEmpty(Config.TOKEN) && File.Exists(Path.Combine(APP_DIR, "BBDownApp.data")) && appApi)
                {
                    Log("加载本地token...");
                    LogDebug("文件路径：{0}", Path.Combine(APP_DIR, "BBDownApp.data"));
                    Config.TOKEN = File.ReadAllText(Path.Combine(APP_DIR, "BBDownApp.data"));
                    Config.TOKEN = Config.TOKEN.Replace("access_token=", "");
                }

                // 检测是否登录了账号
                if (!intlApi && !tvApi)
                {
                    Log("检测账号登录...");
                    if (!await CheckLogin(Config.COOKIE))
                    {
                        LogWarn("你尚未登录B站账号, 解析可能受到限制");
                    }
                }

                Log("获取aid...");
                aidOri = await GetAvIdAsync(input);
                Log("获取aid结束: " + aidOri);
                //-p的优先级大于URL中的自带p参数，所以先清空selectedPages
                if (!string.IsNullOrEmpty(selectPage) && selectPage != "ALL")
                {
                    selectedPages = new List<string>();
                    try
                    {
                        string tmp = selectPage;
                        tmp = tmp.Trim().Trim(',');
                        if (tmp.Contains("-"))
                        {
                            int start = int.Parse(tmp.Split('-')[0]);
                            int end = int.Parse(tmp.Split('-')[1]);
                            for (int i = start; i <= end; i++)
                            {
                                selectedPages.Add(i.ToString());
                            }
                        }
                        else
                        {
                            foreach (var s in tmp.Split(','))
                            {
                                selectedPages.Add(s);
                            }
                        }
                    }
                    catch { LogError("解析分P参数时失败了~"); selectedPages = null; };
                }

                if (selectPage == "ALL") selectedPages = null;

                if (string.IsNullOrEmpty(aidOri)) throw new Exception("输入有误");
                Log("获取视频信息...");
                IFetcher fetcher = new NormalInfoFetcher();
                if (aidOri.StartsWith("cheese"))
                {
                    fetcher = new CheeseInfoFetcher();
                }
                else if (aidOri.StartsWith("ep"))
                {
                    if (intlApi)
                        fetcher = new IntlBangumiInfoFetcher();
                    else
                        fetcher = new BangumiInfoFetcher();
                }
                else if (aidOri.StartsWith("mid"))
                {
                    fetcher = new SpaceVideoFetcher();
                }
                else if (aidOri.StartsWith("listBizId"))
                {
                    fetcher = new MediaListFetcher();
                }
                else if (aidOri.StartsWith("seriesBizId"))
                {
                    fetcher = new SeriesListFetcher();
                }
                else if (aidOri.StartsWith("favId"))
                {
                    fetcher = new FavListFetcher();
                }
                var vInfo = await fetcher.FetchAsync(aidOri);
                string title = vInfo.Title;
                string pic = vInfo.Pic;
                string pubTime = vInfo.PubTime;
                LogColor("视频标题: " + title);
                Log("发布时间: " + pubTime);
                List<Page> pagesInfo = vInfo.PagesInfo;
                List<Subtitle> subtitleInfo = new List<Subtitle>();
                bool more = false;
                bool bangumi = vInfo.IsBangumi;
                bool cheese = vInfo.IsCheese;

                //打印分P信息
                foreach (Page p in pagesInfo)
                {
                    if (!showAll && more && p.index != pagesInfo.Count) continue;
                    if (!showAll && !more && p.index > 5)
                    {
                        Log("......");
                        more = true;
                    }
                    else
                    {
                        Log($"P{p.index}: [{p.cid}] [{p.title}] [{FormatTime(p.dur)}]");
                    }
                }

                //选择最新分P
                if (!string.IsNullOrEmpty(selectPage) && (selectPage == "LAST" || selectPage == "NEW")) 
                {
                    try
                    {
                        selectedPages = new List<string> { pagesInfo.Count.ToString() };
                        Log("程序已选择最新一P");
                    }
                    catch { LogError("解析分P参数时失败了~"); selectedPages = null; };
                }

                //如果用户没有选择分P，根据epid来确定某一集
                if (selectedPages == null && selectPage != "ALL" && !string.IsNullOrEmpty(vInfo.Index))
                {
                    selectedPages = new List<string> { vInfo.Index };
                    Log("程序已自动选择你输入的集数，如果要下载其他集数请自行指定分P(如可使用-p ALL代表全部)");
                }

                Log($"共计 {pagesInfo.Count} 个分P, 已选择：" + (selectedPages == null ? "ALL" : string.Join(",", selectedPages)));
                var pagesCount = pagesInfo.Count;

                //过滤不需要的分P
                if (selectedPages != null)
                    pagesInfo = pagesInfo.Where(p => selectedPages.Contains(p.index.ToString())).ToList();

                // 根据p数选择存储路径
                savePathFormat = string.IsNullOrEmpty(myOption.FilePattern) ? SinglePageDefaultSavePath : myOption.FilePattern;
                // 1. 多P; 2. 只有1P，但是是番剧，尚未完结时 按照多P处理
                if (pagesCount > 1 || (bangumi && !vInfo.IsBangumiEnd))
                {
                    savePathFormat = string.IsNullOrEmpty(myOption.MultiFilePattern) ? MultiPageDefaultSavePath : myOption.MultiFilePattern;
                }

                foreach (Page p in pagesInfo)
                {
                    int retryCount = 0;
                downloadPage:
                    try
                    {
                        string desc = string.IsNullOrEmpty(p.desc) ? vInfo.Desc : p.desc;
                        if (pagesInfo.Count > 1 && delay > 0)
                        {
                            Log($"停顿{delay}秒...");
                            Thread.Sleep(delay * 1000);
                        }

                        Log($"开始解析P{p.index}...");

                        LogDebug("尝试获取章节信息...");
                        p.points = await FetchPointsAsync(p.cid, p.aid);

                        string webJsonStr = "";
                        List<Video> videoTracks = new List<Video>();
                        List<Audio> audioTracks = new List<Audio>();
                        List<string> clips = new List<string>();
                        List<string> dfns = new List<string>();

                        string videoPath = $"{p.aid}/{p.aid}.P{p.index}.{p.cid}.mp4";
                        string audioPath = $"{p.aid}/{p.aid}.P{p.index}.{p.cid}.m4a";
                        var coverPath = $"{p.aid}/{p.aid}.jpg";

                        //处理文件夹以.结尾导致的异常情况
                        if (title.EndsWith(".")) title += "_fix";

                        //处理封面&&字幕
                        if (!infoMode)
                        {
                            if (!Directory.Exists(p.aid))
                            {
                                Directory.CreateDirectory(p.aid);
                            }
                            if (!skipCover && !subOnly && !File.Exists(coverPath))
                            {
                                Log("下载封面...");
                                var cover = pic == "" ? p.cover : pic;
                                LogDebug("下载：{0}", cover);
                                await using var response = await HTTPUtil.AppHttpClient.GetStreamAsync(cover);
                                await using var fs = new FileStream(coverPath, FileMode.Create);
                                await response.CopyToAsync(fs);
                            }

                            if (!skipSubtitle)
                            {
                                LogDebug("获取字幕...");
                                subtitleInfo = await SubUtil.GetSubtitlesAsync(p.aid, p.cid, p.epid, intlApi);
                                foreach (Subtitle s in subtitleInfo)
                                {
                                    Log($"下载字幕 {s.lan} => {SubUtil.GetSubtitleCode(s.lan).Item2}...");
                                    LogDebug("下载：{0}", s.url);
                                    await SubUtil.SaveSubtitleAsync(s.url, s.path);
                                    if (subOnly && File.Exists(s.path) && File.ReadAllText(s.path) != "")
                                    {
                                        var _outSubPath = FormatSavePath(savePathFormat, title, null, null, p, pagesCount);
                                        if (_outSubPath.Contains("/"))
                                        {
                                            if (!Directory.Exists(_outSubPath.Split('/').First()))
                                                Directory.CreateDirectory(_outSubPath.Split('/').First());
                                        }
                                        _outSubPath = _outSubPath.Substring(0, _outSubPath.LastIndexOf('.')) + ".srt";
                                        File.Move(s.path, _outSubPath, true);
                                    }
                                }
                            }

                            if (subOnly)
                            {
                                if (Directory.Exists(p.aid) && Directory.GetFiles(p.aid).Length == 0) Directory.Delete(p.aid, true);
                                continue;
                            }
                        }

                        //调用解析
                        (webJsonStr, videoTracks, audioTracks, clips, dfns) = await ExtractTracksAsync(aidOri, p.aid, p.cid, p.epid, tvApi, intlApi, appApi);

                        //File.WriteAllText($"debug.json", JObject.Parse(webJson).ToString());

                        var savePath = "";

                        //此处代码简直灾难，后续优化吧
                        if ((videoTracks.Count != 0 || audioTracks.Count != 0) && clips.Count == 0)   //dash
                        {
                            if (webJsonStr.Contains("\"video\":[") && videoTracks.Count == 0)
                            {
                                LogError("没有找到符合要求的视频流");
                                if (!audioOnly) continue;
                            }
                            if (webJsonStr.Contains("\"audio\":[") && audioTracks.Count == 0)
                            {
                                LogError("没有找到符合要求的音频流");
                                if (!videoOnly) continue;
                            }
                            //排序
                            //videoTracks.Sort((v1, v2) => Compare(v1, v2, encodingPriority, dfnPriority));
                            videoTracks = SortTracks(videoTracks, dfnPriority, encodingPriority);
                            audioTracks.Sort(Compare);

                            if (audioOnly) videoTracks.Clear();
                            if (videoOnly) audioTracks.Clear();

                            int vIndex = 0;
                            int aIndex = 0;

                            if (!hideStreams)
                            {
                                //展示所有的音视频流信息
                                if (videoTracks.Count > 0)
                                {
                                    Log($"共计{videoTracks.Count}条视频流.");
                                    int index = 0;
                                    foreach (var v in videoTracks)
                                    {
                                        int pDur = p.dur == 0 ? v.dur : p.dur;
                                        LogColor($"{index++}. [{v.dfn}] [{v.res}] [{v.codecs}] [{v.fps}] [{v.bandwith} kbps] [~{FormatFileSize(pDur * v.bandwith * 1024 / 8)}]".Replace("[] ", ""), false);
                                        if (infoMode) Console.WriteLine(v.baseUrl);
                                    }
                                }
                                if (audioTracks.Count > 0)
                                {
                                    Log($"共计{audioTracks.Count}条音频流.");
                                    int index = 0;
                                    foreach (var a in audioTracks)
                                    {
                                        int pDur = p.dur == 0 ? a.dur : p.dur;
                                        LogColor($"{index++}. [{a.codecs}] [{a.bandwith} kbps] [~{FormatFileSize(pDur * a.bandwith * 1024 / 8)}]", false);
                                        if (infoMode) Console.WriteLine(a.baseUrl);
                                    }
                                }
                            }
                            if (infoMode) continue;
                            if (interactMode && !hideStreams)
                            {
                                if (videoTracks.Count > 0)
                                {
                                    Log("请选择一条视频流(输入序号): ", false);
                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                    vIndex = Convert.ToInt32(Console.ReadLine());
                                    if (vIndex > videoTracks.Count || vIndex < 0) vIndex = 0;
                                    Console.ResetColor();
                                }
                                if (audioTracks.Count > 0)
                                {
                                    Log("请选择一条音频流(输入序号): ", false);
                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                    aIndex = Convert.ToInt32(Console.ReadLine());
                                    if (aIndex > audioTracks.Count || aIndex < 0) aIndex = 0;
                                    Console.ResetColor();
                                }
                            }

                            Log($"已选择的流:");
                            if (videoTracks.Count > 0)
                                LogColor($"[视频] [{videoTracks[vIndex].dfn}] [{videoTracks[vIndex].res}] [{videoTracks[vIndex].codecs}] [{videoTracks[vIndex].fps}] [{videoTracks[vIndex].bandwith} kbps] [~{FormatFileSize(videoTracks[vIndex].dur * videoTracks[vIndex].bandwith * 1024 / 8)}]".Replace("[] ", ""), false);
                            if (audioTracks.Count > 0)
                                LogColor($"[音频] [{audioTracks[aIndex].codecs}] [{audioTracks[aIndex].bandwith} kbps] [~{FormatFileSize(audioTracks[aIndex].dur * audioTracks[aIndex].bandwith * 1024 / 8)}]", false);

                            //处理PCDN
                            var pcdnReg = new Regex("://.*:\\d+/");
                            if (videoTracks.Count > 0 && pcdnReg.IsMatch(videoTracks[vIndex].baseUrl))
                            {
                                LogWarn($"检测到视频流为PCDN，尝试强制替换为{BACKUP_HOST}……");
                                videoTracks[vIndex].baseUrl = pcdnReg.Replace(videoTracks[vIndex].baseUrl, $"://{BACKUP_HOST}/");
                            }

                            if (audioTracks.Count > 0 && pcdnReg.IsMatch(audioTracks[aIndex].baseUrl))
                            {
                                LogWarn($"检测到音频流为PCDN，尝试强制替换为{BACKUP_HOST}……");
                                audioTracks[aIndex].baseUrl = pcdnReg.Replace(audioTracks[aIndex].baseUrl, $"://{BACKUP_HOST}/");
                            }

                            LogDebug("Format Before: " + savePathFormat);
                            savePath = FormatSavePath(savePathFormat, title, videoTracks.ElementAtOrDefault(vIndex), audioTracks.ElementAtOrDefault(aIndex), p, pagesCount);
                            LogDebug("Format After: " + savePath);

                            if (downloadDanmaku)
                            {
                                var danmakuXmlPath = savePath.Substring(0, savePath.LastIndexOf('.')) + ".xml";
                                var danmakuAssPath = savePath.Substring(0, savePath.LastIndexOf('.')) + ".ass";
                                Log("正在下载弹幕Xml文件");
                                string danmakuUrl = "https://comment.bilibili.com/" + p.cid + ".xml";
                                await DownloadFile(danmakuUrl, danmakuXmlPath, false, aria2cProxy);
                                var danmakus = DanmakuUtil.ParseXml(danmakuXmlPath);
                                if (danmakus != null)
                                {
                                    Log("正在保存弹幕Ass文件...");
                                    await DanmakuUtil.SaveAsAssAsync(danmakus, danmakuAssPath);
                                }
                                else
                                {
                                    Log("弹幕Xml解析失败, 删除Xml...");
                                    File.Delete(danmakuXmlPath);
                                }
                            }

                            if (videoTracks.Count > 0)
                            {

                                if (!infoMode && File.Exists(savePath) && new FileInfo(savePath).Length != 0)
                                {
                                    Log($"{savePath}已存在, 跳过下载...");
                                    File.Delete(coverPath);
                                    if (Directory.Exists(p.aid) && Directory.GetFiles(p.aid).Length == 0)
                                    {
                                        Directory.Delete(p.aid, true);
                                    }
                                    continue;
                                }

                                //杜比视界，若ffmpeg版本小于5.0，使用mp4box封装
                                if (videoTracks[vIndex].dfn == Config.qualitys["126"] && !useMp4box && !CheckFFmpegDOVI())
                                {
                                    LogWarn($"检测到杜比视界清晰度且您的ffmpeg版本小于5.0,将使用mp4box混流...");
                                    useMp4box = true;
                                }
                                if (multiThread && !videoTracks[vIndex].baseUrl.Contains("-cmcc-"))
                                {
                                    // 下载前先清理残片
                                    foreach (var file in new DirectoryInfo(Path.GetDirectoryName(videoPath)).EnumerateFiles("*.?clip")) file.Delete();
                                    Log($"开始多线程下载P{p.index}视频...");
                                    await MultiThreadDownloadFileAsync(videoTracks[vIndex].baseUrl, videoPath, useAria2c, aria2cProxy, forceHttp);
                                    Log("合并视频分片...");
                                    CombineMultipleFilesIntoSingleFile(GetFiles(Path.GetDirectoryName(videoPath), ".vclip"), videoPath);
                                    Log("清理分片...");
                                    foreach (var file in new DirectoryInfo(Path.GetDirectoryName(videoPath)).EnumerateFiles("*.?clip")) file.Delete();
                                }
                                else
                                {
                                    if (multiThread && videoTracks[vIndex].baseUrl.Contains("-cmcc-"))
                                    {
                                        LogWarn("检测到cmcc域名cdn, 已经禁用多线程");
                                        forceHttp = false;
                                    }
                                    Log($"开始下载P{p.index}视频...");
                                    await DownloadFile(videoTracks[vIndex].baseUrl, videoPath, useAria2c, aria2cProxy, forceHttp);
                                }
                            }
                            if (audioTracks.Count > 0)
                            {
                                if (multiThread && !audioTracks[aIndex].baseUrl.Contains("-cmcc-"))
                                {
                                    // 下载前先清理残片
                                    foreach (var file in new DirectoryInfo(Path.GetDirectoryName(audioPath)).EnumerateFiles("*.?clip")) file.Delete();
                                    Log($"开始多线程下载P{p.index}音频...");
                                    await MultiThreadDownloadFileAsync(audioTracks[aIndex].baseUrl, audioPath, useAria2c, aria2cProxy, forceHttp);
                                    Log("合并音频分片...");
                                    CombineMultipleFilesIntoSingleFile(GetFiles(Path.GetDirectoryName(audioPath), ".aclip"), audioPath);
                                    Log("清理分片...");
                                    foreach (var file in new DirectoryInfo(Path.GetDirectoryName(audioPath)).EnumerateFiles("*.?clip")) file.Delete();
                                }
                                else
                                {
                                    if (multiThread && audioTracks[aIndex].baseUrl.Contains("-cmcc-"))
                                    {
                                        LogWarn("检测到cmcc域名cdn, 已经禁用多线程");
                                        forceHttp = false;
                                    }
                                    Log($"开始下载P{p.index}音频...");
                                    await DownloadFile(audioTracks[aIndex].baseUrl, audioPath, useAria2c, aria2cProxy, forceHttp);
                                }
                            }

                            Log($"下载P{p.index}完毕");
                            if (videoTracks.Count == 0) videoPath = "";
                            if (audioTracks.Count == 0) audioPath = "";
                            if (skipMux) continue;
                            Log("开始合并音视频" + (subtitleInfo.Count > 0 ? "和字幕" : "") + "...");
                            if (audioOnly)
                                savePath = string.Join("", savePath.Take(savePath.Length - 4)) + ".m4a";
                            int code = MuxAV(useMp4box, videoPath, audioPath, savePath,
                                desc,
                                title,
                                (pagesCount > 1 || (bangumi && !vInfo.IsBangumiEnd)) ? p.title : "",
                                File.Exists(coverPath) ? coverPath : "",
                                lang,
                                subtitleInfo, audioOnly, videoOnly, p.points);
                            if (code != 0 || !File.Exists(savePath) || new FileInfo(savePath).Length == 0)
                            {
                                LogError("合并失败"); continue;
                            }
                            Log("清理临时文件...");
                            Thread.Sleep(200);
                            if (videoTracks.Count > 0) File.Delete(videoPath);
                            if (audioTracks.Count > 0) File.Delete(audioPath);
                            if (p.points.Count > 0) File.Delete(Path.Combine(Path.GetDirectoryName(string.IsNullOrEmpty(videoPath) ? audioPath : videoPath), "chapters"));
                            foreach (var s in subtitleInfo) File.Delete(s.path);
                            if (pagesInfo.Count == 1 || p.index == pagesInfo.Last().index || p.aid != pagesInfo.Last().aid)
                                File.Delete(coverPath);
                            if (Directory.Exists(p.aid) && Directory.GetFiles(p.aid).Length == 0) Directory.Delete(p.aid, true);
                        }
                        else if (clips.Count > 0 && dfns.Count > 0)   //flv
                        {
                            bool flag = false;
                            int vIndex = 0;
                        reParse:
                            //排序
                            //videoTracks.Sort((v1, v2) => Compare(v1, v2, encodingPriority, dfnPriority));
                            videoTracks = SortTracks(videoTracks, dfnPriority, encodingPriority);

                            if (interactMode && !flag)
                            {
                                int i = 0;
                                dfns.ForEach(key => LogColor($"{i++}.{Config.qualitys[key]}"));
                                Log("请选择最想要的清晰度(输入序号): ", false);
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                vIndex = Convert.ToInt32(Console.ReadLine());
                                if (vIndex > dfns.Count || vIndex < 0) vIndex = 0;
                                Console.ResetColor();
                                //重新解析
                                videoTracks.Clear();
                                (webJsonStr, videoTracks, audioTracks, clips, dfns) = await ExtractTracksAsync(aidOri, p.aid, p.cid, p.epid, tvApi, intlApi, appApi, dfns[vIndex]);
                                flag = true;
                                goto reParse;
                            }

                            Log($"共计{videoTracks.Count}条流(共有{clips.Count}个分段).");
                            int index = 0;
                            foreach (var v in videoTracks)
                            {
                                LogColor($"{index++}. [{v.dfn}] [{v.res}] [{v.codecs}] [{v.fps}] [~{(v.size / 1024 / v.dur * 8).ToString("00")} kbps] [{FormatFileSize(v.size)}]".Replace("[] ", ""), false);
                                if (infoMode)
                                {
                                    clips.ForEach(delegate (string c) { Console.WriteLine(c); });
                                }
                            }
                            if (infoMode) continue;
                            savePath = FormatSavePath(savePathFormat, title, videoTracks.ElementAtOrDefault(vIndex), null, p, pagesCount);
                            if (File.Exists(savePath) && new FileInfo(savePath).Length != 0)
                            {
                                Log($"{savePath}已存在, 跳过下载...");
                                if (pagesInfo.Count == 1 && Directory.Exists(p.aid))
                                {
                                    Directory.Delete(p.aid, true);
                                }
                                continue;
                            }
                            var pad = string.Empty.PadRight(clips.Count.ToString().Length, '0');
                            for (int i = 0; i < clips.Count; i++)
                            {
                                var link = clips[i];
                                videoPath = $"{p.aid}/{p.aid}.P{p.index}.{p.cid}.{i.ToString(pad)}.mp4";
                                if (multiThread && !link.Contains("-cmcc-"))
                                {
                                    if (videoTracks.Count != 0)
                                    {
                                        // 下载前先清理残片
                                        foreach (var file in new DirectoryInfo(Path.GetDirectoryName(videoPath)).EnumerateFiles("*.?clip")) file.Delete();
                                        Log($"开始多线程下载P{p.index}视频, 片段({(i + 1).ToString(pad)}/{clips.Count})...");
                                        await MultiThreadDownloadFileAsync(link, videoPath, useAria2c, aria2cProxy, forceHttp);
                                        Log("合并视频分片...");
                                        CombineMultipleFilesIntoSingleFile(GetFiles(Path.GetDirectoryName(videoPath), ".vclip"), videoPath);
                                        Log("清理分片...");
                                        foreach (var file in new DirectoryInfo(Path.GetDirectoryName(videoPath)).EnumerateFiles("*.?clip")) file.Delete();
                                    }
                                }
                                else
                                {
                                    if (multiThread && link.Contains("-cmcc-"))
                                    {
                                        LogWarn("检测到cmcc域名cdn, 已经禁用多线程");
                                        forceHttp = false;
                                    }
                                    if (videoTracks.Count != 0)
                                    {
                                        Log($"开始下载P{p.index}视频, 片段({(i + 1).ToString(pad)}/{clips.Count})...");
                                        await DownloadFile(link, videoPath, useAria2c, aria2cProxy, forceHttp);
                                    }
                                }
                            }
                            Log($"下载P{p.index}完毕");
                            Log("开始合并分段...");
                            var files = GetFiles(Path.GetDirectoryName(videoPath), ".mp4");
                            videoPath = $"{p.aid}/{p.aid}.P{p.index}.{p.cid}.mp4";
                            MergeFLV(files, videoPath);
                            if (skipMux) continue;
                            Log("开始混流视频" + (subtitleInfo.Count > 0 ? "和字幕" : "") + "...");
                            if (audioOnly)
                                savePath = string.Join("", savePath.Take(savePath.Length - 4)) + ".m4a";
                            int code = MuxAV(false, videoPath, "", savePath,
                                desc,
                                title,
                                (pagesCount > 1 || (bangumi && !vInfo.IsBangumiEnd)) ? p.title : "",
                                File.Exists(coverPath) ? coverPath : "",
                                lang,
                                subtitleInfo, audioOnly, videoOnly, p.points);
                            if (code != 0 || !File.Exists(savePath) || new FileInfo(savePath).Length == 0)
                            {
                                LogError("合并失败"); continue;
                            }
                            Log("清理临时文件...");
                            Thread.Sleep(200);
                            if (videoTracks.Count != 0) File.Delete(videoPath);
                            foreach (var s in subtitleInfo) File.Delete(s.path);
                            if (p.points.Count > 0) File.Delete(Path.Combine(Path.GetDirectoryName(string.IsNullOrEmpty(videoPath) ? audioPath : videoPath), "chapters"));
                            if (pagesInfo.Count == 1 || p.index == pagesInfo.Last().index || p.aid != pagesInfo.Last().aid)
                                File.Delete(coverPath);
                            if (Directory.Exists(p.aid) && Directory.GetFiles(p.aid).Length == 0) Directory.Delete(p.aid, true);
                        }
                        else
                        {
                            if (webJsonStr.Contains("平台不可观看"))
                            {
                                throw new Exception("当前(WEB)平台不可观看，请尝试使用TV API解析。");
                            }
                            else if (webJsonStr.Contains("地区不可观看") || webJsonStr.Contains("地区不支持"))
                            {
                                throw new Exception("当前地区不可观看，尝试设置系统代理后解析。");
                            }
                            else if (webJsonStr.Contains("购买后才能观看"))
                            {
                                throw new Exception("购买后才能观看哦");
                            }
                            LogError("解析此分P失败(使用--debug查看详细信息)");
                            LogDebug("{0}", webJsonStr);
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (++retryCount <= 2)
                        {
                            LogError(ex.Message);
                            LogWarn("下载出现异常, 3秒后将进行自动重试...");
                            await Task.Delay(3000);
                            goto downloadPage;
                        }
                        else throw;
                    }
                }
                Log("任务完成");
            }
            catch (Exception e)
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(Config.DEBUG_LOG ? e.ToString() : e.Message);
                Console.ResetColor();
                Console.WriteLine();
                Thread.Sleep(1);
                Environment.Exit(1);
            }
        }

        private static List<Video> SortTracks(List<Video> videoTracks, Dictionary<string, int> dfnPriority, Dictionary<string, byte> encodingPriority)
        {
            //用户同时输入了自定义分辨率优先级和自定义编码优先级，则根据输入顺序依次进行排序
            if (dfnPriority.Count > 0 && encodingPriority.Count > 0 && Environment.CommandLine.IndexOf("--encoding-priority") < Environment.CommandLine.IndexOf("--dfn-priority"))
            {
                return videoTracks
                    .OrderBy(v =>
                    {
                        if (encodingPriority.TryGetValue(v.codecs, out byte i)) return i;
                        return 100;
                    })
                    .ThenBy(v =>
                    {
                        if (dfnPriority.TryGetValue(v.dfn, out int i)) return i;
                        return 100;
                    })
                    .ThenByDescending(v => Convert.ToInt32(v.id))
                    .ThenByDescending(v => v.bandwith)
                    .ToList();
            }
            else
            {
                return videoTracks
                    .OrderBy(v =>
                    {
                        if (dfnPriority.TryGetValue(v.dfn, out int i)) return i;
                        return 100;
                    })
                    .ThenBy(v =>
                    {
                        if (encodingPriority.TryGetValue(v.codecs, out byte i)) return i;
                        return 100;
                    })
                    .ThenByDescending(v => Convert.ToInt32(v.id))
                    .ThenByDescending(v => v.bandwith)
                    .ToList(); 
            }
        }

        private static string FormatSavePath(string savePathFormat, string title, Video videoTrack, Audio audioTrack, Page p, int pagesCount)
        {
            var result = savePathFormat.Replace('\\', '/');
            var regex = new Regex("<(\\w+?)>");
            foreach (Match m in regex.Matches(result))
            {
                var key = m.Groups[1].Value;
                var v = key switch
                {
                    "videoTitle" => GetValidFileName(title, filterSlash: true),
                    "pageNumber" => p.index.ToString(),
                    "pageNumberWithZero" => p.index.ToString().PadLeft((int)Math.Log10(pagesCount) + 1, '0'),
                    "pageTitle" => GetValidFileName(p.title, filterSlash: true),
                    "aid" => p.aid,
                    "cid" => p.cid,
                    "ownerName" => p.ownerName == null ? "" : GetValidFileName(p.ownerName, filterSlash: true),
                    "ownerMid" => p.ownerMid == null ? "" : p.ownerMid,
                    "dfn" => videoTrack == null ? "" : videoTrack.dfn,
                    "res" => videoTrack == null ? "" : videoTrack.res,
                    "fps" => videoTrack == null ? "" : videoTrack.fps,
                    "videoCodecs" => videoTrack == null ? "" : videoTrack.codecs,
                    "videoBandwidth" => videoTrack == null ? "" : videoTrack.bandwith.ToString(),
                    "audioCodecs" => audioTrack == null ? "" : audioTrack.codecs,
                    "audioBandwidth" => audioTrack == null ? "" : audioTrack.bandwith.ToString(),
                    _ => key
                };
                result = result.Replace(m.Value, v);
            }
            if (!result.EndsWith(".mp4")) { result += ".mp4"; }
            return result;
        }

        private static async Task loginWEB()
        {
            try
            {
                Log("获取登录地址...");
                string loginUrl = "https://passport.bilibili.com/qrcode/getLoginUrl";
                string url = JsonDocument.Parse(await HTTPUtil.GetWebSourceAsync(loginUrl)).RootElement.GetProperty("data").GetProperty("url").ToString();
                string oauthKey = GetQueryString("oauthKey", url);
                //Log(oauthKey);
                //Log(url);
                bool flag = false;
                Log("生成二维码...");
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
                PngByteQRCode pngByteCode = new PngByteQRCode(qrCodeData);
                File.WriteAllBytes("qrcode.png", pngByteCode.GetGraphic(7));
                Log("生成二维码成功：qrcode.png, 请打开并扫描, 或扫描打印的二维码");
                var consoleQRCode = new ConsoleQRCode(qrCodeData);
                consoleQRCode.GetGraphic();

                while (true)
                {
                    await Task.Delay(1000);
                    string w = await GetLoginStatusAsync(oauthKey);
                    string data = JsonDocument.Parse(w).RootElement.GetProperty("data").ToString();
                    if (data == "-2")
                    {
                        LogColor("二维码已过期, 请重新执行登录指令.");
                        break;
                    }
                    else if (data == "-4") //等待扫码
                    {
                        continue;
                    }
                    else if (data == "-5") //等待确认
                    {
                        if (!flag)
                        {
                            Log("扫码成功, 请确认...");
                            flag = !flag;
                        }
                    }
                    else
                    {
                        string cc = JsonDocument.Parse(w).RootElement.GetProperty("data").GetProperty("url").ToString();
                        Log("登录成功: SESSDATA=" + GetQueryString("SESSDATA", cc));
                        //导出cookie
                        File.WriteAllText(Path.Combine(APP_DIR, "BBDown.data"), cc.Substring(cc.IndexOf('?') + 1).Replace("&", ";"));
                        File.Delete("qrcode.png");
                        break;
                    }
                }
            }
            catch (Exception e) { LogError(e.Message); }
        }

        private static async Task loginTV()
        {
            try
            {
                string loginUrl = "https://passport.snm0516.aisee.tv/x/passport-tv-login/qrcode/auth_code";
                string pollUrl = "https://passport.bilibili.com/x/passport-tv-login/qrcode/poll";
                var parms = GetTVLoginParms();
                Log("获取登录地址...");
                byte[] responseArray = await (await HTTPUtil.AppHttpClient.PostAsync(loginUrl, new FormUrlEncodedContent(parms.ToDictionary()))).Content.ReadAsByteArrayAsync();
                string web = Encoding.UTF8.GetString(responseArray);
                string url = JsonDocument.Parse(web).RootElement.GetProperty("data").GetProperty("url").ToString();
                string authCode = JsonDocument.Parse(web).RootElement.GetProperty("data").GetProperty("auth_code").ToString();
                Log("生成二维码...");
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
                PngByteQRCode pngByteCode = new PngByteQRCode(qrCodeData);
                File.WriteAllBytes("qrcode.png", pngByteCode.GetGraphic(7));
                Log("生成二维码成功：qrcode.png, 请打开并扫描, 或扫描打印的二维码");
                var consoleQRCode = new ConsoleQRCode(qrCodeData);
                consoleQRCode.GetGraphic();
                parms.Set("auth_code", authCode);
                parms.Set("ts", GetTimeStamp(true));
                parms.Remove("sign");
                parms.Add("sign", GetSign(ToQueryString(parms)));
                while (true)
                {
                    await Task.Delay(1000);
                    responseArray = await (await HTTPUtil.AppHttpClient.PostAsync(pollUrl, new FormUrlEncodedContent(parms.ToDictionary()))).Content.ReadAsByteArrayAsync();
                    web = Encoding.UTF8.GetString(responseArray);
                    string code = JsonDocument.Parse(web).RootElement.GetProperty("code").ToString();
                    if (code == "86038")
                    {
                        LogColor("二维码已过期, 请重新执行登录指令.");
                        break;
                    }
                    else if (code == "86039") //等待扫码
                    {
                        continue;
                    }
                    else
                    {
                        string cc = JsonDocument.Parse(web).RootElement.GetProperty("data").GetProperty("access_token").ToString();
                        Log("登录成功: AccessToken=" + cc);
                        //导出cookie
                        File.WriteAllText(Path.Combine(APP_DIR, "BBDownTV.data"), "access_token=" + cc);
                        File.Delete("qrcode.png");
                        break;
                    }
                }
            }
            catch (Exception e) { LogError(e.Message); }
        }
    }
}
