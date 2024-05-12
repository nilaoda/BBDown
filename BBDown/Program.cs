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
using static BBDown.BBDownDownloadUtil;
using static BBDown.Core.Parser;
using static BBDown.Core.Logger;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using BBDown.Core;
using BBDown.Core.Util;
using System.Text.Json.Serialization;
using System.CommandLine.Builder;
using BBDown.Core.Entity;

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

        private static string FormatTimeStamp(long ts, string format)
        {
            try
            {
                return ts == 0 ? "null" : DateTimeOffset.FromUnixTimeSeconds(ts).ToLocalTime().ToString(format);
            }
            catch (Exception ex)
            {
                LogError($"格式化日期出错: {ex.Message}");
                return ts.ToString();
            }
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

            var rootCommand = CommandLineInvoker.GetRootCommand(RunApp);
            Command loginCommand = new(
                "login",
                "通过APP扫描二维码以登录您的WEB账号");
            rootCommand.AddCommand(loginCommand);
            Command loginTVCommand = new(
                "logintv",
                "通过APP扫描二维码以登录您的TV账号");
            rootCommand.AddCommand(loginTVCommand);
            var serverUrlOpt = new Option<string>(
                new[] { "--listen", "-l" },
                description: "服务器监听url");
            Command runAsServerCommand = new(
                "serve",
                "以服务器模式运行")
            { serverUrlOpt };
            runAsServerCommand.SetHandler(StartServer, serverUrlOpt);
            rootCommand.AddCommand(runAsServerCommand);
            rootCommand.Description = "BBDown是一个免费且便捷高效的哔哩哔哩下载/解析软件.";
            rootCommand.TreatUnmatchedTokensAsErrors = true;

            //WEB登录
            loginCommand.SetHandler(BBDownLoginUtil.LoginWEB);

            //TV登录
            loginTVCommand.SetHandler(BBDownLoginUtil.LoginTV);

            var parser = new CommandLineBuilder(rootCommand)
                .UseDefaults()
                .EnablePosixBundling(false)
                .UseExceptionHandler((ex, context) =>
                {
                    LogError(ex.Message);
                    try { Console.CursorVisible = true; } catch { }
                    Thread.Sleep(3000);
                    Environment.Exit(1);
                }, 1)
                .Build();

            var newArgsList = new List<string>();
            var commandLineResult = rootCommand.Parse(args);

            //显式抛出异常
            if (commandLineResult.Errors.Any())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(commandLineResult.Errors.First().Message);
                Console.ResetColor();
                Console.Error.WriteLine($"请使用 BBDown --help 查看帮助");
                return 1;
            }

            if (commandLineResult.CommandResult.Command.Name.ToLower() != Path.GetFileNameWithoutExtension(Environment.ProcessPath)!.ToLower() && Path.GetFileNameWithoutExtension(Environment.ProcessPath)!.ToLower() != "dotnet")
            {
                // 服务器模式需要完整的arg列表
                if (commandLineResult.CommandResult.Command.Name.ToLower() == "serve")
                {
                    return await parser.InvokeAsync(args.ToArray());
                }
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

            if (newArgsList.Contains("--debug"))
            {
                Config.DEBUG_LOG = true;
            }

            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;
            var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version!;
            Console.Write($"BBDown version {ver.Major}.{ver.Minor}.{ver.Build}, Bilibili Downloader.\r\n");
            Console.ResetColor();
            Console.Write("遇到问题请首先到以下地址查阅有无相关信息：\r\nhttps://github.com/nilaoda/BBDown/issues\r\n");
            Console.WriteLine();

            //处理配置文件
            BBDownConfigParser.HandleConfig(newArgsList, rootCommand);

            return await parser.InvokeAsync(newArgsList.ToArray());
        }

        private static Task RunApp(MyOption myOption)
        {
            //检测更新
            CheckUpdateAsync();
            return DoWorkAsync(myOption);
        }

        private static void StartServer(string? listenUrl)
        {
            var defaultListenUrl = "http://0.0.0.0:23333";
            //检测更新
            CheckUpdateAsync();
            var server = new BBDownApiServer();
            server.SetUpServer();
            server.Run(string.IsNullOrEmpty(listenUrl) ? defaultListenUrl : listenUrl);
        }

        public static (Dictionary<string, byte> encodingPriority, Dictionary<string, int> dfnPriority, string? firstEncoding,
            bool downloadDanmaku, string input, string savePathFormat, string lang, string aidOri, int delay)
        SetUpWork(MyOption myOption)
        {
            //处理废弃选项
            HandleDeprecatedOptions(myOption);

            //处理冲突选项
            HandleConflictingOptions(myOption);

            //寻找并设置所需的二进制文件路径
            FindBinaries(myOption);

            //切换工作目录
            ChangeWorkingDir(myOption);

            //解析优先级
            var encodingPriority = ParseEncodingPriority(myOption, out var firstEncoding);
            var dfnPriority = ParseDfnPriority(myOption);

            //优先使用用户设置的UA
            HTTPUtil.UserAgent = string.IsNullOrEmpty(myOption.UserAgent) ? HTTPUtil.UserAgent : myOption.UserAgent;

            bool downloadDanmaku = myOption.DownloadDanmaku || myOption.DanmakuOnly;
            string input = myOption.Url;
            string savePathFormat = myOption.FilePattern;
            string lang = myOption.Language;
            string aidOri = ""; //原始aid
            int delay = Convert.ToInt32(myOption.DelayPerPage);
            Config.DEBUG_LOG = myOption.Debug;
            Config.HOST = myOption.Host;
            Config.EPHOST = myOption.EpHost;
            Config.AREA = myOption.Area;
            Config.COOKIE = myOption.Cookie;
            Config.TOKEN = myOption.AccessToken.Replace("access_token=", "");

            LogDebug("AppDirectory: {0}", APP_DIR);
            LogDebug("运行参数：{0}", JsonSerializer.Serialize(myOption, MyOptionJsonContext.Default.MyOption));
            return (encodingPriority, dfnPriority, firstEncoding, downloadDanmaku, input, savePathFormat, lang, aidOri, delay);
        }

        public static async Task<(string fetchedAid, VInfo vInfo, string apiType)> GetVideoInfoAsync(MyOption myOption, string aidOri, string input)
        {
            //加载认证信息
            LoadCredentials(myOption);

            // 检测是否登录了账号
            bool is_login = await CheckLogin(Config.COOKIE);
            if (!myOption.UseIntlApi && !myOption.UseTvApi && Config.AREA == "")
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

            if (string.IsNullOrEmpty(aidOri))
            {
                throw new Exception("输入有误");
            }

            Log("获取视频信息...");
            IFetcher fetcher = FetcherFactory.CreateFetcher(aidOri, myOption.UseIntlApi);
            var vInfo = await fetcher.FetchAsync(aidOri);
            string title = vInfo.Title;
            long pubTime = vInfo.PubTime;
            LogColor("视频标题: " + title);
            if (pubTime != 0)
            {
                Log("发布时间: " + FormatTimeStamp(pubTime, "yyyy-MM-dd HH:mm:ss zzz"));
            }
            var mid = vInfo.PagesInfo.FirstOrDefault(p => !string.IsNullOrEmpty(p.ownerMid))?.ownerMid;
            if (!string.IsNullOrEmpty(mid))
            {
                Log($"UP主页: https://space.bilibili.com/{mid}");
            }
            string apiType = myOption.UseTvApi ? "TV" : (myOption.UseAppApi ? "APP" : (myOption.UseIntlApi ? "INTL" : "WEB"));

            //打印分P信息
            List<Page> pagesInfo = vInfo.PagesInfo;
            bool more = false;
            foreach (Page p in pagesInfo)
            {
                if (!myOption.ShowAll)
                {
                    if (more && p.index != pagesInfo.Count) continue;
                    if (!more && p.index > 5)
                    {
                        Log("......");
                        more = true;
                        continue;
                    }
                }

                Log($"P{p.index}: [{p.cid}] [{p.title}] [{FormatTime(p.dur)}]");
            }
            return (aidOri, vInfo, apiType);
        }

        public static async Task DownloadPagesAsync(MyOption myOption, VInfo vInfo, Dictionary<string, byte> encodingPriority, Dictionary<string, int> dfnPriority,
            string? firstEncoding, bool downloadDanmaku, string input, string savePathFormat, string lang, string aidOri, int delay, string apiType, DownloadTask? relatedTask = null)
        {
            List<Page> pagesInfo = vInfo.PagesInfo;
            bool bangumi = vInfo.IsBangumi;
            bool cheese = vInfo.IsCheese;
            //获取已选择的分P列表
            List<string>? selectedPages = GetSelectedPages(myOption, vInfo, input);

            Log($"共计 {pagesInfo.Count} 个分P, 已选择：" + (selectedPages == null ? "ALL" : string.Join(",", selectedPages)));
            var pagesCount = pagesInfo.Count;

            //过滤不需要的分P
            if (selectedPages != null)
            {
                pagesInfo = pagesInfo.Where(p => selectedPages.Contains(p.index.ToString())).ToList();
            }

            // 根据p数选择存储路径
            savePathFormat = string.IsNullOrEmpty(myOption.FilePattern) ? SinglePageDefaultSavePath : myOption.FilePattern;
            // 1. 多P; 2. 只有1P, 但是是番剧, 尚未完结时 按照多P处理
            if (pagesCount > 1 || (bangumi && !vInfo.IsBangumiEnd))
            {
                savePathFormat = string.IsNullOrEmpty(myOption.MultiFilePattern) ? MultiPageDefaultSavePath : myOption.MultiFilePattern;
            }

            foreach (Page p in pagesInfo)
            {
                if (pagesInfo.Count > 1 && delay > 0)
                {
                    Log($"停顿{delay}秒...");
                    await Task.Delay(delay * 1000);
                }
                Log($"开始解析P{p.index}: {p.aid}... ({pagesInfo.IndexOf(p) + 1} of {pagesInfo.Count})");

                if (myOption.SaveArchivesToFile)
                {
                    if (CheckAidFromFile(p.aid))
                    {

                        Log($"aid: {p.aid}已下载过, 跳过下载...");
                        continue;
                    }
                }

                await DownloadPageAsync(p, myOption, vInfo, pagesInfo, encodingPriority, dfnPriority, firstEncoding,
                    downloadDanmaku, input, savePathFormat, lang, aidOri, apiType, relatedTask);

                if (myOption.SaveArchivesToFile)
                {
                    SaveAidToFile(p.aid);
                }
            }

            Log("任务完成");
        }

        private static async Task DownloadPageAsync(Page p, MyOption myOption, VInfo vInfo, List<Page> selectedPagesInfo, Dictionary<string, byte> encodingPriority, Dictionary<string, int> dfnPriority,
            string? firstEncoding, bool downloadDanmaku, string input, string savePathFormat, string lang, string aidOri, string apiType, DownloadTask? relatedTask = null)
        {
            string desc = string.IsNullOrEmpty(p.desc) ? vInfo.Desc : p.desc;
            bool bangumi = vInfo.IsBangumi;
            var pagesCount = selectedPagesInfo.Count;
            List<Subtitle> subtitleInfo = new();
            string title = vInfo.Title;
            string pic = vInfo.Pic;
            long pubTime = vInfo.PubTime;
            bool selected = false; //用户是否已经手动选择过了轨道
            int retryCount = 0;
        downloadPage:
            try
            {
                LogDebug("尝试获取章节信息...");
                p.points = await FetchPointsAsync(p.cid, p.aid);

                string videoPath = $"{p.aid}/{p.aid}.P{p.index}.{p.cid}.mp4";
                string audioPath = $"{p.aid}/{p.aid}.P{p.index}.{p.cid}.m4a";
                var coverPath = $"{p.aid}/{p.aid}.jpg";

                //处理文件夹以.结尾导致的异常情况
                if (title.EndsWith(".")) title += "_fix";
                //处理文件夹以.开头导致的异常情况
                if (title.StartsWith(".")) title = "_" + title;

                //处理封面&&字幕
                if (!myOption.OnlyShowInfo)
                {
                    if (!Directory.Exists(p.aid))
                    {
                        Directory.CreateDirectory(p.aid);
                    }
                    if (!myOption.SkipCover && !myOption.SubOnly && !File.Exists(coverPath) && !myOption.DanmakuOnly && !myOption.CoverOnly)
                    {
                        await DownloadFile((pic == "" ? p.cover! : pic), coverPath, new DownloadConfig());
                    }

                    if (!myOption.SkipSubtitle && !myOption.DanmakuOnly && !myOption.CoverOnly)
                    {
                        LogDebug("获取字幕...");
                        subtitleInfo = await SubUtil.GetSubtitlesAsync(p.aid, p.cid, p.epid, p.index, myOption.UseIntlApi);
                        if (myOption.SkipAi && subtitleInfo.Any())
                        {
                            Log($"跳过下载AI字幕");
                            subtitleInfo = subtitleInfo.Where(s => !s.lan.StartsWith("ai-")).ToList();
                        }
                        foreach (Subtitle s in subtitleInfo)
                        {
                            Log($"下载字幕 {s.lan} => {SubUtil.GetSubtitleCode(s.lan).Item2}...");
                            LogDebug("下载：{0}", s.url);
                            await SubUtil.SaveSubtitleAsync(s.url, s.path);
                            if (myOption.SubOnly && File.Exists(s.path) && File.ReadAllText(s.path) != "")
                            {
                                var _outSubPath = FormatSavePath(savePathFormat, title, null, null, p, pagesCount, apiType, pubTime);
                                if (_outSubPath.Contains('/'))
                                {
                                    if (!Directory.Exists(_outSubPath.Split('/').First()))
                                        Directory.CreateDirectory(_outSubPath.Split('/').First());
                                }
                                _outSubPath = Path.ChangeExtension(_outSubPath, $".{s.lan}.srt");
                                File.Move(s.path, _outSubPath, true);
                            }
                        }
                    }

                    if (myOption.SubOnly)
                    {
                        if (Directory.Exists(p.aid) && Directory.GetFiles(p.aid).Length == 0) Directory.Delete(p.aid, true);
                        return;
                    }
                }

                //调用解析
                ParsedResult parsedResult = await ExtractTracksAsync(aidOri, p.aid, p.cid, p.epid, myOption.UseTvApi, myOption.UseIntlApi, myOption.UseAppApi, firstEncoding);
                List<AudioMaterial> audioMaterial = new();
                if (!p.points.Any())
                {
                    p.points = parsedResult.ExtraPoints;
                }

                if (Config.DEBUG_LOG)
                {
                    File.WriteAllText($"debug_{DateTime.Now:yyyyMMddHHmmssfff}.json", parsedResult.WebJsonString);
                }

                var savePath = "";

                var downloadConfig = new DownloadConfig()
                {
                    UseAria2c = myOption.UseAria2c,
                    Aria2cArgs = myOption.Aria2cArgs,
                    ForceHttp = myOption.ForceHttp,
                    MultiThread = myOption.MultiThread,
                    RelatedTask = relatedTask,
                };

                //此处代码简直灾难, 后续优化吧
                if ((parsedResult.VideoTracks.Any() || parsedResult.AudioTracks.Any()) && !parsedResult.Clips.Any())   //dash
                {
                    if (parsedResult.VideoTracks.Count == 0)
                    {
                        LogWarn("没有找到符合要求的视频流");
                        if (myOption.VideoOnly) return;
                    }
                    if (parsedResult.AudioTracks.Count == 0)
                    {
                        LogWarn("没有找到符合要求的音频流");
                        if (myOption.AudioOnly) return;
                    }

                    if (myOption.AudioOnly)
                    {
                        parsedResult.VideoTracks.Clear();
                    }
                    if (myOption.VideoOnly)
                    {
                        parsedResult.AudioTracks.Clear();
                        parsedResult.BackgroundAudioTracks.Clear();
                        parsedResult.RoleAudioList.Clear();
                    }

                    //排序
                    parsedResult.VideoTracks = SortTracks(parsedResult.VideoTracks, dfnPriority, encodingPriority, myOption.VideoAscending);
                    parsedResult.AudioTracks.Sort(Compare);
                    parsedResult.BackgroundAudioTracks.Sort(Compare);
                    foreach (var role in parsedResult.RoleAudioList)
                    {
                        role.audio.Sort(Compare);
                    }
                    if (myOption.AudioAscending)
                    {
                        parsedResult.AudioTracks.Reverse();
                        parsedResult.BackgroundAudioTracks.Reverse();
                        foreach (var role in parsedResult.RoleAudioList)
                        {
                            role.audio.Reverse();
                        }
                    }

                    //打印轨道信息
                    if (!myOption.HideStreams)
                    {
                        PrintAllTracksInfo(parsedResult, p.dur, myOption.OnlyShowInfo);
                    }

                    //仅展示 跳过下载
                    if (myOption.OnlyShowInfo)
                    {
                        return;
                    }

                    int vIndex = 0; //用户手动选择的视频序号
                    int aIndex = 0; //用户手动选择的音频序号

                    //选择轨道
                    if (myOption.Interactive && !selected)
                    {
                        SelectTrackManually(parsedResult, ref vIndex, ref aIndex);
                        selected = true;
                    }

                    Video? selectedVideo = parsedResult.VideoTracks.ElementAtOrDefault(vIndex);
                    Audio? selectedAudio = parsedResult.AudioTracks.ElementAtOrDefault(aIndex);
                    Audio? selectedBackgroundAudio = parsedResult.BackgroundAudioTracks.ElementAtOrDefault(aIndex);

                    LogDebug("Format Before: " + savePathFormat);
                    savePath = FormatSavePath(savePathFormat, title, selectedVideo, selectedAudio, p, pagesCount, apiType, pubTime);
                    LogDebug("Format After: " + savePath);

                    if (downloadDanmaku)
                    {
                        var danmakuXmlPath = Path.ChangeExtension(savePath, ".xml");
                        var danmakuAssPath = Path.ChangeExtension(savePath, ".ass");
                        Log("正在下载弹幕Xml文件");
                        string danmakuUrl = $"https://comment.bilibili.com/{p.cid}.xml";
                        await DownloadFile(danmakuUrl, danmakuXmlPath, downloadConfig);
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
                        if (myOption.DanmakuOnly)
                        {
                            if (Directory.Exists(p.aid))
                            {
                                Directory.Delete(p.aid);
                            }
                            return;
                        }
                    }

                    if (myOption.CoverOnly)
                    {
                        var coverUrl = pic == "" ? p.cover! : pic;
                        var newCoverPath = Path.ChangeExtension(savePath, Path.GetExtension(coverUrl));
                        await DownloadFile(coverUrl, newCoverPath, downloadConfig);
                        if (Directory.Exists(p.aid) && Directory.GetFiles(p.aid).Length == 0) Directory.Delete(p.aid, true);
                        return;
                    }

                    Log($"已选择的流:");
                    PrintSelectedTrackInfo(selectedVideo, selectedAudio, p.dur);

                    //用户开启了强制替换
                    if (myOption.ForceReplaceHost && string.IsNullOrEmpty(myOption.UposHost))
                    {
                        myOption.UposHost = BACKUP_HOST;
                    }

                    //处理PCDN
                    HandlePcdn(myOption, selectedVideo, selectedAudio);

                    if (!myOption.OnlyShowInfo && File.Exists(savePath) && new FileInfo(savePath).Length != 0)
                    {
                        Log($"{savePath}已存在, 跳过下载...");
                        File.Delete(coverPath);
                        if (Directory.Exists(p.aid) && Directory.GetFiles(p.aid).Length == 0)
                        {
                            Directory.Delete(p.aid, true);
                        }
                        return;
                    }

                    if (selectedVideo != null)
                    {
                        //杜比视界, 若ffmpeg版本小于5.0, 使用mp4box封装
                        if (selectedVideo.dfn == Config.qualitys["126"] && !myOption.UseMP4box && !CheckFFmpegDOVI())
                        {
                            LogWarn($"检测到杜比视界清晰度且您的ffmpeg版本小于5.0,将使用mp4box混流...");
                            myOption.UseMP4box = true;
                        }
                        Log($"开始下载P{p.index}视频...");
                        await DownloadTrackAsync(selectedVideo.baseUrl, videoPath, downloadConfig, video: true);
                    }

                    if (selectedAudio != null)
                    {
                        Log($"开始下载P{p.index}音频...");
                        await DownloadTrackAsync(selectedAudio.baseUrl, audioPath, downloadConfig, video: false);
                    }

                    if (selectedBackgroundAudio != null)
                    {
                        var backgroundPath = $"{p.aid}/{p.aid}.{p.cid}.P{p.index}.back_ground.m4a";
                        Log($"开始下载P{p.index}背景配音...");
                        await DownloadTrackAsync(selectedBackgroundAudio.baseUrl, backgroundPath, downloadConfig, video: false);
                        audioMaterial.Add(new AudioMaterial("背景音频", "", backgroundPath));
                    }

                    if (parsedResult.RoleAudioList.Any())
                    {
                        foreach (var role in parsedResult.RoleAudioList)
                        {
                            Log($"开始下载P{p.index}配音[{role.title}]...");
                            await DownloadTrackAsync(role.audio[aIndex].baseUrl, role.path, downloadConfig, video: false);
                            audioMaterial.Add(new AudioMaterial(role));
                        }
                    }

                    Log($"下载P{p.index}完毕");
                    if (!parsedResult.VideoTracks.Any()) videoPath = "";
                    if (!parsedResult.AudioTracks.Any()) audioPath = "";
                    if (myOption.SkipMux) return;
                    Log($"开始合并音视频{(subtitleInfo.Any() ? "和字幕" : "")}...");
                    if (myOption.AudioOnly)
                        savePath = savePath[..^4] + ".m4a";
                    int code = BBDownMuxer.MuxAV(myOption.UseMP4box, videoPath, audioPath, audioMaterial, savePath,
                        desc,
                        title,
                        p.ownerName ?? "",
                        (pagesCount > 1 || (bangumi && !vInfo.IsBangumiEnd)) ? p.title : "",
                        File.Exists(coverPath) ? coverPath : "",
                        lang,
                        subtitleInfo, myOption.AudioOnly, myOption.VideoOnly, p.points, p.pubTime, myOption.SimplyMux);
                    if (code != 0 || !File.Exists(savePath) || new FileInfo(savePath).Length == 0)
                    {
                        LogError("合并失败"); return;
                    }
                    Log("清理临时文件...");
                    Thread.Sleep(200);
                    if (parsedResult.VideoTracks.Any()) File.Delete(videoPath);
                    if (parsedResult.AudioTracks.Any()) File.Delete(audioPath);
                    if (p.points.Any()) File.Delete(Path.Combine(Path.GetDirectoryName(string.IsNullOrEmpty(videoPath) ? audioPath : videoPath)!, "chapters"));
                    foreach (var s in subtitleInfo) File.Delete(s.path);
                    foreach (var a in audioMaterial) File.Delete(a.path);
                    if (selectedPagesInfo.Count == 1 || p.index == selectedPagesInfo.Last().index || p.aid != selectedPagesInfo.Last().aid)
                        File.Delete(coverPath);
                    if (Directory.Exists(p.aid) && Directory.GetFiles(p.aid).Length == 0) Directory.Delete(p.aid, true);
                }
                else if (parsedResult.Clips.Any() && parsedResult.Dfns.Any())   //flv
                {
                    bool flag = false;
                    var clips = parsedResult.Clips;
                    var dfns = parsedResult.Dfns;
                reParse:
                    //排序
                    parsedResult.VideoTracks = SortTracks(parsedResult.VideoTracks, dfnPriority, encodingPriority, myOption.VideoAscending);

                    int vIndex = 0;
                    if (myOption.Interactive && !flag && !selected)
                    {
                        int i = 0;
                        dfns.ForEach(key => LogColor($"{i++}.{Config.qualitys[key]}"));
                        Log("请选择最想要的清晰度(输入序号): ", false);
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        vIndex = Convert.ToInt32(Console.ReadLine());
                        if (vIndex > dfns.Count || vIndex < 0) vIndex = 0;
                        Console.ResetColor();
                        //重新解析
                        parsedResult.VideoTracks.Clear();
                        parsedResult = await ExtractTracksAsync(aidOri, p.aid, p.cid, p.epid, myOption.UseTvApi, myOption.UseIntlApi, myOption.UseAppApi, firstEncoding, dfns[vIndex]);
                        if (!p.points.Any()) p.points = parsedResult.ExtraPoints;
                        flag = true;
                        selected = true;
                        goto reParse;
                    }

                    Log($"共计{parsedResult.VideoTracks.Count}条流(共有{clips.Count}个分段).");
                    int index = 0;
                    foreach (var v in parsedResult.VideoTracks)
                    {
                        LogColor($"{index++}. [{v.dfn}] [{v.res}] [{v.codecs}] [{v.fps}] [~{v.size / 1024 / v.dur * 8:00} kbps] [{FormatFileSize(v.size)}]".Replace("[] ", ""), false);
                        if (myOption.OnlyShowInfo)
                        {
                            clips.ForEach(Console.WriteLine);
                        }
                    }
                    if (myOption.OnlyShowInfo) return;
                    savePath = FormatSavePath(savePathFormat, title, parsedResult.VideoTracks.ElementAtOrDefault(vIndex), null, p, pagesCount, apiType, pubTime);
                    if (File.Exists(savePath) && new FileInfo(savePath).Length != 0)
                    {
                        Log($"{savePath}已存在, 跳过下载...");
                        if (selectedPagesInfo.Count == 1 && Directory.Exists(p.aid))
                        {
                            Directory.Delete(p.aid, true);
                        }
                        return;
                    }
                    var pad = string.Empty.PadRight(clips.Count.ToString().Length, '0');
                    for (int i = 0; i < clips.Count; i++)
                    {
                        var link = clips[i];
                        videoPath = $"{p.aid}/{p.aid}.P{p.index}.{p.cid}.{i.ToString(pad)}.mp4";
                        Log($"开始下载P{p.index}视频, 片段({(i + 1).ToString(pad)}/{clips.Count})...");
                        await DownloadTrackAsync(link, videoPath, downloadConfig, video: true);
                    }
                    Log($"下载P{p.index}完毕");
                    Log("开始合并分段...");
                    var files = GetFiles(Path.GetDirectoryName(videoPath)!, ".mp4");
                    videoPath = $"{p.aid}/{p.aid}.P{p.index}.{p.cid}.mp4";
                    BBDownMuxer.MergeFLV(files, videoPath);
                    if (myOption.SkipMux) return;
                    Log($"开始混流视频{(subtitleInfo.Any() ? "和字幕" : "")}...");
                    if (myOption.AudioOnly)
                        savePath = savePath[..^4] + ".m4a";
                    int code = BBDownMuxer.MuxAV(false, videoPath, "", audioMaterial, savePath,
                        desc,
                        title,
                        p.ownerName ?? "",
                        (pagesCount > 1 || (bangumi && !vInfo.IsBangumiEnd)) ? p.title : "",
                        File.Exists(coverPath) ? coverPath : "",
                        lang,
                        subtitleInfo, myOption.AudioOnly, myOption.VideoOnly, p.points, p.pubTime, myOption.SimplyMux);
                    if (code != 0 || !File.Exists(savePath) || new FileInfo(savePath).Length == 0)
                    {
                        LogError("合并失败"); return;
                    }
                    Log("清理临时文件...");
                    Thread.Sleep(200);
                    if (parsedResult.VideoTracks.Count != 0) File.Delete(videoPath);
                    foreach (var s in subtitleInfo) File.Delete(s.path);
                    foreach (var a in audioMaterial) File.Delete(a.path);
                    if (p.points.Any()) File.Delete(Path.Combine(Path.GetDirectoryName(string.IsNullOrEmpty(videoPath) ? audioPath : videoPath)!, "chapters"));
                    if (selectedPagesInfo.Count == 1 || p.index == selectedPagesInfo.Last().index || p.aid != selectedPagesInfo.Last().aid)
                        File.Delete(coverPath);
                    if (Directory.Exists(p.aid) && Directory.GetFiles(p.aid).Length == 0) Directory.Delete(p.aid, true);
                }
                else
                {
                    LogError("解析此分P失败(建议--debug查看详细信息)");
                    if (parsedResult.WebJsonString.Length < 100)
                    {
                        LogError(parsedResult.WebJsonString);
                    }
                    LogDebug("{0}", parsedResult.WebJsonString);
                    return;
                }
            }
            catch (Exception ex)
            {
                if (++retryCount > 2) throw;
                LogError(ex.Message);
                LogWarn("下载出现异常, 3秒后将进行自动重试...");
                await Task.Delay(3000);
                goto downloadPage;
            }
        }

        private static async Task DoWorkAsync(MyOption myOption)
        {
            try
            {
                var (encodingPriority, dfnPriority, firstEncoding, downloadDanmaku,
                    input, savePathFormat, lang, aidOri, delay) = SetUpWork(myOption);
                var (fetchedAid, vInfo, apiType) = await GetVideoInfoAsync(myOption, aidOri, input);
                await DownloadPagesAsync(myOption, vInfo, encodingPriority, dfnPriority, firstEncoding, downloadDanmaku,
                    input, savePathFormat, lang, fetchedAid, delay, apiType);
            }
            catch (Exception e)
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                var msg = Config.DEBUG_LOG ? e.ToString() : e.Message;
                Console.Write($"{msg}{Environment.NewLine}请尝试升级到最新版本后重试!");
                Console.ResetColor();
                Console.WriteLine();
                Thread.Sleep(1);
                Environment.Exit(1);
            }
        }

        private static List<Video> SortTracks(List<Video> videoTracks, Dictionary<string, int> dfnPriority, Dictionary<string, byte> encodingPriority, bool videoAscending)
        {
            //用户同时输入了自定义分辨率优先级和自定义编码优先级, 则根据输入顺序依次进行排序
            return dfnPriority.Any() && encodingPriority.Any() && Environment.CommandLine.IndexOf("--encoding-priority") < Environment.CommandLine.IndexOf("--dfn-priority")
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

        private static string FormatSavePath(string savePathFormat, string title, Video? videoTrack, Audio? audioTrack, Page p, int pagesCount, string apiType, long pubTime)
        {
            var result = savePathFormat.Replace('\\', '/');
            var regex = InfoRegex();
            foreach (Match m in regex.Matches(result).Cast<Match>())
            {
                var key = m.Groups[1].Value;

                //解析自定义日期格式
                var defaultDateFormat = "yyyy-MM-dd_HH-mm-ss";
                string[] prefixes = { "publishDate:", "videoDate:" };
                foreach (var prefix in prefixes)
                {
                    if (key.StartsWith(prefix))
                    {
                        defaultDateFormat = key.Substring(key.IndexOf(':') + 1);
                        key = prefix.Replace(":", "");
                        break;
                    }
                }

                var v = key switch
                {
                    "videoTitle" => GetValidFileName(title, filterSlash: true).Trim().TrimEnd('.').Trim(),
                    "pageNumber" => p.index.ToString(),
                    "pageNumberWithZero" => p.index.ToString().PadLeft(pagesCount.ToString().Length, '0'),
                    "pageTitle" => GetValidFileName(p.title, filterSlash: true).Trim().TrimEnd('.').Trim(),
                    "bvid" => p.bvid,
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
                    "publishDate" => FormatTimeStamp(pubTime, defaultDateFormat),
                    "videoDate" => FormatTimeStamp(p.pubTime, defaultDateFormat),
                    "apiType" => apiType,
                    _ => $"<{key}>"
                };
                result = result.Replace(m.Value, v);
            }
            if (!result.EndsWith(".mp4")) { result += ".mp4"; }
            return result;
        }

        [GeneratedRegex("<([\\w:\\-.]+?)>")]
        private static partial Regex InfoRegex();
    }
}
