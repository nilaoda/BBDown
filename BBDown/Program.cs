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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static BBDown.BBDownEntity;
using static BBDown.BBDownUtil;
using static BBDown.BBDownParser;
using static BBDown.BBDownLogger;
using static BBDown.BBDownMuxer;
using System.Text;

namespace BBDown
{
    class Program
    {
        public static string BB_VERSION = "1.2.1";
        public static string COOKIE = "";
        public static string TOKEN = "";
        static Dictionary<string, string> qualitys = new Dictionary<string, string>() {
            {"120","超清 4K" }, {"116","高清 1080P60" },{"112","高清 1080P+" },
            {"80","高清 1080P" }, {"74","高清 720P60" },{"64","高清 720P" },
            {"48","高清 720P" }, {"32","清晰 480P" },{"16","流畅 360P" }
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
            public bool OnlyHevc { get; set; }
            public bool OnlyShowInfo { get; set; }
            public bool Interactive { get; set; }
            public bool HideStreams { get; set; }
            public bool MultiThread { get; set; }
            public string SelectPage { get; set; }
            public string Cookie { get; set; }
            public string AccessToken { get; set; }
        }

        public static int Main(params string[] args)
        {
            ServicePointManager.DefaultConnectionLimit = 2048;

            var rootCommand = new RootCommand
            {
                new Argument<string>(
                    "url",
                    description: "视频地址 或 av,bv,BV,ep,ss"),
                new Option<bool>(
                    new string[]{ "--use-tv-api" ,"-tv"},
                    "使用TV端API解析"),
                new Option<bool>(
                    new string[]{ "--only-hevc" ,"-hevc"},
                    "选择HEVC编码"),
                new Option<bool>(
                    new string[]{ "--only-show-info" ,"-info"},
                    "仅解析流信息"),
                new Option<bool>(
                    new string[]{ "--hide-streams", "-hs"},
                    "不显示可用音视频流"),
                new Option<bool>(
                    new string[]{ "--interactive", "-ia"},
                    "交互选择流"),
                new Option<bool>(
                    new string[]{ "--multi-thread", "-mt"},
                    "多线程下载"),
                new Option<string>(
                    new string[]{ "--select-page" ,"-p"},
                    "指定分p或分p范围"),
                new Option<string>(
                    new string[]{ "--cookie" ,"-c"},
                    "设置cookie以访问会员内容"),
                new Option<string>(
                    new string[]{ "--access-token" ,"-a"},
                    "设置access_token以访问TV端会员内容")
            };

            Command loginCommand = new Command(
                "login",
                "扫描二维码登录WEB账号");
            rootCommand.AddCommand(loginCommand);
            Command loginTVCommand = new Command(
                "logintv",
                "扫描二维码登录TV账号");
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
                //Console.WriteLine(myOption.ToString());
                await DoWorkAsync(myOption);
            });

