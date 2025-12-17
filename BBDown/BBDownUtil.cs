using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using static BBDown.Core.Entity.Entity;
using static BBDown.Core.Logger;
using static BBDown.Core.Util.HTTPUtil;

namespace BBDown;

static partial class BBDownUtil
{
    public static async Task CheckUpdateAsync()
    {
        try
        {
            var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version!;
            string nowVer = $"{ver.Major}.{ver.Minor}.{ver.Build}";
            string redirectUrl = await GetWebLocationAsync("https://github.com/nilaoda/BBDown/releases/latest");
            string latestVer = redirectUrl.Replace("https://github.com/nilaoda/BBDown/releases/tag/", "");
            if (nowVer != latestVer && !latestVer.StartsWith("https"))
            {
                Console.Title = $"发现新版本：{latestVer}";
                LogColor($"发现新版本：{latestVer}");
            }
        }
        catch (Exception)
        {
            ;
        }
    }

    public static async Task<string> GetAvIdAsync(string input)
    {
        var avid = input;
        if (input.StartsWith("http"))
        {
            if (input.Contains("b23.tv"))
            {
                string tmp = await GetWebLocationAsync(input);
                if (tmp == input) throw new Exception("无限重定向");
                input = tmp;
            }
            if (input.Contains("video/av"))
            {
                avid = AvRegex().Match(input).Groups[1].Value;
            }
            else if (input.ToLower().Contains("video/bv"))
            {
                avid = GetAidByBV(BVRegex().Match(input).Groups[1].Value);
            }
            else if (input.Contains("/cheese/"))
            {
                string epId = "";
                if (input.Contains("/ep"))
                {
                    epId = EpRegex().Match(input).Groups[1].Value;
                }
                else if (input.Contains("/ss"))
                {
                    epId = await GetEpidBySSIdAsync(SsRegex().Match(input).Groups[1].Value);
                }
                avid = $"cheese:{epId}";
            }
            else if (input.Contains("/ep"))
            {
                string epId = EpRegex().Match(input).Groups[1].Value;
                avid = $"ep:{epId}";
            }
            else if (input.Contains("/ss"))
            {
                string epId = await GetEpIdByBangumiSSIdAsync(SsRegex().Match(input).Groups[1].Value);
                avid = $"ep:{epId}";
            }
            else if (input.Contains("/medialist/") && input.Contains("business_id=") && input.Contains("business=space_collection")) // 列表类型是合集
            {
                string bizId = GetQueryString("business_id", input);
                avid = $"listBizId:{bizId}";
            }
            else if (input.Contains("/medialist/") && input.Contains("business_id=") && input.Contains("business=space_series")) // 列表类型是系列
            {
                string bizId = GetQueryString("business_id", input);
                avid = $"seriesBizId:{bizId}";
            }
            else if (input.Contains("/channel/collectiondetail?sid="))
            {
                string bizId = GetQueryString("sid", input);
                avid = $"listBizId:{bizId}";
            }
            else if (input.Contains("/channel/seriesdetail?sid="))
            {
                string bizId = GetQueryString("sid", input);
                avid = $"seriesBizId:{bizId}";
            }
            // 新版个人空间合集/系列链接兼容：
            // 例如：
            //   合集: https://space.bilibili.com/392959666/lists/1560264?type=season
            //   系列: https://space.bilibili.com/392959666/lists/1560264?type=series
            else if (input.Contains("/space.bilibili.com/") && input.Contains("/lists/"))
            {
                var type = GetQueryString("type", input).ToLower();
                // path 最后一个 / 后到 ? 前即为 sid
                var path = input.Split('?', '#')[0];
                var sidPart = path[(path.LastIndexOf('/') + 1)..];

                if (type == "season")
                {
                    avid = $"listBizId:{sidPart}";
                }
                else if (type == "series")
                {
                    avid = $"seriesBizId:{sidPart}";
                }
                else
                {
                    // 未知类型按合集处理，至少不会识别失败
                    avid = $"listBizId:{sidPart}";
                }
            }
            else if (input.Contains("/space.bilibili.com/") && input.Contains("/favlist"))
            {
                string mid = UidRegex().Match(input).Groups[1].Value;
                string fid = GetQueryString("fid", input);
                avid = $"favId:{fid}:{mid}";
            }
            else if (input.Contains("/space.bilibili.com/"))
            {
                string mid = UidRegex().Match(input).Groups[1].Value;
                avid = $"mid:{mid}";
            }
            else if (input.Contains("ep_id="))
            {
                string epId = GetQueryString("ep_id", input);
                avid = $"ep:{epId}";
            }
            else if (GlobalEpRegex().Match(input).Success)
            {
                string epId = GlobalEpRegex().Match(input).Groups[1].Value;
                avid = $"ep:{epId}";
            }
            else if (BangumiMdRegex().Match(input).Success)
            {
                string mdId = BangumiMdRegex().Match(input).Groups[1].Value;
                string epId = await GetEpIdByMDAsync(mdId);
                avid = $"ep:{epId}";
            }
            else
            {
                string web = await GetWebSourceAsync(input);
                Regex regex = StateRegex();
                string json = regex.Match(web).Groups[1].Value;
                using var jDoc = JsonDocument.Parse(json);
                string epId = jDoc.RootElement.GetProperty("epList").EnumerateArray().First().GetProperty("id").ToString();
                avid = $"ep:{epId}";
            }
        }
        else if (input.ToLower().StartsWith("bv"))
        {
            avid = GetAidByBV(input[3..]);
        }
        else if (input.ToLower().StartsWith("av")) // av
        {
            avid = input.ToLower()[2..];
        }
        else if (input.StartsWith("cheese/")) // ^cheese/(ep|ss)\d+ 格式
        {
            string epId = "";
            if (input.Contains("/ep"))
            {
                epId = EpRegex().Match(input).Groups[1].Value;
            }
            else if (input.Contains("/ss"))
            {
                epId = await GetEpidBySSIdAsync(SsRegex().Match(input).Groups[1].Value);
            }
            avid = $"cheese:{epId}";
        }
        else if (input.StartsWith("ep"))
        {
            string epId = input[2..];
            avid = $"ep:{epId}";
        }
        else if (input.StartsWith("ss"))
        {
            string epId = await GetEpIdByBangumiSSIdAsync(input[2..]);
            avid = $"ep:{epId}";
        }
        else if (input.StartsWith("md"))
        {
            string mdId = MdRegex().Match(input).Groups[1].Value;
            string epId = await GetEpIdByMDAsync(mdId);
            avid = $"ep:{epId}";
        }
        else
        {
            throw new Exception("输入有误");
        }
        return await FixAvidAsync(avid);
    }

