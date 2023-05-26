using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using static BBDown.Core.Entity.Entity;
using static BBDown.Core.Logger;
using static BBDown.Core.Util.HTTPUtil;

namespace BBDown
{
    static partial class BBDownUtil
    {
        public static async Task CheckUpdateAsync()
        {
            try
            {
                var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version!;
                string nowVer = $"{ver.Major}.{ver.Minor}.{ver.Build}";
                string redirctUrl = await Get302("https://github.com/nilaoda/BBDown/releases/latest");
                string latestVer = redirctUrl.Replace("https://github.com/nilaoda/BBDown/releases/tag/", "");
                if (nowVer != latestVer && !latestVer.StartsWith("https"))
                {
                    Console.Title = $"发现新版本：{latestVer}";
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
                    input = await Get302(input);
                Match match;
                if (input.Contains("video/av"))
                {
                    avid = AvRegex().Match(input).Groups[1].Value;
                }
                else if (input.ToLower().Contains("video/bv"))
                {
                    avid = await GetAidByBVAsync(BVRegex().Match(input).Groups[1].Value);
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
                else if (input.Contains("/medialist/") && input.Contains("business_id=") && input.Contains("business=space_collection")) //列表类型是合集
                {
                    string bizId = GetQueryString("business_id", input);
                    avid = $"listBizId:{bizId}";
                }
                else if (input.Contains("/medialist/") && input.Contains("business_id=") && input.Contains("business=space_series")) //列表类型是系列
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
                else if ((match = GlobalEpRegex().Match(input)).Success)
                {
                    string epId = match.Groups[1].Value;
                    avid = $"ep:{epId}";
                }
                else if ((match = BangumiMdRegex().Match(input)).Success)
                {
                    var mdId = match.Groups[1].Value;
                    avid = await GetAvIdAsync(mdId);
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
                avid = await GetAidByBVAsync(input[2..]);
            }
            else if (input.ToLower().StartsWith("av")) //av
            {
                avid = input.ToLower()[2..];
            }
            else if (input.StartsWith("ep"))
            {
                string epId = EpRegex2().Match(input).Groups[1].Value;
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
                >= 1024 * 1024 * 1024 => string.Format("{0:########0.00} GB", (double)fileSize / (1024 * 1024 * 1024)),
                >= 1024 * 1024 => string.Format("{0:####0.00} MB", (double)fileSize / (1024 * 1024)),
                >= 1024 => string.Format("{0:####0.00} KB", (double)fileSize / 1024),
                _ => string.Format("{0} bytes", fileSize)
            };
        }

        public static string FormatTime(int time, bool absolute = false)
        {
            TimeSpan ts = new(0, 0, time);
            string str = !absolute
                ? (ts.Hours.ToString("00") == "00" ? "" : ts.Hours.ToString("00") + "h") + ts.Minutes.ToString("00") + "m" + ts.Seconds.ToString("00") + "s"
                : ts.Hours.ToString("00") + ":" + ts.Minutes.ToString("00") + ":" + ts.Seconds.ToString("00");
            return str;
        }

        /// <summary>
        /// 通过avid检测是否为版权内容, 如果是的话返回ep:xx格式
        /// </summary>
        /// <param name="avid"></param>
        /// <returns></returns>
        public static async Task<string> FixAvidAsync(string avid)
        {
            if (!NumRegex().IsMatch(avid))
                return avid;
            string api = $"https://api.bilibili.com/x/web-interface/archive/stat?aid={avid}";
            string json = await GetWebSourceAsync(api);
            using var jDoc = JsonDocument.Parse(json);
            bool copyRight = jDoc.RootElement.GetProperty("data").GetProperty("copyright").GetInt32() == 2;
            if (copyRight)
            {
                api = $"https://api.bilibili.com/x/web-interface/view?aid={avid}";
                json = await GetWebSourceAsync(api);
                using var infoJson = JsonDocument.Parse(json);
                var data = infoJson.RootElement.GetProperty("data");
                if (data.TryGetProperty("redirect_url", out _) && data.GetProperty("redirect_url").ToString().Contains("bangumi"))
                {
                    var epId = RedirectRegex().Match(data.GetProperty("redirect_url").ToString()).Groups[1].Value;
                    return $"ep:{epId}";
                }
            }
            return avid;
        }

        public static async Task<string> GetAidByBVAsync(string bv)
        {
            if (bv.Length == 10)
            {
                // 能在本地就在本地
                string TABLE = "fZodR9XQDSUm21yCkr6zBqiveYah8bt4xsWpHnJE7jL5VG3guMTKNPAwcF";
                var BVChange = new Dictionary<char, byte>();
                byte[] S = { 9, 8, 1, 6, 2, 4 };
                long XOR = 177451812;
                long ADD = 8728348608;
                for (byte i = 0; i < 58; i++) BVChange.Add(TABLE[i], i);

                long T = 0;
                for (byte i = 0; i < 6; i++)
                {
                    T += (long)Math.Pow(58, i) * BVChange[bv[S[i]]];
                }
                return ((T - ADD) ^ XOR).ToString();
            }
            else
            {
                string api = $"https://api.bilibili.com/x/web-interface/archive/stat?bvid={bv}";
                string json = await GetWebSourceAsync(api);
                using var jDoc = JsonDocument.Parse(json);
                string aid = jDoc.RootElement.GetProperty("data").GetProperty("aid").ToString();
                return aid;
            }
        }

        public static async Task<string> GetEpidBySSIdAsync(string ssid)
        {
            string api = $"https://api.bilibili.com/pugv/view/web/season?season_id={ssid}";
            string json = await GetWebSourceAsync(api);
            using var jDoc = JsonDocument.Parse(json);
            string epId = jDoc.RootElement.GetProperty("data").GetProperty("episodes").EnumerateArray().First().GetProperty("id").ToString();
            return epId;
        }

        public static async Task<string> GetEpIdByBangumiSSIdAsync(string ssId)
		{
            string api = $"https://{Core.Config.EPHOST}/pgc/view/web/season?season_id={ssId}";
            string json = await GetWebSourceAsync(api);
            using var jDoc = JsonDocument.Parse(json);
            string epId = jDoc.RootElement.GetProperty("result").GetProperty("episodes").EnumerateArray().First().GetProperty("id").ToString();
            return epId;
        }

        public static async Task<string> GetEpIdByMDAsync(string mdId)
		{
            string api = $"https://api.bilibili.com/pgc/review/user?media_id={mdId}";
            string json = await GetWebSourceAsync(api);
            using var jDoc = JsonDocument.Parse(json);
            string epId = jDoc.RootElement.GetProperty("result").GetProperty("media").GetProperty("new_ep").GetProperty("id").ToString();
            return epId;
        }

        private static async Task RangeDownloadToTmpAsync(int id, string url, string tmpName, long fromPosition, long? toPosition, Action<int, long, long> onProgress, bool failOnRangeNotSupported = false)
        {
            DateTimeOffset? lastTime = File.Exists(tmpName) ? new FileInfo(tmpName).LastWriteTimeUtc : null;
            using var fileStream = new FileStream(tmpName, FileMode.Create);
            fileStream.Seek(0, SeekOrigin.End);
            var downloadedBytes = fromPosition + fileStream.Position;

            using var httpRequestMessage = new HttpRequestMessage();
            if (!url.Contains("platform=android_tv_yst") && !url.Contains("platform=android"))
                httpRequestMessage.Headers.TryAddWithoutValidation("Referer", "https://www.bilibili.com");
            httpRequestMessage.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0");
            httpRequestMessage.Headers.TryAddWithoutValidation("Cookie", Core.Config.COOKIE);
            httpRequestMessage.Headers.Range = new(downloadedBytes, toPosition);
            httpRequestMessage.Headers.IfRange = lastTime != null ? new(lastTime.Value) : null;
            httpRequestMessage.RequestUri = new(url);

            using var response = (await AppHttpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead)).EnsureSuccessStatusCode();

            if (response.StatusCode == HttpStatusCode.OK) // server doesn't response a partial content
            {
                if (failOnRangeNotSupported && (downloadedBytes > 0 || toPosition != null)) throw new NotSupportedException("Range request is not supported.");
                downloadedBytes = 0;
                fileStream.Seek(0, SeekOrigin.Begin);
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            var totalBytes = downloadedBytes + (response.Content.Headers.ContentLength ?? long.MaxValue - downloadedBytes);

            const int blockSize = 1048576 / 4;
            var buffer = new byte[blockSize];

            while (downloadedBytes < totalBytes)
            {
                var recevied = await stream.ReadAsync(buffer);
                if (recevied == 0) break;
                await fileStream.WriteAsync(buffer.AsMemory(0, recevied));
                await fileStream.FlushAsync();
                downloadedBytes += recevied;
                onProgress(id, downloadedBytes - fromPosition, totalBytes);
            }

            if (response.Content.Headers.ContentLength != null && (response.Content.Headers.ContentLength != new FileInfo(tmpName).Length))
                throw new Exception("Retry...");
        }

        /// <summary>
        /// 将下载地址强制转换为HTTP
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static string ReplaceUrl(string url)
        {
            if (McdnRegex().IsMatch(url))
            {
                LogDebug("对[*.mcdn.bilivideo.cn:xxx]域名不做处理");
                return url;
            }
            else
            {
                LogDebug("将https更改为http");
                return url.Replace("https:", "http:");
            }
        }

        public static async Task DownloadFile(string url, string path, bool aria2c, string aria2cArgs, bool forceHttp = false)
        {
            if (forceHttp) url = ReplaceUrl(url);
            LogDebug("Start downloading: {0}", url);
            if (!Directory.Exists(Path.GetDirectoryName(Path.GetFullPath(path))))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            }
            if (aria2c)
            {
                await BBDownAria2c.DownloadFileByAria2cAsync(url, path, aria2cArgs);
                if (File.Exists(path + ".aria2") || !File.Exists(path))
                    throw new Exception("aria2下载可能存在错误");
                Console.WriteLine();
                return;
            }
            int retry = 0;
            string tmpName = Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path) + ".tmp");
        reDown:
            try
            {
                using var progress = new ProgressBar();
                await RangeDownloadToTmpAsync(0, url, tmpName, 0, null, (_, downloaded, total) => progress.Report((double)downloaded / total));
                File.Move(tmpName, path, true);
            }
            catch (Exception)
            {
                if (++retry == 3) throw;
                goto reDown;
            }
        }

