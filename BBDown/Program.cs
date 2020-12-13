using Newtonsoft.Json.Linq;
using QRCoder;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using static BBDown.BBDownEntity;
using static BBDown.BBDownUtil;
using static BBDown.BBDownParser;
using static BBDown.BBDownLogger;
using static BBDown.BBDownMuxer;
using System.Text;
using System.Linq;

namespace BBDown
{
    class Program
    {
        public static string COOKIE { get; set; } = "";
        public static string TOKEN { get; set; } = "";

        public static Dictionary<string, string> qualitys = new Dictionary<string, string>() {
            {"125","HDR 真彩" }, {"120","4K 超清" }, {"116","1080P 高帧率" },
            {"112","1080P 高码率" }, {"80","1080P 高清" }, {"74","720P 高帧率" },
            {"64","720P 高清" }, {"48","720P 高清" }, {"32","480P 清晰" }, {"16","360P 流畅" }
        };

        private static int Compare(Video r1, Video r2)
        {
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
            public bool UseIntlApi { get; set; }
            public bool OnlyHevc { get; set; }
            public bool OnlyShowInfo { get; set; }
            public bool ShowAll { get; set; }
            public bool UseAria2c { get; set; }
            public bool Interactive { get; set; }
            public bool HideStreams { get; set; }
            public bool MultiThread { get; set; }
            public bool VideoOnly { get; set; }
            public bool AudioOnly { get; set; }
            public bool Debug { get; set; }
            public bool SkipMux { get; set; }
            public string SelectPage { get; set; } = "";
            public string Cookie { get; set; } = "";
            public string AccessToken { get; set; } = "";

            public override string ToString()
            {
                return $"{{Input={Url}, {nameof(UseTvApi)}={UseTvApi.ToString()}, " +
                    $"{nameof(UseIntlApi)}={UseIntlApi.ToString()}, " +
                    $"{nameof(OnlyHevc)}={OnlyHevc.ToString()}, " +
                    $"{nameof(OnlyShowInfo)}={OnlyShowInfo.ToString()}, " +
                    $"{nameof(Interactive)}={Interactive.ToString()}, " +
                    $"{nameof(HideStreams)}={HideStreams.ToString()}, " +
                    $"{nameof(ShowAll)}={ShowAll.ToString()}, " +
                    $"{nameof(UseAria2c)}={UseAria2c.ToString()}, " +
                    $"{nameof(MultiThread)}={MultiThread.ToString()}, " +
                    $"{nameof(VideoOnly)}={VideoOnly.ToString()}, " +
                    $"{nameof(AudioOnly)}={AudioOnly.ToString()}, " +
                    $"{nameof(Debug)}={Debug.ToString()}, " +
                    $"{nameof(SelectPage)}={SelectPage}, " +
                    $"{nameof(Cookie)}={Cookie}, " +
                    $"{nameof(AccessToken)}={AccessToken}}}";
            }
        }

        public static int Main(params string[] args)
        {
            ServicePointManager.DefaultConnectionLimit = 2048;
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) =>
            {
                return true;
            };