    public static string FormatFileSize(double fileSize)
    {
        return fileSize switch
        {
            < 0 => throw new ArgumentOutOfRangeException(nameof(fileSize)),
            >= 1024 * 1024 * 1024 => $"{fileSize / (1024 * 1024 * 1024):########0.00} GB",
            >= 1024 * 1024 => $"{fileSize / (1024 * 1024):####0.00} MB",
            >= 1024 => $"{fileSize / 1024:####0.00} KB",
            _ => $"{fileSize} bytes"
        };
    }

    public static string FormatTime(int time, bool absolute = false)
    {
        var ts = TimeSpan.FromSeconds(time);
        var totalHours = (int)ts.TotalHours;
        var minutes = ts.Minutes;
        var seconds = ts.Seconds;

        if (absolute)
        {
            return $"{totalHours:D2}:{minutes:D2}:{seconds:D2}";
        }

        return totalHours == 0 ? $"{minutes:D2}m{seconds:D2}s" : $"{totalHours}h{minutes:D2}m{seconds:D2}s";
    }

    /// <summary>
    /// 通过avid检测是否为版权内容, 如果是的话返回ep:xx格式
    /// </summary>
    /// <param name="avid"></param>
    /// <returns></returns>
    private static async Task<string> FixAvidAsync(string avid)
    {
        if (!avid.All(char.IsDigit))
            return avid;
        string api = $"https://www.bilibili.com/video/av{avid}/";
        string location = await GetWebLocationAsync(api);
        return location.Contains("/ep") ? $"ep:{EpRegex().Match(location).Groups[1].Value}" : avid;
    }

    private static string GetAidByBV(string bv)
    {
        // 能在本地就在本地
        return Core.Util.BilibiliBvConverter.Decode(bv).ToString();
    }