            return rootCommand.InvokeAsync(args).Result;
        }

        private static async Task DoWorkAsync(MyOption myOption)
        {
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"BBDown version {BB_VERSION}, Bilibili Downloader.\r\n");
            Console.ResetColor();
            Console.Write("请注意：任何BUG请前往以下网址反馈：\r\n" +
                "https://github.com/nilaoda/BBDown/issues\r\n");
            Console.WriteLine();
            try
            {
                bool interactMode = myOption.Interactive;
                bool infoMode = myOption.OnlyShowInfo;
                bool tvApi = myOption.UseTvApi;
                bool hevc = myOption.OnlyHevc;
                bool hideStreams = myOption.HideStreams;
                bool multiThread = myOption.MultiThread;
                string input = myOption.Url;
                string selectPage = myOption.SelectPage;
                string aid = "";
                COOKIE = myOption.Cookie;
                TOKEN = myOption.AccessToken != null ? myOption.AccessToken.Replace("access_token=", "") : "";
                List<string> selectedPages = null;
                if (!string.IsNullOrEmpty(GetQueryString("p", input)))
                {
                    selectedPages = new List<string>();
                    selectedPages.Add(GetQueryString("p", input));
                }

                if (File.Exists(Path.Combine(AppContext.BaseDirectory, "BBDown.data")) && !tvApi) 
                {
                    Log("加载本地cookie...");
                    COOKIE = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "BBDown.data"));
                }
                if (File.Exists(Path.Combine(AppContext.BaseDirectory, "BBDownTV.data")) && tvApi)
                {
                    Log("加载本地token...");
                    TOKEN = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "BBDownTV.data"));
                }
                Log("获取aid...");
                aid = await GetAvIdAsync(input);
                Log("获取aid结束: " + aid);
                //-p的优先级大于URL中的自带p参数，所以先清空selectedPages
                if (!string.IsNullOrEmpty(selectPage)) 
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

                if (string.IsNullOrEmpty(aid)) throw new Exception("输入有误");
                string api = $"https://api.bilibili.com/x/web-interface/view?aid={aid}";
                string json = GetWebSource(api);
                JObject infoJson = JObject.Parse(json);
                Log("获取视频信息...");
                string title = infoJson["data"]["title"].ToString();
                string desc = infoJson["data"]["desc"].ToString();
                string pic = infoJson["data"]["pic"].ToString();
                string pubTime = infoJson["data"]["pubdate"].ToString();
                LogColor("视频标题: " + title);
                Log("发布时间: " + new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Convert.ToDouble(pubTime)).ToLocalTime());
                JArray subs = JArray.Parse(infoJson["data"]["subtitle"]["list"].ToString());
                JArray pages = JArray.Parse(infoJson["data"]["pages"].ToString());
                List<Page> pagesInfo = new List<Page>();
                List<Subtitle> subtitleInfo = new List<Subtitle>();
                foreach (JObject sub in subs)
                {
                    Subtitle subtitle = new Subtitle();
                    subtitle.url = sub["subtitle_url"].ToString();
                    subtitle.lan = sub["lan"].ToString();
                    subtitle.path = $"{aid}/{aid}.{subtitle.lan}.srt";
                    subtitleInfo.Add(subtitle);
                }
                bool more = false;
                bool bangumi = false;
                string epId = "";

                if (!infoMode)
                {
                    if (!Directory.Exists(aid))
                    {
                        Directory.CreateDirectory(aid);
                    }
                    Log("下载封面...");
                    new WebClient().DownloadFile(pic, $"{aid}/{aid}.jpg");
                    foreach (Subtitle s in subtitleInfo)
                    {
                        Log($"下载字幕 {s.lan}...");
                        BBDownSubUtil.SaveSubtitle(s.url, s.path);
                    }
                }

                try
                {
                    if (infoJson["data"]["redirect_url"].ToString().Contains("bangumi"))
                    {
                        bangumi = true;
                        epId = Regex.Match(infoJson["data"]["redirect_url"].ToString(), "ep(\\d+)").Groups[1].Value;
                    }
                }
                catch { }
                foreach (JObject page in pages)
                {
                    Page p = new Page(page["page"].Value<int>(),
                        page["cid"].ToString(), page["part"].ToString(),
                        page["duration"].Value<int>(), page["dimension"]["width"] + "x" + page["dimension"]["height"]);
                    pagesInfo.Add(p);

                    if (more) continue;
                    if (Convert.ToInt32(page["page"].ToString()) > 5)
                    {
                        Log("P...");
                        Log("分P太多, 已经省略部分...");
                        more = true;
                    }
                    else
                    {
                        Log($"P{p.index}: [{p.cid}] [{p.title}] [{FormatTime(p.dur)}]");
                    }
                }
                Log($"共计 {pagesInfo.Count} 个分P, 已选择：" + (selectedPages == null ? "ALL" : string.Join(",", selectedPages)));
                
                foreach (Page p in pagesInfo)
                {
                    //跳过不需要的分P
                    if (selectedPages != null && !selectedPages.Contains(p.index.ToString())) continue;

                    Log($"开始解析P{p.index}...");
                    List<Video> videoInfo = new List<Video>();
                    List<Audio> audioInfo = new List<Audio>();
                    string videoPath = $"{aid}/{aid}.P{p.index}.{p.cid}.mp4";
                    string audioPath = $"{aid}/{aid}.P{p.index}.{p.cid}.m4a";
                    string outPath = GetValidFileName(title + (pagesInfo.Count > 1 ? $"[P{p.index}.{p.title}].mp4" : ".mp4"));
                    //调用解析
                    string webJson = GetPlayJson(aid, p.cid, epId, tvApi, bangumi);
                    //File.WriteAllText($"debug.json", JObject.Parse(webJson).ToString());

                    JArray audio = null;
                    JArray video = null;

                    //此处代码简直灾难，后续优化吧
                    if (webJson.Contains("\"dash\":{")) //dash
                    {
                        string nodeName = "data";
                        if (webJson.Contains("\"result\":{"))
                        {
                            nodeName = "result";
                        }

                        try { video = JArray.Parse(!tvApi ? JObject.Parse(webJson)[nodeName]["dash"]["video"].ToString() : JObject.Parse(webJson)["dash"]["video"].ToString()); } catch { }
                        try { audio = JArray.Parse(!tvApi ? JObject.Parse(webJson)[nodeName]["dash"]["audio"].ToString() : JObject.Parse(webJson)["dash"]["audio"].ToString()); } catch { }
                        if (video != null)
                            foreach (JObject node in video)
                            {
                                Video v = new Video();
                                v.id = node["id"].ToString();
                                v.dfn = qualitys[node["id"].ToString()];
                                v.bandwith = Convert.ToInt64(node["bandwidth"].ToString()) / 1000;
                                v.baseUrl = node["base_url"].ToString();
                                v.codecs = node["codecid"].ToString() == "12" ? "HEVC" : "AVC";
                                if (!tvApi)
                                {
                                    v.res = node["width"].ToString() + "x" + node["height"].ToString();
                                    v.fps = node["frame_rate"].ToString();
                                }
                                if (hevc && v.codecs == "AVC") continue;
                                videoInfo.Add(v);
                            }

                        if (audio != null)
                            foreach (JObject node in audio)
                            {
                                Audio a = new Audio();
                                a.id = node["id"].ToString();
                                a.dfn = node["id"].ToString();
                                a.bandwith = Convert.ToInt64(node["bandwidth"].ToString()) / 1000;
                                a.baseUrl = node["base_url"].ToString();
                                a.codecs = "M4A";
                                audioInfo.Add(a);
                            }

                        if (video != null && videoInfo.Count == 0)
                        {
                            LogError("没有找到符合要求的视频流");
                            continue;
                        }
                        if (audio != null && audioInfo.Count == 0)
                        {
                            LogError("没有找到符合要求的音频流");
                            continue;
                        }
                        //降序
                        videoInfo.Sort(Compare);
                        audioInfo.Sort(Compare);
                        int vIndex = 0;
                        int aIndex = 0;
                        if (!hideStreams)
                        {
                            //展示所有的音视频流信息
                            Log($"共计{videoInfo.Count}条视频流.");
                            int index = 0;
                            foreach (var v in videoInfo)
                            {
                                LogColor($"{index++}. [{v.dfn}] [{v.res}] [{v.codecs}] [{v.fps}] [{v.bandwith} kbps] [~{FormatFileSize(p.dur * v.bandwith * 1024 / 8)}]".Replace("[] ", ""), false);
                            }
                            Log($"共计{audioInfo.Count}条音频流.");
                            index = 0;
                            foreach (var a in audioInfo)
                            {
                                LogColor($"{index++}. [{a.codecs}] [{a.bandwith} kbps] [~{FormatFileSize(p.dur * a.bandwith * 1024 / 8)}]", false);
                            }
                        }
                        if (infoMode) continue;
                        if (interactMode && !hideStreams)
                        {
                            Log("请选择一条视频流(输入序号): ", false);
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            vIndex = Convert.ToInt32(Console.ReadLine());
                            Console.ResetColor();
                            Log("请选择一条音频流(输入序号): ", false);
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            aIndex = Convert.ToInt32(Console.ReadLine());
                            Console.ResetColor();
                        }
                        if (File.Exists(outPath) && new FileInfo(outPath).Length != 0)
                        {
                            Log($"{outPath}已存在, 跳过下载...");
                            continue;
                        }

                        Log($"已选择的流:");
                        if (video != null)
                            LogColor($"[视频] [{videoInfo[vIndex].dfn}] [{videoInfo[vIndex].res}] [{videoInfo[vIndex].codecs}] [{videoInfo[vIndex].fps}] [{videoInfo[vIndex].bandwith} kbps] [~{FormatFileSize(p.dur * videoInfo[vIndex].bandwith * 1024 / 8)}]".Replace("[] ", ""), false);
                        if (audio != null)
                            LogColor($"[音频] [{audioInfo[aIndex].codecs}] [{audioInfo[aIndex].bandwith} kbps] [~{FormatFileSize(p.dur * audioInfo[aIndex].bandwith * 1024 / 8)}]", false);

                        if (multiThread && !videoInfo[vIndex].baseUrl.Contains("-cmcc-"))
                        {
                            if (video != null)
                            {
                                Log($"开始多线程下载P{p.index}视频...");
                                await MultiThreadDownloadFileAsync(videoInfo[vIndex].baseUrl, videoPath);
                                Log("合并视频分片...");
                                CombineMultipleFilesIntoSingleFile(GetFiles(Path.GetDirectoryName(videoPath), ".vclip"), videoPath);
                            }
                            if (audio != null)
                            {
                                Log($"开始多线程下载P{p.index}音频...");
                                await MultiThreadDownloadFileAsync(audioInfo[aIndex].baseUrl, audioPath);
                                Log("合并音频分片...");
                                CombineMultipleFilesIntoSingleFile(GetFiles(Path.GetDirectoryName(audioPath), ".aclip"), audioPath);
                            }
                            Log("清理分片...");
                            foreach (var file in new DirectoryInfo(Path.GetDirectoryName(videoPath)).EnumerateFiles("*.?clip")) file.Delete();
                        }
                        else
                        {
                            if (multiThread && videoInfo[vIndex].baseUrl.Contains("-cmcc-"))
                                LogError("检测到cmcc域名cdn, 已经禁用多线程");
                            if (video != null)
                            {
                                Log($"开始下载P{p.index}视频...");
                                await DownloadFile(videoInfo[vIndex].baseUrl, videoPath);
                            }
                            if (audio != null)
                            {
                                Log($"开始下载P{p.index}音频...");
                                await DownloadFile(audioInfo[aIndex].baseUrl, audioPath);
                            }
                        }
                        Log($"下载P{p.index}完毕");
                        if (video == null) videoPath = "";
                        if (audio == null) audioPath = "";
                        Log("开始合并音视频" + (subtitleInfo.Count > 0 ? "和字幕" : "") + "...");
                        int code = MuxAV(videoPath, audioPath, outPath,
                            desc.Replace("\"", ""),
                            title.Replace("\"", ""),
                            pagesInfo.Count > 1 ? GetValidFileName($"P{p.index}.{p.title}") : "",
                            File.Exists($"{aid}/{aid}.jpg") ? $"{aid}/{aid}.jpg" : "",
                            subtitleInfo);
                        if (code != 0 || !File.Exists(outPath) || new FileInfo(outPath).Length == 0)
                        {
                            LogError("合并失败"); continue;
                        }
                        Log("清理临时文件...");
                        if (video != null) File.Delete(videoPath);
                        if (audio != null) File.Delete(audioPath);
                        foreach (var s in subtitleInfo) File.Delete(s.path);
                        if (pagesInfo.Count == 1 || p.index == pagesInfo.Count)
                            File.Delete($"{aid}/{aid}.jpg");
                        if (Directory.Exists(aid) && Directory.GetFiles(aid).Length == 0) Directory.Delete(aid, true);
                    }
                    else if (webJson.Contains("\"durl\":["))  //flv
                    {
                        //重新解析最高清晰度
                        webJson = GetPlayJson(aid, p.cid, epId, tvApi, bangumi, "120");
                        List<string> clips = new List<string>();
                        string quality = "";
                        string videoCodecid = "";
                        string url = "";
                        string format = "";
                        double size = 0;
                        double length = 0;
                        if (webJson.Contains("\"data\":{"))
                        {
                            format = JObject.Parse(webJson)["data"]["format"].ToString();
                            quality = JObject.Parse(webJson)["data"]["quality"].ToString();
                            videoCodecid = JObject.Parse(webJson)["data"]["video_codecid"].ToString();
                            //获取所有分段
                            foreach (JObject node in JArray.Parse(JObject.Parse(webJson)["data"]["durl"].ToString()))
                            {
                                clips.Add(node["url"].ToString());
                                size += node["size"].Value<double>();
                                length += node["length"].Value<double>();
                            }
                        }
                        else
                        {
                            format = JObject.Parse(webJson)["format"].ToString();
                            quality = JObject.Parse(webJson)["quality"].ToString();
                            videoCodecid = JObject.Parse(webJson)["video_codecid"].ToString();
                            //获取所有分段
                            foreach (JObject node in JArray.Parse(JObject.Parse(webJson)["durl"].ToString()))
                            {
                                clips.Add(node["url"].ToString());
                                size += node["size"].Value<double>();
                                length += node["length"].Value<double>();
                            }
                        }
                        Video v1 = new Video();
                        v1.id = quality;
                        v1.dfn = qualitys[quality];
                        v1.baseUrl = url;
                        v1.codecs = videoCodecid == "12" ? "HEVC" : "AVC";
                        if (hevc && v1.codecs == "AVC") { }
                        else videoInfo.Add(v1);

                        //降序
                        videoInfo.Sort(Compare);

                        Log($"共计{videoInfo.Count}条流({format}, 共有{clips.Count}个分段).");
                        int index = 0;
                        int vIndex = 0;
                        foreach (var v in videoInfo)
                        {
                            LogColor($"{index++}. [{v.dfn}] [{v.res}] [{v.codecs}] [{v.fps} fps] [~{(size / 1024 / (length / 1000) * 8).ToString("00")} kbps] [{FormatFileSize(size)}]".Replace("[] ", ""), false);
                        }
                        if (infoMode) continue;
                        if (interactMode)
                        {
                            Log("请选择一条流(输入序号): ", false);
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            vIndex = Convert.ToInt32(Console.ReadLine());
                            Console.ResetColor();
                        }
                        if (File.Exists(outPath) && new FileInfo(outPath).Length != 0)
                        {
                            Log($"{outPath}已存在, 跳过下载...");
                            continue;
                        }
                        var pad = string.Empty.PadRight(clips.Count.ToString().Length, '0');
                        for (int i = 0; i < clips.Count; i++) 
                        {
                            var link = clips[i];
                            videoPath= $"{aid}/{aid}.P{p.index}.{p.cid}.{i.ToString(pad)}.mp4";
                            if (multiThread && !link.Contains("-cmcc-"))
                            {
                                if (videoInfo.Count != 0)
                                {
                                    Log($"开始多线程下载P{p.index}视频, 片段({(i + 1).ToString(pad)}/{clips.Count})...");
                                    await MultiThreadDownloadFileAsync(link, videoPath);
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
                                if (videoInfo.Count != 0)
                                {
                                    Log($"开始下载P{p.index}视频, 片段({(i + 1).ToString(pad)}/{clips.Count})...");
                                    await DownloadFile(link, videoPath);
                                }
                            }
                        }
                        Log($"下载P{p.index}完毕");
                        Log("开始合并分段...");
                        var files = GetFiles(Path.GetDirectoryName(videoPath), ".mp4");
                        videoPath = $"{aid}/{aid}.P{p.index}.{p.cid}.mp4";
                        MergeFLV(files, videoPath);
                        Log("开始混流视频" + (subtitleInfo.Count > 0 ? "和字幕" : "") + "...");
                        int code = MuxAV(videoPath, "", outPath,
                            desc.Replace("\"", ""),
                            title.Replace("\"", ""),
                            pagesInfo.Count > 1 ? GetValidFileName($"P{p.index}.{p.title}") : "",
                            File.Exists($"{aid}/{aid}.jpg") ? $"{aid}/{aid}.jpg" : "",
                            subtitleInfo);
                        if (code != 0 || !File.Exists(outPath) || new FileInfo(outPath).Length == 0)
                        {
                            LogError("合并失败"); continue;
                        }
                        Log("清理临时文件...");
                        if (videoInfo.Count != 0) File.Delete(videoPath);
                        foreach (var s in subtitleInfo) File.Delete(s.path);
                        if (pagesInfo.Count == 1 || p.index == pagesInfo.Count)
                            File.Delete($"{aid}/{aid}.jpg");
                        if (Directory.Exists(aid) && Directory.GetFiles(aid).Length == 0) Directory.Delete(aid, true);
                    }
                    else
                    {
                        LogError("解析此分P失败");
                        continue;
                    }
                }
                Log("任务完成");
            }
            catch (Exception e)
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(e.Message);
                Console.ResetColor();
                Thread.Sleep(1);
            }
        }
    }
}