        public static async Task MultiThreadDownloadFileAsync(string url, string path, bool aria2c, string aria2cArgs, bool forceHttp = false)
        {
            if (forceHttp) url = ReplaceUrl(url);
            LogDebug("Start downloading: {0}", url);
            if (aria2c)
            {
                await BBDownAria2c.DownloadFileByAria2cAsync(url, path, aria2cArgs);
                if (File.Exists(path + ".aria2") || !File.Exists(path))
                    throw new Exception("aria2下载可能存在错误");
                Console.WriteLine();
                return;
            }
            long fileSize = await GetFileSizeAsync(url);
            LogDebug("文件大小：{0} bytes", fileSize);
            //已下载过, 跳过下载
            if (File.Exists(path) && new FileInfo(path).Length == fileSize)
            {
                LogDebug("文件已下载过, 跳过下载");
                return;
            }
            List<Clip> allClips = GetAllClips(url, fileSize);
            int total = allClips.Count;
            LogDebug("分段数量：{0}", total);
            ConcurrentDictionary<int, long> clipProgress = new();
            foreach (var i in allClips) clipProgress[i.index] = 0;

            using var progress = new ProgressBar();
            progress.Report(0);
            await Parallel.ForEachAsync(allClips, async (clip, _) =>
            {
                int retry = 0;
                string tmp = Path.Combine(Path.GetDirectoryName(path)!, clip.index.ToString("00000") + "_" + Path.GetFileNameWithoutExtension(path) + (Path.GetExtension(path).EndsWith(".mp4") ? ".vclip" : ".aclip"));
            reDown:
                try
                {
                    await RangeDownloadToTmpAsync(clip.index, url, tmp, clip.from, clip.to == -1 ? null : clip.to, (index, downloaded, _) =>
                    {
                        clipProgress[index] = downloaded;
                        progress.Report((double)clipProgress.Values.Sum() / fileSize);
                    }, true);
                }
                catch (NotSupportedException)
                {
                    if (++retry == 3) throw new Exception($"服务器可能并不支持多线程下载, 请使用 --multi-thread false 关闭多线程");
                    goto reDown;
                }
                catch (Exception)
                {
                    if (++retry == 3) throw new Exception($"Failed to download clip {clip.index}");
                    goto reDown;
                }
            });
        }

