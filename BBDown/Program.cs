using QRCoder;
using System;
using System.Collections.Generic;
using System.CommandLine;
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
using System.Text.Json.Serialization;
using System.CommandLine.Binding;
using System.CommandLine.Builder;

namespace BBDown
{
    partial class Program
    {
        private static readonly string BACKUP_HOST = "upos-sz-mirrorcoso1.bilivideo.com";
        public static string SinglePageDefaultSavePath { get; set; } = "<videoTitle>";
        public static string MultiPageDefaultSavePath { get; set; } = "<videoTitle>/[P<pageNumberWithZero>]<pageTitle>";

        public readonly static string APP_DIR = Path.GetDirectoryName(Environment.ProcessPath)!;

        private static int Compare(Audio r1, Audio r2)
        {
            return r1.bandwith - r2.bandwith > 0 ? -1 : 1;
        }

        [JsonSerializable(typeof(MyOption))]
        partial class MyOptionJsonContext : JsonSerializerContext { }

        private static void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            LogWarn("Force Exit...");
            try
            {
                Console.ResetColor();
                Console.CursorVisible = true;
                if (!OperatingSystem.IsWindows())
                    System.Diagnostics.Process.Start("stty", "echo");
            }
            catch { }
            Environment.Exit(0);
        }

        public static async Task<int> Main(params string[] args)
        {
            Console.CancelKeyPress += Console_CancelKeyPress;
            ServicePointManager.DefaultConnectionLimit = 2048;

            var rootCommand = CommandLineInvoker.GetRootCommand(DoWorkAsync);
            Command loginCommand = new(
                "login",
                "通过APP扫描二维码以登录您的WEB账号");
            rootCommand.AddCommand(loginCommand);
            Command loginTVCommand = new(
                "logintv",
                "通过APP扫描二维码以登录您的TV账号");
            rootCommand.AddCommand(loginTVCommand);
            rootCommand.Description = "BBDown是一个免费且便捷高效的哔哩哔哩下载/解析软件.";
            rootCommand.TreatUnmatchedTokensAsErrors = true;

            //WEB登录
            loginCommand.SetHandler(LoginWEB);

            //TV登录
            loginTVCommand.SetHandler(LoginTV);

            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;
            var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version!;
            Console.Write($"BBDown version {ver.Major}.{ver.Minor}.{ver.Build}, Bilibili Downloader.\r\n");
            Console.ResetColor();
            Console.Write("欢迎到讨论区交流：\r\n" +
                "https://github.com/nilaoda/BBDown/discussions\r\n");
            Console.WriteLine();

            var parser = new CommandLineBuilder(rootCommand)
                .UseDefaults()
                .EnablePosixBundling(false)
                .UseExceptionHandler((ex, context) =>
                {
                    LogError(ex.Message);
                    try { Console.CursorVisible = true; } catch { }
                    Thread.Sleep(3000);
                }, 1)
                .Build();

            var newArgsList = new List<string>();
            var commandLineResult = rootCommand.Parse(args);

            //显式抛出异常
            if (commandLineResult.Errors.Count > 0)
            {
                LogError(commandLineResult.Errors.First().Message);
                return 1;
            }

            if (commandLineResult.CommandResult.Command.Name.ToLower() != Path.GetFileNameWithoutExtension(Environment.ProcessPath)!.ToLower())
            {
                newArgsList.Add(commandLineResult.CommandResult.Command.Name);
                return await parser.InvokeAsync(newArgsList.ToArray());
            }

            foreach (var item in commandLineResult.CommandResult.Children)
            {
                if (item is ArgumentResult a)
                {
                    newArgsList.Add(a.Tokens[0].Value);
                }
                else if (item is OptionResult o)
                {
                    newArgsList.Add("--" + o.Option.Name);
                    newArgsList.AddRange(o.Tokens.Select(t => t.Value));
                }
            }
            if (newArgsList.Contains("--debug")) Config.DEBUG_LOG = true;

            //处理配置文件
            BBDownConfigParser.HandleConfig(newArgsList, rootCommand);

            return await parser.InvokeAsync(newArgsList.ToArray());
        }