    private static async Task<string> GetEpidBySSIdAsync(string ssid)
    {
        string api = $"https://api.bilibili.com/pugv/view/web/season?season_id={ssid}";
        string json = await GetWebSourceAsync(api);
        using var jDoc = JsonDocument.Parse(json);
        string epId = jDoc.RootElement.GetProperty("data").GetProperty("episodes").EnumerateArray().First().GetProperty("id").ToString();
        return epId;
    }

    private static async Task<string> GetEpIdByBangumiSSIdAsync(string ssId)
    {
        string api = $"https://{Core.Config.EPHOST}/pgc/view/web/season?season_id={ssId}";
        string json = await GetWebSourceAsync(api);
        using var jDoc = JsonDocument.Parse(json);
        string epId = jDoc.RootElement.GetProperty("result").GetProperty("episodes").EnumerateArray().First().GetProperty("id").ToString();
        return epId;
    }

    private static async Task<string> GetEpIdByMDAsync(string mdId)
    {
        string api = $"https://api.bilibili.com/pgc/review/user?media_id={mdId}";
        string json = await GetWebSourceAsync(api);
        using var jDoc = JsonDocument.Parse(json);
        string epId = jDoc.RootElement.GetProperty("result").GetProperty("media").GetProperty("new_ep").GetProperty("id").ToString();
        return epId;
    }