        //此函数主要是切片下载逻辑
        private static List<Clip> GetAllClips(string url, long fileSize)
        {
            List<Clip> clips = new();
            int index = 0;
            long counter = 0;
            int perSize = 10 * 1024 * 1024;
            while (fileSize > 0)
            {
                Clip c = new()
                {
                    index = index,
                    from = counter,
                    to = counter + perSize
                };
                //没到最后
                if (fileSize - perSize > 0)
                {
                    fileSize -= perSize;
                    counter += perSize + 1;
                    index++;
                    clips.Add(c);
                }
                //已到最后
                else
                {
                    c.to = -1;
                    clips.Add(c);
                    break;
                }
            }
            return clips;
        }

        /// <summary>
        /// 输入一堆已存在的文件, 合并到新文件
        /// </summary>
        /// <param name="files"></param>
        /// <param name="outputFilePath"></param>
        public static void CombineMultipleFilesIntoSingleFile(string[] files, string outputFilePath)
        {
            if (files.Length == 0) return;
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
            List<string> al = new();
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

        private static async Task<long> GetFileSizeAsync(string url)
        {
            using var httpRequestMessage = new HttpRequestMessage();
            if (!url.Contains("platform=android_tv_yst") && !url.Contains("platform=android"))
                httpRequestMessage.Headers.TryAddWithoutValidation("Referer", "https://www.bilibili.com");
            httpRequestMessage.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0");
            httpRequestMessage.Headers.TryAddWithoutValidation("Cookie", Core.Config.COOKIE);
            httpRequestMessage.RequestUri = new(url);
            var response = (await AppHttpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead)).EnsureSuccessStatusCode();
            long totalSizeBytes = response.Content.Headers.ContentLength ?? 0;

            return totalSizeBytes;
        }