        private static async Task DoWorkAsync(MyOption myOption)
        {
            //检测更新
            CheckUpdateAsync();
            try
            {
                //兼容旧版本命令行参数并给出警告
                if (myOption.AddDfnSubfix)
                {
                    LogWarn("--add-dfn-subfix 已被弃用, 建议使用 --file-pattern/-F 或 --multi-file-pattern/-M 来自定义输出文件名格式");
                    if (string.IsNullOrEmpty(myOption.FilePattern) && string.IsNullOrEmpty(myOption.MultiFilePattern))
                    {
                        SinglePageDefaultSavePath += "[<dfn>]";
                        MultiPageDefaultSavePath += "[<dfn>]";
                        LogWarn($"已切换至 -F \"{SinglePageDefaultSavePath}\" -M \"{MultiPageDefaultSavePath}\"");
                    }
                }
                if (myOption.Aria2cProxy != "")
                {
                    LogWarn("--aria2c-proxy 已被弃用, 请使用 --aria2c-args 来设置aria2c代理, 本次执行已添加该代理");
                    myOption.Aria2cArgs += $" --all-proxy=\"{myOption.Aria2cProxy}\"";
                }
                if (myOption.OnlyHevc)
                {
                    LogWarn("--only-hevc/-hevc 已被弃用, 请使用 --encoding-priority 来设置编码优先级, 本次执行已将hevc设置为最高优先级");
                    myOption.EncodingPriority = "hevc";
                }
                if (myOption.OnlyAvc)
                {
                    LogWarn("--only-avc/-avc 已被弃用, 请使用 --encoding-priority 来设置编码优先级, 本次执行已将avc设置为最高优先级");
                    myOption.EncodingPriority = "avc";
                }
                if (myOption.OnlyAv1)
                {
                    LogWarn("--only-av1/-av1 已被弃用, 请使用 --encoding-priority 来设置编码优先级, 本次执行已将av1设置为最高优先级");
                    myOption.EncodingPriority = "av1";
                }
                if (myOption.NoPaddingPageNum)
                {
                    LogWarn("--no-padding-page-num 已被弃用, 建议使用 --file-pattern/-F 或 --multi-file-pattern/-M 来自定义输出文件名格式");
                    if (string.IsNullOrEmpty(myOption.FilePattern) && string.IsNullOrEmpty(myOption.MultiFilePattern))
                    {
                        MultiPageDefaultSavePath = MultiPageDefaultSavePath.Replace("<pageNumberWithZero>", "<pageNumber>");
                        LogWarn($"已切换至 -M \"{MultiPageDefaultSavePath}\"");
                    }
                }
                if (myOption.BandwithAscending)
                {
                    LogWarn("--bandwith-ascending 已被弃用, 建议使用 --video-ascending 与 --audio-ascending 来指定视频或音频是否升序, 本次执行已将视频与音频均设为升序");
                    myOption.VideoAscending = true;
                    myOption.AudioAscending = true;
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
                bool danmakuOnly = myOption.DanmakuOnly;
                bool coverOnly = myOption.CoverOnly;
                bool subOnly = myOption.SubOnly;
                bool skipMux = myOption.SkipMux;
                bool skipSubtitle = myOption.SkipSubtitle;
                bool skipCover = myOption.SkipCover;
                bool forceHttp = myOption.ForceHttp;
                bool downloadDanmaku = myOption.DownloadDanmaku || danmakuOnly;
                bool skipAi = myOption.SkipAi;
                bool videoAscending = myOption.VideoAscending;
                bool audioAscending = myOption.AudioAscending;
                bool allowPcdn = myOption.AllowPcdn;
                bool showAll = myOption.ShowAll;
                bool useAria2c = myOption.UseAria2c;
                string aria2cArgs = myOption.Aria2cArgs;
                Config.DEBUG_LOG = myOption.Debug;
                string input = myOption.Url;
                string savePathFormat = myOption.FilePattern;
                string lang = myOption.Language;
                string selectPage = myOption.SelectPage.ToUpper();
                string uposHost = myOption.UposHost;
                string aidOri = ""; //原始aid
                int delay = Convert.ToInt32(myOption.DelayPerPage);
                Config.HOST = myOption.Host;
                Config.EPHOST = myOption.EpHost;
                Config.AREA = myOption.Area;
                Config.COOKIE = myOption.Cookie;
                Config.TOKEN = myOption.AccessToken.Replace("access_token=", "");

                if (interactMode) hideStreams = false; //手动选择时不能隐藏流

                if (!string.IsNullOrEmpty(myOption.WorkDir))
                {
                    //解释环境变量
                    myOption.WorkDir = Environment.ExpandEnvironmentVariables(myOption.WorkDir);
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
                            var binPath = FindExecutable("mp4box") ?? FindExecutable("MP4box");
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

                List<string>? selectedPages = null;
                if (!string.IsNullOrEmpty(GetQueryString("p", input)))
                {
                    selectedPages = new List<string>
                    {
                        GetQueryString("p", input)
                    };
                }

                LogDebug("AppDirectory: {0}", APP_DIR);
                LogDebug("运行参数：{0}", JsonSerializer.Serialize(myOption, MyOptionJsonContext.Default.MyOption));
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
                bool is_login = await CheckLogin(Config.COOKIE);
                if (!intlApi && !tvApi && Config.AREA == "")
                {
                    Log("检测账号登录...");
                    if (!is_login)
                    {
                        LogWarn("你尚未登录B站账号, 解析可能受到限制");
                    }
                }

                Log("获取aid...");
                aidOri = await GetAvIdAsync(input);
                Log("获取aid结束: " + aidOri);
                //-p的优先级大于URL中的自带p参数, 所以先清空selectedPages
                if (!string.IsNullOrEmpty(selectPage) && selectPage != "ALL")
                {
                    selectedPages = new List<string>();
                    try
                    {
                        string tmp = selectPage;
                        tmp = tmp.Trim().Trim(',');
                        if (tmp.Contains('-'))
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
                    fetcher = intlApi ? new IntlBangumiInfoFetcher() : new BangumiInfoFetcher();
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
                List<Subtitle> subtitleInfo = new();
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
                if (!string.IsNullOrEmpty(selectPage) && (selectPage == "LAST" || selectPage == "NEW" || selectPage == "LATEST"))
                {
                    try
                    {
                        selectedPages = new List<string> { pagesInfo.Count.ToString() };
                        Log("程序已选择最新一P");
                    }
                    catch { LogError("解析分P参数时失败了~"); selectedPages = null; };
                }

                //如果用户没有选择分P, 根据epid来确定某一集
                if (selectedPages == null && selectPage != "ALL" && !string.IsNullOrEmpty(vInfo.Index))
                {
                    selectedPages = new List<string> { vInfo.Index };
                    Log("程序已自动选择你输入的集数, 如果要下载其他集数请自行指定分P(如可使用-p ALL代表全部)");
                }

                Log($"共计 {pagesInfo.Count} 个分P, 已选择：" + (selectedPages == null ? "ALL" : string.Join(",", selectedPages)));
                var pagesCount = pagesInfo.Count;

                //过滤不需要的分P
                if (selectedPages != null)
                    pagesInfo = pagesInfo.Where(p => selectedPages.Contains(p.index.ToString())).ToList();

                // 根据p数选择存储路径
                savePathFormat = string.IsNullOrEmpty(myOption.FilePattern) ? SinglePageDefaultSavePath : myOption.FilePattern;
                // 1. 多P; 2. 只有1P, 但是是番剧, 尚未完结时 按照多P处理
                if (pagesCount > 1 || (bangumi && !vInfo.IsBangumiEnd))
                {
                    savePathFormat = string.IsNullOrEmpty(myOption.MultiFilePattern) ? MultiPageDefaultSavePath : myOption.MultiFilePattern;
                }

                foreach (Page p in pagesInfo)
                {
                    int vIndex = 0; //用户手动选择的视频序号
                    int aIndex = 0; //用户手动选择的音频序号
                    bool selected = false; //用户是否已经手动选择过了轨道
                    int retryCount = 0;
                downloadPage:
                    try
                    {
                        string desc = string.IsNullOrEmpty(p.desc) ? vInfo.Desc : p.desc;
                        if (pagesInfo.Count > 1 && delay > 0)
                        {
                            Log($"停顿{delay}秒...");
                            await Task.Delay(delay * 1000);
                        }

                        Log($"开始解析P{p.index}...");

                        LogDebug("尝试获取章节信息...");
                        p.points = await FetchPointsAsync(p.cid, p.aid);

                        string webJsonStr = "";
                        List<Video> videoTracks = new();
                        List<Audio> audioTracks = new();
                        List<string> clips = new();
                        List<string> dfns = new();

                        string videoPath = $"{p.aid}/{p.aid}.P{p.index}.{p.cid}.mp4";
                        string audioPath = $"{p.aid}/{p.aid}.P{p.index}.{p.cid}.m4a";
                        var coverPath = $"{p.aid}/{p.aid}.jpg";

                        //处理文件夹以.结尾导致的异常情况
                        if (title.EndsWith(".")) title += "_fix";
                        //处理文件夹以.开头导致的异常情况
                        if (title.StartsWith(".")) title = "_" + title;

                        //处理封面&&字幕
                        if (!infoMode)
                        {
                            if (!Directory.Exists(p.aid))
                            {
                                Directory.CreateDirectory(p.aid);
                            }
                            if (!skipCover && !subOnly && !File.Exists(coverPath) && !danmakuOnly && !coverOnly)
                            {
                                await DownloadCoverAsync(pic, p, coverPath);
                            }

                            if (!skipSubtitle && !danmakuOnly && !coverOnly)
                            {
                                LogDebug("获取字幕...");
                                subtitleInfo = await SubUtil.GetSubtitlesAsync(p.aid, p.cid, p.epid, p.index, intlApi);
                                foreach (Subtitle s in subtitleInfo)
                                {
                                    if (skipAi && s.lan.StartsWith("ai-")) {
                                        Log($"跳过下载AI字幕 {s.lan} => {SubUtil.GetSubtitleCode(s.lan).Item2}");
                                        continue;
                                    }
                                    Log($"下载字幕 {s.lan} => {SubUtil.GetSubtitleCode(s.lan).Item2}...");
                                    LogDebug("下载：{0}", s.url);
                                    await SubUtil.SaveSubtitleAsync(s.url, s.path);
                                    if (subOnly && File.Exists(s.path) && File.ReadAllText(s.path) != "")
                                    {
                                        var _outSubPath = FormatSavePath(savePathFormat, title, null, null, p, pagesCount, tvApi, appApi, intlApi);
                                        if (_outSubPath.Contains('/'))
                                        {
                                            if (!Directory.Exists(_outSubPath.Split('/').First()))
                                                Directory.CreateDirectory(_outSubPath.Split('/').First());
                                        }
                                        _outSubPath = _outSubPath[.._outSubPath.LastIndexOf('.')] + $".{s.lan}.srt";
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

                        if (Config.DEBUG_LOG)
                            File.WriteAllText($"debug.json", webJsonStr);

                        var savePath = "";

                        //此处代码简直灾难, 后续优化吧
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
                            videoTracks = SortTracks(videoTracks, dfnPriority, encodingPriority, videoAscending);
                            audioTracks.Sort(Compare);
                            if (audioAscending) audioTracks.Reverse();

                            if (audioOnly) videoTracks.Clear();
                            if (videoOnly) audioTracks.Clear();

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
                                        var size = v.size > 0 ? v.size : pDur * v.bandwith * 1024 / 8;
                                        LogColor($"{index++}. [{v.dfn}] [{v.res}] [{v.codecs}] [{v.fps}] [{v.bandwith} kbps] [~{FormatFileSize(size)}]".Replace("[] ", ""), false);
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

                            if (downloadDanmaku)
                            {
                                LogDebug("Format Before: " + savePathFormat);
                                savePath = FormatSavePath(savePathFormat, title, videoTracks.ElementAtOrDefault(vIndex), audioTracks.ElementAtOrDefault(aIndex), p, pagesCount, tvApi, appApi, intlApi);
                                LogDebug("Format After: " + savePath);
                                var danmakuXmlPath = savePath[..savePath.LastIndexOf('.')] + ".xml";
                                var danmakuAssPath = savePath[..savePath.LastIndexOf('.')] + ".ass";
                                Log("正在下载弹幕Xml文件");
                                string danmakuUrl = "https://comment.bilibili.com/" + p.cid + ".xml";
                                await DownloadFile(danmakuUrl, danmakuXmlPath, false, aria2cArgs);
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
                                if (danmakuOnly)
                                {
                                    if (Directory.Exists(p.aid))
                                    {
                                        Directory.Delete(p.aid);
                                    }
                                    continue;
                                }
                            }

                            if (interactMode && !hideStreams && !selected)
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
                                selected = true;
                            }

                            LogDebug("Format Before: " + savePathFormat);
                            savePath = FormatSavePath(savePathFormat, title, videoTracks.ElementAtOrDefault(vIndex), audioTracks.ElementAtOrDefault(aIndex), p, pagesCount, tvApi, appApi, intlApi);
                            LogDebug("Format After: " + savePath);

                            if (coverOnly)
                            {
                                var newCoverPath = savePath[..savePath.LastIndexOf('.')] + Path.GetExtension(pic);
                                await DownloadCoverAsync(pic, p, newCoverPath);
                                if (Directory.Exists(p.aid) && Directory.GetFiles(p.aid).Length == 0) Directory.Delete(p.aid, true);
                                continue;
                            }

                            Log($"已选择的流:");
                            if (videoTracks.Count > 0)
                            {
                                var size = videoTracks[vIndex].size > 0 ? videoTracks[vIndex].size : videoTracks[vIndex].dur * videoTracks[vIndex].bandwith * 1024 / 8;
                                LogColor($"[视频] [{videoTracks[vIndex].dfn}] [{videoTracks[vIndex].res}] [{videoTracks[vIndex].codecs}] [{videoTracks[vIndex].fps}] [{videoTracks[vIndex].bandwith} kbps] [~{FormatFileSize(size)}]".Replace("[] ", ""), false);
                            }
                            if (audioTracks.Count > 0)
                                LogColor($"[音频] [{audioTracks[aIndex].codecs}] [{audioTracks[aIndex].bandwith} kbps] [~{FormatFileSize(audioTracks[aIndex].dur * audioTracks[aIndex].bandwith * 1024 / 8)}]", false);

                            if (uposHost == "")
                            {
                                //处理PCDN
                                if (!allowPcdn)
                                {
                                    var pcdnReg = PcdnRegex();
                                    if (videoTracks.Count > 0 && pcdnReg.IsMatch(videoTracks[vIndex].baseUrl))
                                    {
                                        LogWarn($"检测到视频流为PCDN, 尝试强制替换为{BACKUP_HOST}……");
                                        videoTracks[vIndex].baseUrl = pcdnReg.Replace(videoTracks[vIndex].baseUrl, $"://{BACKUP_HOST}/");
                                    }
                                    if (audioTracks.Count > 0 && pcdnReg.IsMatch(audioTracks[aIndex].baseUrl))
                                    {
                                        LogWarn($"检测到音频流为PCDN, 尝试强制替换为{BACKUP_HOST}……");
                                        audioTracks[aIndex].baseUrl = pcdnReg.Replace(audioTracks[aIndex].baseUrl, $"://{BACKUP_HOST}/");
                                    }
                                }

                                var akamReg = AkamRegex();
                                if (videoTracks.Count > 0 && Config.AREA != "" && videoTracks[vIndex].baseUrl.Contains("akamaized.net"))
                                {
                                    LogWarn($"检测到视频流为外国源, 尝试强制替换为{BACKUP_HOST}……");
                                    videoTracks[vIndex].baseUrl = akamReg.Replace(videoTracks[vIndex].baseUrl, $"://{BACKUP_HOST}/");
                                }
                                if (audioTracks.Count > 0 && Config.AREA != "" && audioTracks[aIndex].baseUrl.Contains("akamaized.net"))
                                {
                                    LogWarn($"检测到音频流为外国源, 尝试强制替换为{BACKUP_HOST}……");
                                    audioTracks[aIndex].baseUrl = akamReg.Replace(audioTracks[aIndex].baseUrl, $"://{BACKUP_HOST}/");
                                }
                            }
                            else
                            {
                                var uposReg = UposRegex();
                                if (videoTracks.Count > 0)
                                {
                                    Log($"尝试将视频流强制替换为{uposHost}……");
                                    videoTracks[vIndex].baseUrl = uposReg.Replace(videoTracks[vIndex].baseUrl, $"://{uposHost}/");
                                }
                                if (audioTracks.Count > 0)
                                {
                                    Log($"尝试将音频流强制替换为{uposHost}……");
                                    audioTracks[aIndex].baseUrl = uposReg.Replace(audioTracks[aIndex].baseUrl, $"://{uposHost}/");
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

                                //杜比视界, 若ffmpeg版本小于5.0, 使用mp4box封装
                                if (videoTracks[vIndex].dfn == Config.qualitys["126"] && !useMp4box && !CheckFFmpegDOVI())
                                {
                                    LogWarn($"检测到杜比视界清晰度且您的ffmpeg版本小于5.0,将使用mp4box混流...");
                                    useMp4box = true;
                                }
                                if (multiThread && !videoTracks[vIndex].baseUrl.Contains("-cmcc-"))
                                {
                                    // 下载前先清理残片
                                    foreach (var file in new DirectoryInfo(Path.GetDirectoryName(videoPath)!).EnumerateFiles("*.?clip")) file.Delete();
                                    Log($"开始多线程下载P{p.index}视频...");
                                    await MultiThreadDownloadFileAsync(videoTracks[vIndex].baseUrl, videoPath, useAria2c, aria2cArgs, forceHttp);
                                    Log("合并视频分片...");
                                    CombineMultipleFilesIntoSingleFile(GetFiles(Path.GetDirectoryName(videoPath)!, ".vclip"), videoPath);
                                    Log("清理分片...");
                                    foreach (var file in new DirectoryInfo(Path.GetDirectoryName(videoPath)!).EnumerateFiles("*.?clip")) file.Delete();
                                }
                                else
                                {
                                    if (multiThread && videoTracks[vIndex].baseUrl.Contains("-cmcc-"))
                                    {
                                        LogWarn("检测到cmcc域名cdn, 已经禁用多线程");
                                        forceHttp = false;
                                    }
                                    Log($"开始下载P{p.index}视频...");
                                    await DownloadFile(videoTracks[vIndex].baseUrl, videoPath, useAria2c, aria2cArgs, forceHttp);
                                }
                            }
                            if (audioTracks.Count > 0)
                            {
                                if (multiThread && !audioTracks[aIndex].baseUrl.Contains("-cmcc-"))
                                {
                                    // 下载前先清理残片
                                    foreach (var file in new DirectoryInfo(Path.GetDirectoryName(audioPath)!).EnumerateFiles("*.?clip")) file.Delete();
                                    Log($"开始多线程下载P{p.index}音频...");
                                    await MultiThreadDownloadFileAsync(audioTracks[aIndex].baseUrl, audioPath, useAria2c, aria2cArgs, forceHttp);
                                    Log("合并音频分片...");
                                    CombineMultipleFilesIntoSingleFile(GetFiles(Path.GetDirectoryName(audioPath)!, ".aclip"), audioPath);
                                    Log("清理分片...");
                                    foreach (var file in new DirectoryInfo(Path.GetDirectoryName(audioPath)!).EnumerateFiles("*.?clip")) file.Delete();
                                }
                                else
                                {
                                    if (multiThread && audioTracks[aIndex].baseUrl.Contains("-cmcc-"))
                                    {
                                        LogWarn("检测到cmcc域名cdn, 已经禁用多线程");
                                        forceHttp = false;
                                    }
                                    Log($"开始下载P{p.index}音频...");
                                    await DownloadFile(audioTracks[aIndex].baseUrl, audioPath, useAria2c, aria2cArgs, forceHttp);
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
                                p.ownerName ?? "",
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
                            if (p.points.Count > 0) File.Delete(Path.Combine(Path.GetDirectoryName(string.IsNullOrEmpty(videoPath) ? audioPath : videoPath)!, "chapters"));
                            foreach (var s in subtitleInfo) File.Delete(s.path);
                            if (pagesInfo.Count == 1 || p.index == pagesInfo.Last().index || p.aid != pagesInfo.Last().aid)
                                File.Delete(coverPath);
                            if (Directory.Exists(p.aid) && Directory.GetFiles(p.aid).Length == 0) Directory.Delete(p.aid, true);
                        }
                        else if (clips.Count > 0 && dfns.Count > 0)   //flv
                        {
                            bool flag = false;
                        reParse:
                            //排序
                            //videoTracks.Sort((v1, v2) => Compare(v1, v2, encodingPriority, dfnPriority));
                            videoTracks = SortTracks(videoTracks, dfnPriority, encodingPriority, videoAscending);

                            if (interactMode && !flag && !selected)
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
                                selected = true;
                                goto reParse;
                            }

                            Log($"共计{videoTracks.Count}条流(共有{clips.Count}个分段).");
                            int index = 0;
                            foreach (var v in videoTracks)
                            {
                                LogColor($"{index++}. [{v.dfn}] [{v.res}] [{v.codecs}] [{v.fps}] [~{v.size / 1024 / v.dur * 8:00} kbps] [{FormatFileSize(v.size)}]".Replace("[] ", ""), false);
                                if (infoMode)
                                {
                                    clips.ForEach(delegate (string c) { Console.WriteLine(c); });
                                }
                            }
                            if (infoMode) continue;
                            savePath = FormatSavePath(savePathFormat, title, videoTracks.ElementAtOrDefault(vIndex), null, p, pagesCount, tvApi, appApi, intlApi);
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
                                        foreach (var file in new DirectoryInfo(Path.GetDirectoryName(videoPath)!).EnumerateFiles("*.?clip")) file.Delete();
                                        Log($"开始多线程下载P{p.index}视频, 片段({(i + 1).ToString(pad)}/{clips.Count})...");
                                        await MultiThreadDownloadFileAsync(link, videoPath, useAria2c, aria2cArgs, forceHttp);
                                        Log("合并视频分片...");
                                        CombineMultipleFilesIntoSingleFile(GetFiles(Path.GetDirectoryName(videoPath)!, ".vclip"), videoPath);
                                        Log("清理分片...");
                                        foreach (var file in new DirectoryInfo(Path.GetDirectoryName(videoPath)!).EnumerateFiles("*.?clip")) file.Delete();
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
                                        await DownloadFile(link, videoPath, useAria2c, aria2cArgs, forceHttp);
                                    }
                                }
                            }
                            Log($"下载P{p.index}完毕");
                            Log("开始合并分段...");
                            var files = GetFiles(Path.GetDirectoryName(videoPath)!, ".mp4");
                            videoPath = $"{p.aid}/{p.aid}.P{p.index}.{p.cid}.mp4";
                            MergeFLV(files, videoPath);
                            if (skipMux) continue;
                            Log("开始混流视频" + (subtitleInfo.Count > 0 ? "和字幕" : "") + "...");
                            if (audioOnly)
                                savePath = string.Join("", savePath.Take(savePath.Length - 4)) + ".m4a";
                            int code = MuxAV(false, videoPath, "", savePath,
                                desc,
                                title,
                                p.ownerName ?? "",
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
                            if (p.points.Count > 0) File.Delete(Path.Combine(Path.GetDirectoryName(string.IsNullOrEmpty(videoPath) ? audioPath : videoPath)!, "chapters"));
                            if (pagesInfo.Count == 1 || p.index == pagesInfo.Last().index || p.aid != pagesInfo.Last().aid)
                                File.Delete(coverPath);
                            if (Directory.Exists(p.aid) && Directory.GetFiles(p.aid).Length == 0) Directory.Delete(p.aid, true);
                        }
                        else
                        {
                            if (webJsonStr.Contains("平台不可观看"))
                            {
                                throw new Exception("当前(WEB)平台不可观看, 请尝试使用TV API解析。");
                            }
                            else if (webJsonStr.Contains("地区不可观看") || webJsonStr.Contains("地区不支持"))
                            {
                                throw new Exception("当前地区不可观看, 尝试设置系统代理后解析。");
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

        private static async Task DownloadCoverAsync(string pic, Page p, string coverPath)
        {
            Log("下载封面...");
            var cover = pic == "" ? p.cover : pic;
            if (cover != null)
            {
                LogDebug("下载：{0}", cover);
                await using var response = await HTTPUtil.AppHttpClient.GetStreamAsync(cover);
                await using var fs = new FileStream(coverPath, FileMode.Create);
                await response.CopyToAsync(fs);
            }
        }

        private static List<Video> SortTracks(List<Video> videoTracks, Dictionary<string, int> dfnPriority, Dictionary<string, byte> encodingPriority, bool videoAscending)
        {
            //用户同时输入了自定义分辨率优先级和自定义编码优先级, 则根据输入顺序依次进行排序
            return dfnPriority.Count > 0 && encodingPriority.Count > 0 && Environment.CommandLine.IndexOf("--encoding-priority") < Environment.CommandLine.IndexOf("--dfn-priority")
                ? videoTracks
                    .OrderBy(v => encodingPriority.TryGetValue(v.codecs, out byte i) ? i : 100)
                    .ThenBy(v => dfnPriority.TryGetValue(v.dfn, out int i) ? i : 100)
                    .ThenByDescending(v => Convert.ToInt32(v.id))
                    .ThenBy(v => videoAscending ? v.bandwith : -v.bandwith)
                    .ToList()
                : videoTracks
                    .OrderBy(v => dfnPriority.TryGetValue(v.dfn, out int i) ? i : 100)
                    .ThenBy(v => encodingPriority.TryGetValue(v.codecs, out byte i) ? i : 100)
                    .ThenByDescending(v => Convert.ToInt32(v.id))
                    .ThenBy(v => videoAscending ? v.bandwith : -v.bandwith)
                    .ToList();
        }

        private static string FormatSavePath(string savePathFormat, string title, Video? videoTrack, Audio? audioTrack, Page p, int pagesCount, bool tvApi, bool appApi, bool intlApi)
        {
            var result = savePathFormat.Replace('\\', '/');
            var regex = InfoRegex();
            foreach (Match m in regex.Matches(result).Cast<Match>())
            {
                var key = m.Groups[1].Value;
                var v = key switch
                {
                    "videoTitle" => GetValidFileName(title, filterSlash: true).Trim().TrimEnd('.').Trim(),
                    "pageNumber" => p.index.ToString(),
                    "pageNumberWithZero" => p.index.ToString().PadLeft((int)Math.Log10(pagesCount) + 1, '0'),
                    "pageTitle" => GetValidFileName(p.title, filterSlash: true).Trim().TrimEnd('.').Trim(),
                    "aid" => p.aid,
                    "cid" => p.cid,
                    "ownerName" => p.ownerName == null ? "" : GetValidFileName(p.ownerName, filterSlash: true).Trim().TrimEnd('.').Trim(),
                    "ownerMid" => p.ownerMid ?? "",
                    "dfn" => videoTrack == null ? "" : videoTrack.dfn,
                    "res" => videoTrack == null ? "" : videoTrack.res,
                    "fps" => videoTrack == null ? "" : videoTrack.fps,
                    "videoCodecs" => videoTrack == null ? "" : videoTrack.codecs,
                    "videoBandwidth" => videoTrack == null ? "" : videoTrack.bandwith.ToString(),
                    "audioCodecs" => audioTrack == null ? "" : audioTrack.codecs,
                    "audioBandwidth" => audioTrack == null ? "" : audioTrack.bandwith.ToString(),
                    "apiType" => tvApi ? "TV" : (appApi ? "APP" : (intlApi ? "INTL" : "WEB")),
                    _ => $"<{key}>"
                };
                result = result.Replace(m.Value, v);
            }
            if (!result.EndsWith(".mp4")) { result += ".mp4"; }
            return result;
        }

        private static async Task LoginWEB()
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
                QRCodeGenerator qrGenerator = new();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
                PngByteQRCode pngByteCode = new(qrCodeData);
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
                        File.WriteAllText(Path.Combine(APP_DIR, "BBDown.data"), cc[(cc.IndexOf('?') + 1)..].Replace("&", ";"));
                        File.Delete("qrcode.png");
                        break;
                    }
                }
            }
            catch (Exception e) { LogError(e.Message); }
        }

        private static async Task LoginTV()
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
                QRCodeGenerator qrGenerator = new();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
                PngByteQRCode pngByteCode = new(qrCodeData);
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

        [GeneratedRegex("://.*:\\d+/")]
        private static partial Regex PcdnRegex();
        [GeneratedRegex("://.*akamaized\\.net/")]
        private static partial Regex AkamRegex();
        [GeneratedRegex("://[^/]+/")]
        private static partial Regex UposRegex();
        [GeneratedRegex("<(\\w+?)>")]
        private static partial Regex InfoRegex();
    }
}
