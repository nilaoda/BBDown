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
using Newtonsoft.Json;

namespace BBDown
{
    partial class Program
    {
        public static string COOKIE { get; set; } = "";
        public static string TOKEN { get; set; } = "";

        static Dictionary<string, string> qualitys = new Dictionary<string, string>() {
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
            Console.Write("请注意：任何BUG请前往以下网址反馈：\r\n" +
                "https://github.com/nilaoda/BBDown/issues\r\n");
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
                    myOption.AudioOnly = false;
                    myOption.VideoOnly = false;
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
                        subtitleInfo = BBDownSubUtil.GetSubtitles(p.aid, p.cid);
                        foreach (Subtitle s in subtitleInfo)
                        {
                            Log($"下载字幕 {s.lan} => {BBDownSubUtil.SubDescDic[s.lan]}...");
                            LogDebug("下载：{0}", s.url);
                            BBDownSubUtil.SaveSubtitle(s.url, s.path);
                        }
                    }
                    List<Video> videoTracks = new List<Video>();
                    List<Audio> audioTracks = new List<Audio>();
                    string indexStr = p.index.ToString("0".PadRight(pagesInfo.OrderByDescending(_p => _p.index).First().index.ToString().Length, '0'));
                    string videoPath = $"{p.aid}/{p.aid}.P{indexStr}.{p.cid}.mp4";
                    string audioPath = $"{p.aid}/{p.aid}.P{indexStr}.{p.cid}.m4a";
                    //处理文件夹以.结尾导致的异常情况
                    if (title.EndsWith(".")) title += "_fix";
                    string outPath = GetValidFileName(title) + (pagesInfo.Count > 1 ? $"/[P{indexStr}]{GetValidFileName(p.title)}" : (vInfo.PagesInfo.Count > 1 ? $"[P{indexStr}]{GetValidFileName(p.title)}" : "")) + ".mp4";
                    //调用解析
                    string webJson = GetPlayJson(aidOri, p.aid, p.cid, p.epid, tvApi);
                    //File.WriteAllText($"debug.json", JObject.Parse(webJson).ToString());

                    IBBDownParse webResp;
                    if (tvApi)
                        webResp = JsonConvert.DeserializeObject<TVResponse>(webJson);
                    else
                        webResp = JsonConvert.DeserializeObject<PlayInfoResp>(webJson);


                    int pDur;
                    //此处代码简直灾难，后续优化吧
                    if (webJson.Contains("\"dash\":{")) //dash
                    {

                        List<Video> videoTrackstmp;
                        List<Audio> audioTrackstmp;

                        (pDur, videoTrackstmp, audioTrackstmp) = webResp.GetVideoInfos(p, myOption);

                        #region 此处处理免二压视频，需要单独再请求一次
                        webJson = GetPlayJson(aidOri, p.aid, p.cid, p.epid, tvApi, "125");
                        //File.WriteAllText($"debug.json", JObject.Parse(webJson).ToString());

                        if (tvApi)
                            webResp = JsonConvert.DeserializeObject<TVResponse>(webJson);
                        else
                            webResp = JsonConvert.DeserializeObject<PlayInfoResp>(webJson);
                        int tmpdata;
                        (tmpdata, videoTracks, audioTracks) = webResp.GetVideoInfos(p, myOption);

                        //合并两次解析video结果
                        foreach (var item in videoTracks)
                        {
                            if (!videoTracks.Contains(item)) videoTracks.Add(item);
                        }

                        #endregion

                        if (videoTracks.Count == 0)
                        {
                            LogError("没有找到符合要求的视频流");
                            continue;
                        }
                        if (audioTracks.Count == 0)
                        {
                            LogError("没有找到符合要求的音频流");
                            continue;
                        }

                        //选择音视频

                        (Video dvideo, Audio daudio, bool isSelected) = VideoSelector(videoTracks, audioTracks, myOption, outPath, pDur);
                        if (!isSelected)
                        {
                            continue;
                        }

                        #region  下载

                        await DownLoadData(vInfo, subtitleInfo, p, videoTracks, audioTracks, dvideo, daudio, myOption, indexStr);

                        #endregion
                    }
                    else if (webJson.Contains("\"durl\":["))  //flv
                    {

                        Video v1;
                        (videoTracks, v1) = webResp.GetVideoInfo(myOption);

                        //选择画质，如果和当前不一样，进行下载
                        string quality = SingleVideoSelector(v1, myOption);
                        if (v1.id != quality)
                        {
                            videoTracks.Clear();
                            webJson = GetPlayJson(aidOri, p.aid, p.cid, p.epid, tvApi, quality);
                            if (tvApi)
                                webResp = JsonConvert.DeserializeObject<TVResponse>(webJson);
                            else
                                webResp = JsonConvert.DeserializeObject<PlayInfoResp>(webJson);
                            (videoTracks, v1) = webResp.GetVideoInfo(myOption);

                        }

                        //下载

                        await DownloadFlvFile(vInfo, subtitleInfo, p, videoTracks, v1, myOption, indexStr);

                    }
                    else
                    {
                        if (webJson.Contains("平台不可观看"))
                        {
                            throw new Exception("当前(WEB)平台不可观看，请尝试使用TV API解析。");
                        }
                        else if (webJson.Contains("地区不可观看") || webJson.Contains("地区不支持"))
                        {
                            throw new Exception("当前地区不可观看，请尝试使用代理解析。");
                        }
                        else if (webJson.Contains("购买后才能观看"))
                        {
                            throw new Exception("购买后才能观看哦");
                        }
                        LogError("解析此分P失败(使用--debug查看详细信息)");
                        LogDebug("{0}", webJson);
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