        //重定向
        public static async Task<string> Get302(string url)
        {
            //this allows you to set the settings so that we can get the redirect url
            var handler = new HttpClientHandler()
            {
                AllowAutoRedirect = false
            };
            string redirectedUrl = "";
            using (HttpClient client = new(handler))
            using (HttpResponseMessage response = await client.GetAsync(url))
            using (HttpContent content = response.Content)
            {
                // ... Read the response to see if we have the redirected url
                if (response.StatusCode == System.Net.HttpStatusCode.Found)
                {
                    HttpResponseHeaders headers = response.Headers;
                    if (headers != null && headers.Location != null)
                    {
                        redirectedUrl = headers.Location.AbsoluteUri;
                    }
                }
            }

            return redirectedUrl;
        }

        private static char[] InvalidChars = "34,60,62,124,0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,58,42,63,92,47"
                .Split(',').Select(s => (char)int.Parse(s)).ToArray();

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

        public static async Task<string> GetLoginStatusAsync(string oauthKey)
        {
            string queryUrl = "https://passport.bilibili.com/qrcode/getLoginInfo";
            NameValueCollection postValues = new()
            {
                { "oauthKey", oauthKey },
                { "gourl", "https%3A%2F%2Fwww.bilibili.com%2F" }
            };
            byte[] responseArray = await (await AppHttpClient.PostAsync(queryUrl, new FormUrlEncodedContent(postValues.ToDictionary()))).Content.ReadAsByteArrayAsync();
            return Encoding.UTF8.GetString(responseArray);
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
            return string.Concat(MD5.HashData(Encoding.UTF8.GetBytes(toEncode)).Select(i => i.ToString("x2")).ToArray());
        }