            var rootCommand = new RootCommand
            {
                new Argument<string>(
                    "url",
                    description: "视频地址 或 av|bv|BV|ep|ss"),
                new Option<bool>(
                    new string[]{ "--use-tv-api" ,"-tv"},
                    "使用TV端解析模式"),
                new Option<bool>(
                    new string[]{ "--use-intl-api" ,"-intl"},
                    "使用国际版解析模式"),
                new Option<bool>(
                    new string[]{ "--only-hevc" ,"-hevc"},
                    "只下载hevc编码"),
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
                new Option<bool>(
                    new string[]{ "--multi-thread", "-mt"},
                    "使用多线程下载"),
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
                    new string[]{ "--debug"},
                    "输出调试日志"),
                new Option<bool>(
                    new string[]{ "--skip-mux"},
                    "跳过混流步骤"),
                new Option<string>(
                    new string[]{ "--cookie" ,"-c"},
                    "设置字符串cookie用以下载网页接口的会员内容"),
                new Option<string>(
                    new string[]{ "--access-token" ,"-a"},
                    "设置access_token用以下载TV接口的会员内容")
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
            loginCommand.Handler = CommandHandler.Create(delegate
            {
                try
                {
                    Log("获取登录地址...");
                    string loginUrl = "https://passport.bilibili.com/qrcode/getLoginUrl";
                    string url = JObject.Parse(GetWebSource(loginUrl))["data"]["url"].ToString();
                    string oauthKey = GetQueryString("oauthKey", url);
                    //Log(oauthKey);
                    //Log(url);
                    bool flag = false;
                    Log("生成二维码...");
                    QRCodeGenerator qrGenerator = new QRCodeGenerator();
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
                    QRCode qrCode = new QRCode(qrCodeData);
                    Bitmap qrCodeImage = qrCode.GetGraphic(7);
                    qrCodeImage.Save("qrcode.png", System.Drawing.Imaging.ImageFormat.Png);
                    Log("生成二维码成功：qrcode.png, 请打开并扫描");
                    while (true)
                    {
                        Thread.Sleep(1000);
                        string w = GetLoginStatus(oauthKey);
                        string data = JObject.Parse(w)["data"].ToString();
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
                            string cc = JObject.Parse(w)["data"]["url"].ToString();
                            Log("登录成功: SESSDATA=" + GetQueryString("SESSDATA", cc));
                            //导出cookie
                            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "BBDown.data"), "SESSDATA=" + GetQueryString("SESSDATA", cc));
                            File.Delete("qrcode.png");
                            break;
                        }
                    }
                }
                catch (Exception e) { LogError(e.Message); }
            });

            //TV登录
            loginTVCommand.Handler = CommandHandler.Create(delegate
            {
                try
                {
                    string loginUrl = "https://passport.snm0516.aisee.tv/x/passport-tv-login/qrcode/auth_code";
                    string pollUrl = "https://passport.bilibili.com/x/passport-tv-login/qrcode/poll";
                    var parms = GetTVLoginParms();
                    Log("获取登录地址...");
                    WebClient webClient = new WebClient();
                    byte[] responseArray = webClient.UploadValues(loginUrl, parms);
                    string web = Encoding.UTF8.GetString(responseArray);
                    string url = JObject.Parse(web)["data"]["url"].ToString();
                    string authCode = JObject.Parse(web)["data"]["auth_code"].ToString();
                    Log("生成二维码...");
                    QRCodeGenerator qrGenerator = new QRCodeGenerator();
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
                    QRCode qrCode = new QRCode(qrCodeData);
                    Bitmap qrCodeImage = qrCode.GetGraphic(7);
                    qrCodeImage.Save("qrcode.png", System.Drawing.Imaging.ImageFormat.Png);
                    Log("生成二维码成功：qrcode.png, 请打开并扫描");
                    parms.Set("auth_code", authCode);
                    parms.Set("ts", GetTimeStamp(true));
                    parms.Remove("sign");
                    parms.Add("sign", GetSign(ToQueryString(parms)));
                    while (true)
                    {
                        Thread.Sleep(1000);
                        responseArray = webClient.UploadValues(pollUrl, parms);
                        web = Encoding.UTF8.GetString(responseArray);
                        string code = JObject.Parse(web)["code"].ToString();
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
                            string cc = JObject.Parse(web)["data"]["access_token"].ToString();
                            Log("登录成功: AccessToken=" + cc);
                            //导出cookie
                            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "BBDownTV.data"), "access_token=" + cc);
                            File.Delete("qrcode.png");
                            break;
                        }
                    }
                }
                catch (Exception e) { LogError(e.Message); }
            });

            rootCommand.Handler = CommandHandler.Create<MyOption>(async (myOption) =>
            {
                await DoWorkAsync(myOption);
            });

            return rootCommand.InvokeAsync(args).Result;
        }

        private static async Task DoWorkAsync(MyOption myOption)
        {
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;
            var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            Console.Write($"BBDown version {ver.Major}.{ver.Minor}.{ver.Build}, Bilibili Downloader.\r\n");
            Console.ResetColor();
            Console.Write("欢迎到讨论区交流：\r\n" +
                "https://github.com/nilaoda/BBDown/discussions\r\n");
            Console.WriteLine();
            //检测更新
            new Thread(async () =>
            {
                await CheckUpdateAsync();
            }).Start();
            try
            {
                bool interactMode = myOption.Interactive;
                bool infoMode = myOption.OnlyShowInfo;
                bool tvApi = myOption.UseTvApi;
                bool intlApi = myOption.UseIntlApi;
                bool hevc = myOption.OnlyHevc;
                bool hideStreams = myOption.HideStreams;
                bool multiThread = myOption.MultiThread;
                bool audioOnly = myOption.AudioOnly;
                bool videoOnly = myOption.VideoOnly;
                bool skipMux = myOption.SkipMux;
                bool showAll = myOption.ShowAll;
                bool useAria2c = myOption.UseAria2c;
                DEBUG_LOG = myOption.Debug;
                string input = myOption.Url;
                string selectPage = myOption.SelectPage.ToUpper();
                string aidOri = ""; //原始aid
                COOKIE = myOption.Cookie;
                TOKEN = myOption.AccessToken.Replace("access_token=", "");

                //audioOnly和videoOnly同时开启则全部忽视
                if (audioOnly && videoOnly)
                {
                    audioOnly = false;
                    videoOnly = false;
                }

                List<string> selectedPages = null;
                if (!string.IsNullOrEmpty(GetQueryString("p", input)))
                {
                    selectedPages = new List<string>();
                    selectedPages.Add(GetQueryString("p", input));
                }

                LogDebug("运行参数：{0}", myOption);
                if (string.IsNullOrEmpty(COOKIE) && File.Exists(Path.Combine(AppContext.BaseDirectory, "BBDown.data")) && !tvApi)
                {
                    Log("加载本地cookie...");
                    LogDebug("文件路径：{0}", Path.Combine(AppContext.BaseDirectory, "BBDown.data"));
                    COOKIE = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "BBDown.data"));
                }
                if (string.IsNullOrEmpty(TOKEN) && File.Exists(Path.Combine(AppContext.BaseDirectory, "BBDownTV.data")) && tvApi)
                {
                    Log("加载本地token...");
                    LogDebug("文件路径：{0}", Path.Combine(AppContext.BaseDirectory, "BBDownTV.data"));
                    TOKEN = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "BBDownTV.data"));
                    TOKEN = TOKEN.Replace("access_token=", "");
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
                IFetcher fetcher = new BBDownNormalInfoFetcher();
                if (aidOri.StartsWith("cheese"))
                {
                    fetcher = new BBDownCheeseInfoFetcher();
                }
                else if (aidOri.StartsWith("ep"))
                {
                    fetcher = new BBDownBangumiInfoFetcher();
                }
                else if (aidOri.StartsWith("mid"))
                {
                    fetcher = new BBDownSpaceVideoFetcher();
                }
                var vInfo = fetcher.Fetch(aidOri);
                string title = vInfo.Title;
                string desc = vInfo.Desc;
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

                //如果用户没有选择分P，根据epid来确定某一集
                if (selectedPages == null && selectPage != "ALL" && !string.IsNullOrEmpty(vInfo.Index))
                {
                    selectedPages = new List<string> { vInfo.Index };
                    Log("程序已自动选择你输入的集数，如果要下载其他集数请自行指定分P(如可使用-p ALL代表全部)");
                }

                Log($"共计 {pagesInfo.Count} 个分P, 已选择：" + (selectedPages == null ? "ALL" : string.Join(",", selectedPages)));

                //过滤不需要的分P
                if (selectedPages != null)
                    pagesInfo = pagesInfo.Where(p => selectedPages.Contains(p.index.ToString())).ToList();

                foreach (Page p in pagesInfo)
                {
                    Log($"开始解析P{p.index}...");
                    if (!infoMode)
                    {
                        if (!Directory.Exists(p.aid))
                        {
                            Directory.CreateDirectory(p.aid);
                        }
                        if (!File.Exists($"{p.aid}/{p.aid}.jpg"))
                        {
                            Log("下载封面...");
                            LogDebug("下载：{0}", pic);
                            new WebClient().DownloadFile(pic, $"{p.aid}/{p.aid}.jpg");
                        }
                        LogDebug("获取字幕...");
                        subtitleInfo = BBDownSubUtil.GetSubtitles(p.aid, p.cid, p.epid, intlApi);
                        foreach (Subtitle s in subtitleInfo)
                        {
                            Log($"下载字幕 {s.lan} => {BBDownSubUtil.SubDescDic[s.lan]}...");
                            LogDebug("下载：{0}", s.url);
                            BBDownSubUtil.SaveSubtitle(s.url, s.path);
                        }
                    }

                    string webJsonStr = "";
                    List<Video> videoTracks = new List<Video>();
                    List<Audio> audioTracks = new List<Audio>();
                    List<string> clips = new List<string>();
                    List<string> dfns = new List<string>();

                    string indexStr = p.index.ToString("0".PadRight(pagesInfo.OrderByDescending(_p => _p.index).First().index.ToString().Length, '0'));
                    string videoPath = $"{p.aid}/{p.aid}.P{indexStr}.{p.cid}.mp4";
                    string audioPath = $"{p.aid}/{p.aid}.P{indexStr}.{p.cid}.m4a";
                    //处理文件夹以.结尾导致的异常情况
                    if (title.EndsWith(".")) title += "_fix";
                    string outPath = GetValidFileName(title) + (pagesInfo.Count > 1 ? $"/[P{indexStr}]{GetValidFileName(p.title)}" : (vInfo.PagesInfo.Count > 1 ? $"[P{indexStr}]{GetValidFileName(p.title)}" : "")) + ".mp4";
                    
                    //调用解析
                    (webJsonStr, videoTracks, audioTracks, clips, dfns) = ExtractTracks(hevc, aidOri, p.aid, p.cid, p.epid, tvApi, intlApi);

                    //File.WriteAllText($"debug.json", JObject.Parse(webJson).ToString());
                    JObject respJson = JObject.Parse(webJsonStr);


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
                        //降序
                        videoTracks.Sort(Compare);
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
                        if (File.Exists(outPath) && new FileInfo(outPath).Length != 0)
                        {
                            Log($"{outPath}已存在, 跳过下载...");
                            continue;
                        }

                        Log($"已选择的流:");
                        if (videoTracks.Count > 0)
                            LogColor($"[视频] [{videoTracks[vIndex].dfn}] [{videoTracks[vIndex].res}] [{videoTracks[vIndex].codecs}] [{videoTracks[vIndex].fps}] [{videoTracks[vIndex].bandwith} kbps] [~{FormatFileSize(videoTracks[vIndex].dur * videoTracks[vIndex].bandwith * 1024 / 8)}]".Replace("[] ", ""), false);
                        if (audioTracks.Count > 0)
                            LogColor($"[音频] [{audioTracks[aIndex].codecs}] [{audioTracks[aIndex].bandwith} kbps] [~{FormatFileSize(audioTracks[aIndex].dur * audioTracks[aIndex].bandwith * 1024 / 8)}]", false);

                        if (videoTracks.Count > 0)
                        {
                            if (multiThread && !videoTracks[vIndex].baseUrl.Contains("-cmcc-"))
                            {
                                Log($"开始多线程下载P{p.index}视频...");
                                await MultiThreadDownloadFileAsync(videoTracks[vIndex].baseUrl, videoPath, useAria2c);
                                Log("合并视频分片...");
                                CombineMultipleFilesIntoSingleFile(GetFiles(Path.GetDirectoryName(videoPath), ".vclip"), videoPath);
                                Log("清理分片...");
                                foreach (var file in new DirectoryInfo(Path.GetDirectoryName(videoPath)).EnumerateFiles("*.?clip")) file.Delete();
                            }
                            else
                            {
                                if (multiThread && videoTracks[vIndex].baseUrl.Contains("-cmcc-"))
                                    LogError("检测到cmcc域名cdn, 已经禁用多线程");
                                Log($"开始下载P{p.index}视频...");
                                await DownloadFile(videoTracks[vIndex].baseUrl, videoPath, useAria2c);
                            }
                        }
                        if (audioTracks.Count > 0)
                        {
                            if (multiThread && !audioTracks[aIndex].baseUrl.Contains("-cmcc-"))
                            {
                                Log($"开始多线程下载P{p.index}音频...");
                                await MultiThreadDownloadFileAsync(audioTracks[aIndex].baseUrl, audioPath, useAria2c);
                                Log("合并音频分片...");
                                CombineMultipleFilesIntoSingleFile(GetFiles(Path.GetDirectoryName(audioPath), ".aclip"), audioPath);
                                Log("清理分片...");
                                foreach (var file in new DirectoryInfo(Path.GetDirectoryName(videoPath)).EnumerateFiles("*.?clip")) file.Delete();
                            }
                            else
                            {
                                if (multiThread && audioTracks[aIndex].baseUrl.Contains("-cmcc-"))
                                    LogError("检测到cmcc域名cdn, 已经禁用多线程");
                                Log($"开始下载P{p.index}音频...");
                                await DownloadFile(audioTracks[aIndex].baseUrl, audioPath, useAria2c);
                            }
                        }

                        Log($"下载P{p.index}完毕");
                        if (videoTracks.Count == 0) videoPath = "";
                        if (audioTracks.Count == 0) audioPath = "";
                        if (skipMux) continue;
                        Log("开始合并音视频" + (subtitleInfo.Count > 0 ? "和字幕" : "") + "...");
                        int code = MuxAV(videoPath, audioPath, outPath,
                            desc,
                            title,
                            vInfo.PagesInfo.Count > 1 ? ($"P{indexStr}.{p.title}") : "",
                            File.Exists($"{p.aid}/{p.aid}.jpg") ? $"{p.aid}/{p.aid}.jpg" : "",
                            subtitleInfo, audioOnly, videoOnly);
                        if (code != 0 || !File.Exists(outPath) || new FileInfo(outPath).Length == 0)
                        {
                            LogError("合并失败"); continue;
                        }
                        Log("清理临时文件...");
                        if (videoTracks.Count > 0) File.Delete(videoPath);
                        if (audioTracks.Count > 0) File.Delete(audioPath);
                        foreach (var s in subtitleInfo) File.Delete(s.path);
                        if (pagesInfo.Count == 1 || p.index == pagesInfo.Last().index || p.aid != pagesInfo.Last().aid)
                            File.Delete($"{p.aid}/{p.aid}.jpg");
                        if (Directory.Exists(p.aid) && Directory.GetFiles(p.aid).Length == 0) Directory.Delete(p.aid, true);
                    }
                    else if (clips.Count > 0 && dfns.Count > 0)   //flv
                    {
                        bool flag = false;
                    reParse:
                        //降序
                        videoTracks.Sort(Compare);

                        if (interactMode && !flag)
                        {
                            int i = 0;
                            dfns.ForEach(key => LogColor($"{i++}.{qualitys[key]}"));
                            Log("请选择最想要的清晰度(输入序号): ", false);
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            var vIndex = Convert.ToInt32(Console.ReadLine());
                            if (vIndex > dfns.Count || vIndex < 0) vIndex = 0;
                            Console.ResetColor();
                            //重新解析
                            (webJsonStr, videoTracks, audioTracks, clips, dfns) = ExtractTracks(hevc, aidOri, p.aid, p.cid, p.epid, tvApi, intlApi, dfns[vIndex]);
                            flag = true;
                            videoTracks.Clear();
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
                        if (File.Exists(outPath) && new FileInfo(outPath).Length != 0)
                        {
                            Log($"{outPath}已存在, 跳过下载...");
                            continue;
                        }
                        var pad = string.Empty.PadRight(clips.Count.ToString().Length, '0');
                        for (int i = 0; i < clips.Count; i++)
                        {
                            var link = clips[i];
                            videoPath = $"{p.aid}/{p.aid}.P{indexStr}.{p.cid}.{i.ToString(pad)}.mp4";
                            if (multiThread && !link.Contains("-cmcc-"))
                            {
                                if (videoTracks.Count != 0)
                                {
                                    Log($"开始多线程下载P{p.index}视频, 片段({(i + 1).ToString(pad)}/{clips.Count})...");
                                    await MultiThreadDownloadFileAsync(link, videoPath, useAria2c);
                                    Log("合并视频分片...");
                                    CombineMultipleFilesIntoSingleFile(GetFiles(Path.GetDirectoryName(videoPath), ".vclip"), videoPath);
                                }
                                Log("清理分片...");
                                foreach (var file in new DirectoryInfo(Path.GetDirectoryName(videoPath)).EnumerateFiles("*.?clip")) file.Delete();
                            }
                            else
                            {
                                if (multiThread && link.Contains("-cmcc-"))
                                    LogError("检测到cmcc域名cdn, 已经禁用多线程");
                                if (videoTracks.Count != 0)
                                {
                                    Log($"开始下载P{p.index}视频, 片段({(i + 1).ToString(pad)}/{clips.Count})...");
                                    await DownloadFile(link, videoPath, useAria2c);
                                }
                            }
                        }
                        Log($"下载P{p.index}完毕");
                        Log("开始合并分段...");
                        var files = GetFiles(Path.GetDirectoryName(videoPath), ".mp4");
                        videoPath = $"{p.aid}/{p.aid}.P{indexStr}.{p.cid}.mp4";
                        MergeFLV(files, videoPath);
                        if (skipMux) continue;
                        Log("开始混流视频" + (subtitleInfo.Count > 0 ? "和字幕" : "") + "...");
                        int code = MuxAV(videoPath, "", outPath,
                            desc,
                            title,
                            vInfo.PagesInfo.Count > 1 ? ($"P{indexStr}.{p.title}") : "",
                            File.Exists($"{p.aid}/{p.aid}.jpg") ? $"{p.aid}/{p.aid}.jpg" : "",
                            subtitleInfo, audioOnly, videoOnly);
                        if (code != 0 || !File.Exists(outPath) || new FileInfo(outPath).Length == 0)
                        {
                            LogError("合并失败"); continue;
                        }
                        Log("清理临时文件...");
                        if (videoTracks.Count != 0) File.Delete(videoPath);
                        foreach (var s in subtitleInfo) File.Delete(s.path);
                        if (pagesInfo.Count == 1 || p.index == pagesInfo.Last().index || p.aid != pagesInfo.Last().aid)
                            File.Delete($"{p.aid}/{p.aid}.jpg");
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
                Log("任务完成");
            }
            catch (Exception e)
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(e.Message);
                Console.ResetColor();
                Console.WriteLine();
                Thread.Sleep(1);
            }
        }
    }
}