    /// <summary>
    /// 输入一堆已存在的文件, 合并到新文件
    /// </summary>
    /// <param name="files"></param>
    /// <param name="outputFilePath"></param>
    public static void CombineMultipleFilesIntoSingleFile(string[] files, string outputFilePath)
    {
        if (!files.Any()) return;
        if (files.Length == 1)
        {
            FileInfo fi = new(files[0]);
            fi.MoveTo(outputFilePath, true);
            return;
        }

        if (!Directory.Exists(Path.GetDirectoryName(outputFilePath)))
            Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath)!);

        string[] inputFilePaths = files;
        using var outputStream = File.Create(outputFilePath);
        foreach (var inputFilePath in inputFilePaths)
        {
            if (inputFilePath == "")
                continue;
            using var inputStream = File.OpenRead(inputFilePath);
            // Buffer size can be passed as the second argument.
            inputStream.CopyTo(outputStream);
            //Console.WriteLine("The file {0} has been processed.", inputFilePath);
        }
        //Global.ExplorerFile(outputFilePath);
    }

    /// <summary>
    /// 寻找指定目录下指定后缀的文件的详细路径 如".txt"
    /// </summary>
    /// <param name="dir"></param>
    /// <param name="ext"></param>
    /// <returns></returns>
    public static string[] GetFiles(string dir, string ext)
    {
        List<string> al = [];
        StringBuilder sb = new();
        DirectoryInfo d = new(dir);
        foreach (FileInfo fi in d.GetFiles())
        {
            if (fi.Extension.ToUpper() == ext.ToUpper())
            {
                al.Add(fi.FullName);
            }
        }
        string[] res = al.ToArray();
        Array.Sort(res); //排序
        return res;
    }

    private static readonly char[] InvalidChars = "34,60,62,124,0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,58,42,63,92,47"
        .Split(',').Select(s => (char)byte.Parse(s)).ToArray();

    public static string GetValidFileName(string input, string re = "_", bool filterSlash = false)
    {
        string title = input;

        foreach (char invalidChar in InvalidChars)
        {
            title = title.Replace(invalidChar.ToString(), re);
        }
        if (filterSlash)
        {
            title = title.Replace("/", re);
            title = title.Replace("\\", re);
        }
        return title;
    }


    /// <summary>
    /// 获取url字符串参数, 返回参数值字符串
    /// </summary>
    /// <param name="name">参数名称</param>
    /// <param name="url">url字符串</param>
    /// <returns></returns>
    public static string GetQueryString(string name, string url)
    {
        Regex re = QueryRegex();
        MatchCollection mc = re.Matches(url);
        foreach (Match m in mc.Cast<Match>())
        {
            if (m.Result("$2").Equals(name))
            {
                return m.Result("$3");
            }
        }
        return "";
    }

    //https://s1.hdslb.com/bfs/static/player/main/video.9efc0c61.js
    public static string GetSession(string buvid3)
    {
        //这个参数可以没有 所以此处就不写具体实现了
        throw new NotImplementedException();
    }

    public static string GetSign(string parms)
    {
        string toEncode = parms + "59b43e04ad6965f34319062b478f83dd";
        return string.Concat(MD5.HashData(Encoding.UTF8.GetBytes(toEncode)).Select(i => i.ToString("x2")));
    }

    public static string GetTimeStamp(bool bflag)
    {
        DateTimeOffset ts = DateTimeOffset.Now;
        return (bflag ? ts.ToUnixTimeSeconds() : ts.ToUnixTimeMilliseconds()).ToString();
    }

    //https://stackoverflow.com/questions/1344221/how-can-i-generate-random-alphanumeric-strings
    private static readonly Random random = new();
    public static string GetRandomString(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    //https://stackoverflow.com/a/45088333
    public static string ToQueryString(NameValueCollection nameValueCollection)
    {
        NameValueCollection httpValueCollection = HttpUtility.ParseQueryString(string.Empty);
        httpValueCollection.Add(nameValueCollection);
        return httpValueCollection.ToString()!;
    }

    public static Dictionary<string, string> ToDictionary(this NameValueCollection nameValueCollection)
    {
        var dict = new Dictionary<string, string>();
        foreach (var key in nameValueCollection.AllKeys)
        {
            dict[key!] = nameValueCollection[key]!;
        }
        return dict;
    }

    public static NameValueCollection GetTVLoginParms()
    {
        NameValueCollection sb = new();
        DateTime now = DateTime.Now;
        string deviceId = GetRandomString(20);
        string buvid = GetRandomString(37);
        string fingerprint = $"{now:yyyyMMddHHmmssfff}{GetRandomString(45)}";
        sb.Add("appkey", "4409e2ce8ffd12b8");
        sb.Add("auth_code", "");
        sb.Add("bili_local_id", deviceId);
        sb.Add("build", "102801");
        sb.Add("buvid", buvid);
        sb.Add("channel", "master");
        sb.Add("device", "OnePlus");
        sb.Add($"device_id", deviceId);
        sb.Add("device_name", "OnePlus7TPro");
        sb.Add("device_platform", "Android10OnePlusHD1910");
        sb.Add($"fingerprint", fingerprint);
        sb.Add($"guid", buvid);
        sb.Add($"local_fingerprint", fingerprint);
        sb.Add($"local_id", buvid);
        sb.Add("mobi_app", "android_tv_yst");
        sb.Add("networkstate", "wifi");
        sb.Add("platform", "android");
        sb.Add("sys_ver", "29");
        sb.Add($"ts", GetTimeStamp(true));
        sb.Add($"sign", GetSign(ToQueryString(sb)));

        return sb;
    }

    /// <summary>
    /// 检测ffmpeg是否识别杜比视界
    /// </summary>
    /// <returns></returns>
    public static bool CheckFFmpegDOVI()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = BBDownMuxer.FFMPEG,
                    Arguments = "-version",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string info = process.StandardOutput.ReadToEnd() + Environment.NewLine + process.StandardError.ReadToEnd();
            process.WaitForExit();
            var match = LibavutilRegex().Match(info);
            if (!match.Success) return false;
            if((Convert.ToInt32(match.Groups[1].Value)==57 && Convert.ToInt32(match.Groups[1].Value) >= 17)
               || Convert.ToInt32(match.Groups[1].Value) > 57)
            {
                return true;
            }
        }
        catch (Exception)
        {
        }
        return false;
    }

    /// <summary>
    /// 获取章节信息
    /// </summary>
    /// <param name="cid"></param>
    /// <param name="aid"></param>
    /// <returns></returns>
    public static async Task<List<ViewPoint>> FetchPointsAsync(string cid, string aid)
    {
        var ponints = new List<ViewPoint>();
        try
        {
            string api = $"https://api.bilibili.com/x/player/wbi/v2?cid={cid}&aid={aid}";
            string json = await GetWebSourceAsync(api);
            using var infoJson = JsonDocument.Parse(json);
            if (infoJson.RootElement.GetProperty("data").TryGetProperty("view_points", out JsonElement vPoint))
            {
                foreach (var point in vPoint.EnumerateArray())
                {
                    ponints.Add(new ViewPoint()
                    {
                        title = point.GetProperty("content").GetString()!,
                        start = int.Parse(point.GetProperty("from").ToString()),
                        end = int.Parse(point.GetProperty("to").ToString())
                    });
                }
            }
        }
        catch (Exception) { }
        return ponints;
    }

    /// <summary>
    /// 生成metadata文件, 用于ffmpeg混流章节信息
    /// </summary>
    /// <param name="points"></param>
    /// <returns></returns>
    public static string GetFFmpegMetaString(List<ViewPoint> points)
    {
        StringBuilder sb = new();
        sb.AppendLine(";FFMETADATA");
        foreach (var p in points)
        {
            var time = 1000; //固定 1000
            sb.AppendLine("[CHAPTER]");
            sb.AppendLine($"TIMEBASE=1/{time}");
            sb.AppendLine($"START={p.start * time}");
            sb.AppendLine($"END={p.end * time}");
            sb.AppendLine($"title={p.title}");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    /// <summary>
    /// 生成metadata文件, 用于mp4box混流章节信息
    /// </summary>
    /// <param name="points"></param>
    /// <returns></returns>
    public static string GetMp4boxMetaString(List<ViewPoint> points)
    {
        StringBuilder sb = new();
        foreach (var p in points)
        {
            sb.AppendLine($"{FormatTime(p.start, true)} {p.title}");
        }
        return sb.ToString();
    }

    public static string? FindExecutable(string name)
    {
        var fileExt = OperatingSystem.IsWindows() ? ".exe" : "";
        var searchPath = new [] { Environment.CurrentDirectory, Program.APP_DIR };
        var envPath = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? [];
        return searchPath.Concat(envPath).Select(p => Path.Combine(p, name + fileExt)).FirstOrDefault(File.Exists);
    }

    public static string RSubString(string sub)
    {
        sub = sub[(sub.LastIndexOf('/') + 1)..];
        return sub[..sub.LastIndexOf('.')];
    }

    private static string GetMixinKey(string orig)
    {
        byte[] mixinKeyEncTab = 
        [
            46, 47, 18, 2, 53, 8, 23, 32, 15, 50, 10, 31, 58, 3, 45, 35,
            27, 43, 5, 49, 33, 9, 42, 19, 29, 28, 14, 39, 12, 38, 41, 13
        ];

        var tmp = new StringBuilder(32);
        foreach (var index in mixinKeyEncTab)
        {
            tmp.Append(orig[index]);
        }
        return tmp.ToString();
    }

    public static async Task<bool> CheckLogin(string cookie)
    {
        try
        {
            var api = "https://api.bilibili.com/x/web-interface/nav";
            var source = await GetWebSourceAsync(api);
            var json = JsonDocument.Parse(source).RootElement;
            var is_login = json.GetProperty("data").GetProperty("isLogin").GetBoolean();
            var wbi_img = json.GetProperty("data").GetProperty("wbi_img");
            Core.Config.WBI = GetMixinKey(RSubString(wbi_img.GetProperty("img_url").GetString()) + RSubString(wbi_img.GetProperty("sub_url").GetString()));
            LogDebug("wbi: {0}", Core.Config.WBI);
            return is_login;
        }
        catch (Exception)
        {
            return false;
        }
    }

    [GeneratedRegex("av(\\d+)")]
    private static partial Regex AvRegex();
    [GeneratedRegex("[Bb][Vv]1(\\w+)")]
    private static partial Regex BVRegex();
    [GeneratedRegex("/ep(\\d+)")]
    private static partial Regex EpRegex();
    [GeneratedRegex("/ss(\\d+)")]
    private static partial Regex SsRegex();
    [GeneratedRegex(@"space\.bilibili\.com/(\d+)")]
    private static partial Regex UidRegex();
    [GeneratedRegex(@"\.bilibili\.tv\/\w+\/play\/\d+\/(\d+)")]
    private static partial Regex GlobalEpRegex();
    [GeneratedRegex("bangumi/media/(md\\d+)")]
    private static partial Regex BangumiMdRegex();
    [GeneratedRegex(@"window.__INITIAL_STATE__=([\s\S].*?);\(function\(\)")]
    private static partial Regex StateRegex();
    [GeneratedRegex("md(\\d+)")]
    private static partial Regex MdRegex();
    [GeneratedRegex("(^|&)?(\\w+)=([^&]+)(&|$)?", RegexOptions.Compiled)]
    private static partial Regex QueryRegex();
    [GeneratedRegex("libavutil\\s+(\\d+)\\. +(\\d+)\\.")]
    private static partial Regex LibavutilRegex();
}