        public static string GetTimeStamp(bool bflag)
        {
            DateTimeOffset ts = DateTimeOffset.Now;
            return bflag ? ts.ToUnixTimeSeconds().ToString() : ts.ToUnixTimeMilliseconds().ToString();
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
                string api = $"https://api.bilibili.com/x/player/v2?cid={cid}&aid={aid}";
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
            var envPath = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ??
                          Array.Empty<string>();
            return searchPath.Concat(envPath).Select(p => Path.Combine(p, name + fileExt)).FirstOrDefault(File.Exists);
        }

        public static string RSubString(string sub)
        {
            sub = sub[(sub.LastIndexOf("/") + 1)..];
            return sub[..sub.LastIndexOf(".")];
        }

        public static string GetMixinKey(string orig)
        {
            byte[] mixinKeyEncTab = new byte[]
            {
                46, 47, 18, 2, 53, 8, 23, 32, 15, 50, 10, 31, 58, 3, 45, 35,
                27, 43, 5, 49, 33, 9, 42, 19, 29, 28, 14, 39, 12, 38, 41, 13
            };

            var tmp = new StringBuilder();
            foreach (var index in mixinKeyEncTab)
            {
                tmp.Append(orig[index]);
            }
            return tmp.ToString();
        }

        public static async Task<(bool, string)> CheckLogin(string cookie)
        {
            try
            {
                var api = "https://api.bilibili.com/x/web-interface/nav";
                var source = await GetWebSourceAsync(api);
                var json = JsonDocument.Parse(source).RootElement;
                var is_login = json.GetProperty("data").GetProperty("isLogin").GetBoolean();
                var wbi_img = json.GetProperty("data").GetProperty("wbi_img");
                var wbi = GetMixinKey(RSubString(wbi_img.GetProperty("img_url").GetString()) + RSubString(wbi_img.GetProperty("sub_url").GetString()));
                LogDebug($"wbi: {wbi}");
                return (is_login, wbi);
            }
            catch (Exception)
            {
                return (false, "");
            }
        }

        [GeneratedRegex("av(\\d+)")]
        private static partial Regex AvRegex();
        [GeneratedRegex("[Bb][Vv](\\w+)")]
        private static partial Regex BVRegex();
        [GeneratedRegex("/ep(\\d+)")]
        private static partial Regex EpRegex();
        [GeneratedRegex("/ss(\\d+)")]
        private static partial Regex SsRegex();
        [GeneratedRegex("space\\.bilibili\\.com/(\\d+)")]
        private static partial Regex UidRegex();
        [GeneratedRegex("global\\.bilibili\\.com/play/\\d+/(\\d+)")]
        private static partial Regex GlobalEpRegex();
        [GeneratedRegex("bangumi/media/(md\\d+)")]
        private static partial Regex BangumiMdRegex();
        [GeneratedRegex("window.__INITIAL_STATE__=([\\s\\S].*?);\\(function\\(\\)")]
        private static partial Regex StateRegex();
        [GeneratedRegex("ep(\\d+)")]
        private static partial Regex EpRegex2();
        [GeneratedRegex("md(\\d+)")]
        private static partial Regex MdRegex();
        [GeneratedRegex("^\\d+$")]
        private static partial Regex NumRegex();
        [GeneratedRegex("ep(\\d+)")]
        private static partial Regex RedirectRegex();
        [GeneratedRegex("://.*mcdn\\.bilivideo\\.cn:\\d+")]
        private static partial Regex McdnRegex();
        [GeneratedRegex("(^|&)?(\\w+)=([^&]+)(&|$)?", RegexOptions.Compiled)]
        private static partial Regex QueryRegex();
        [GeneratedRegex("libavutil\\s+(\\d+)\\. +(\\d+)\\.")]
        private static partial Regex LibavutilRegex();
    }
}
