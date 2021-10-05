using ICSharpCode.SharpZipLib.GZip;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using static BBDown.BBDownEntity;
using static BBDown.BBDownLogger;

namespace BBDown
{
    static class BBDownUtil
    {
        public static readonly HttpClient AppHttpClient = new(new HttpClientHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.All
        });

        public static async Task CheckUpdateAsync()
        {
            try
            {
                var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
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
            if (input.StartsWith("http"))
            {
                if (input.Contains("b23.tv"))
                    input = await Get302(input);
                if (input.Contains("video/av"))
                {
                    return Regex.Match(input, "av(\\d{1,})").Groups[1].Value;
                }
                else if (input.Contains("video/BV"))
                {
                    return await GetAidByBVAsync(Regex.Match(input, "BV(\\w+)").Groups[1].Value);
                }
                else if (input.Contains("video/bv"))
                {
                    return await GetAidByBVAsync(Regex.Match(input, "bv(\\w+)").Groups[1].Value);
                }
                else if (input.Contains("/cheese/"))
                {
                    string epId = "";
                    if (input.Contains("/ep"))
                    {
                        epId = Regex.Match(input, "/ep(\\d{1,})").Groups[1].Value;
                    }
                    else if (input.Contains("/ss"))
                    {
                        epId = await GetEpidBySSIdAsync(Regex.Match(input, "/ss(\\d{1,})").Groups[1].Value);
                    }
                    return $"cheese:{epId}";
                }
                else if (input.Contains("/ep"))
                {
                    string epId = Regex.Match(input, "/ep(\\d{1,})").Groups[1].Value;
                    return $"ep:{epId}";
                }
                else if (input.Contains("/space.bilibili.com/"))
                {
                    string mid = Regex.Match(input, "space.bilibili.com/(\\d{1,})").Groups[1].Value;
                    return $"mid:{mid}";
                }
                else if (input.Contains("ep_id="))
                {
                    string epId = GetQueryString("ep_id", input);
                    return $"ep:{epId}";
                }
                else if (Regex.IsMatch(input, "global.bilibili.com/play/\\d+/(\\d+)"))
                {
                    string epId = Regex.Match(input, "global.bilibili.com/play/\\d+/(\\d+)").Groups[1].Value;
                    return $"ep:{epId}";
                }
                else
                {
                    string web = await GetWebSourceAsync(input);
                    Regex regex = new Regex("window.__INITIAL_STATE__=([\\s\\S].*?);\\(function\\(\\)");
                    string json = regex.Match(web).Groups[1].Value;
                    using var jDoc = JsonDocument.Parse(json);
                    string epId = jDoc.RootElement.GetProperty("epList").EnumerateArray().First().GetProperty("id").ToString();
                    return $"ep:{epId}";
                }
            }
            else if (input.StartsWith("BV"))
            {
                return await GetAidByBVAsync(input.Substring(2));
            }
            else if (input.StartsWith("bv"))
            {
                return await GetAidByBVAsync(input.Substring(2));
            }
            else if (input.ToLower().StartsWith("av")) //av
            {
                return input.ToLower().Substring(2);
            }
            else if (input.StartsWith("ep"))
            {
                string epId = Regex.Match(input, "ep(\\d{1,})").Groups[1].Value;
                return $"ep:{epId}";
            }
            else if (input.StartsWith("ss"))
            {
                string web = await GetWebSourceAsync("https://www.bilibili.com/bangumi/play/" + input);
                Regex regex = new Regex("window.__INITIAL_STATE__=([\\s\\S].*?);\\(function\\(\\)");
                string json = regex.Match(web).Groups[1].Value;
                using var jDoc = JsonDocument.Parse(json);
                string epId = jDoc.RootElement.GetProperty("epList").EnumerateArray().First().GetProperty("id").ToString();
                return $"ep:{epId}";
            }
            else
            {
                throw new Exception("输入有误");
            }
        }

        public static string FormatFileSize(double fileSize)
        {
            if (fileSize < 0)
            {
                throw new ArgumentOutOfRangeException("fileSize");
            }
            else if (fileSize >= 1024 * 1024 * 1024)
            {
                return string.Format("{0:########0.00} GB", ((double)fileSize) / (1024 * 1024 * 1024));
            }
            else if (fileSize >= 1024 * 1024)
            {
                return string.Format("{0:####0.00} MB", ((double)fileSize) / (1024 * 1024));
            }
            else if (fileSize >= 1024)
            {
                return string.Format("{0:####0.00} KB", ((double)fileSize) / 1024);
            }
            else
            {
                return string.Format("{0} bytes", fileSize);
            }
        }

        public static string FormatTime(int time)
        {
            TimeSpan ts = new TimeSpan(0, 0, time);
            string str = "";
            str = (ts.Hours.ToString("00") == "00" ? "" : ts.Hours.ToString("00") + "h") + ts.Minutes.ToString("00") + "m" + ts.Seconds.ToString("00") + "s";
            return str;
        }

        public static async Task<string> GetWebSourceAsync(string url)
        {
            string htmlCode = string.Empty;
            try
            {
                using var webRequest = new HttpRequestMessage(HttpMethod.Get, url);
                webRequest.Headers.Add("UserAgent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.108 Safari/537.36");
                webRequest.Headers.Add("Accept-Encoding", "gzip, deflate");
                webRequest.Headers.Add("Cookie", (url.Contains("/ep") || url.Contains("/ss")) ? Program.COOKIE + ";CURRENT_FNVAL=80;" : Program.COOKIE);
                if (url.Contains("api.bilibili.com/pgc/player/web/playurl") || url.Contains("api.bilibili.com/pugv/player/web/playurl"))
                    webRequest.Headers.Add("Referer", "https://www.bilibili.com");
                webRequest.Headers.CacheControl = CacheControlHeaderValue.Parse("no-cache");
                webRequest.Headers.Connection.Clear();

                LogDebug("获取网页内容：Url: {0}, Headers: {1}", url, webRequest.Headers);
                var webResponse = (await AppHttpClient.SendAsync(webRequest, HttpCompletionOption.ResponseHeadersRead)).EnsureSuccessStatusCode();

                htmlCode = await webResponse.Content.ReadAsStringAsync();
            }
            catch (Exception)
            {
                ;
            }
            LogDebug("Response: {0}", htmlCode);
            return htmlCode;
        }

        public static async Task<string> GetPostResponseAsync(string Url, byte[] postData)
        {
            LogDebug("Post to: {0}, data: {1}", Url, Convert.ToBase64String(postData));
            string htmlCode = string.Empty;
            using HttpRequestMessage request = new(HttpMethod.Post, Url);
            request.Headers.Add("ContentType", "application/grpc");
            request.Headers.Add("ContentLength", postData.Length.ToString());
            request.Headers.Add("UserAgent", "Dalvik/2.1.0 (Linux; U; Android 6.0.1; oneplus a5010 Build/V417IR) 6.10.0 os/android model/oneplus a5010 mobi_app/android build/6100500 channel/bili innerVer/6100500 osVer/6.0.1 network/2");
            request.Headers.Add("Cookie", Program.COOKIE);
            request.Content = new ByteArrayContent(postData);
            var webResponse = await AppHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            Stream myRequestStream = await webResponse.Content.ReadAsStreamAsync();
            htmlCode = await webResponse.Content.ReadAsStringAsync();
            return htmlCode;
        }

        public static async Task<string> GetAidByBVAsync(string bv)
        {
            string api = $"https://api.bilibili.com/x/web-interface/archive/stat?bvid={bv}";
            string json = await GetWebSourceAsync(api);
            using var jDoc = JsonDocument.Parse(json);
            string aid = jDoc.RootElement.GetProperty("data").GetProperty("aid").ToString();
            return aid;
        }

        public static async Task<string> GetEpidBySSIdAsync(string ssid)
        {
            string api = $"https://api.bilibili.com/pugv/view/web/season?season_id={ssid}";
            string json = await GetWebSourceAsync(api);
            using var jDoc = JsonDocument.Parse(json);
            string epId = jDoc.RootElement.GetProperty("data").GetProperty("episodes").EnumerateArray().First().GetProperty("id").ToString();
            return epId;
        }

        private static async Task RangeDownloadToTmpAsync(int id, string url, string tmpName, long fromPosition, long? toPosition, Action<int, long, long> onProgress, bool failOnRangeNotSupported = false)
        {
            var lastTime = File.Exists(tmpName) ? new FileInfo(tmpName).LastWriteTimeUtc : DateTimeOffset.MinValue;
            using (var fileStream = new FileStream(tmpName, FileMode.OpenOrCreate))
            {
                fileStream.Seek(0, SeekOrigin.End);
                var downloadedBytes = fromPosition + fileStream.Position;

                using var httpRequestMessage = new HttpRequestMessage();
                if (!url.Contains("platform=android_tv_yst") && !url.Contains("platform=android"))
                    httpRequestMessage.Headers.Add("Referer", "https://www.bilibili.com");
                httpRequestMessage.Headers.Add("User-Agent", "Mozilla/5.0");
                httpRequestMessage.Headers.Add("Cookie", Program.COOKIE);
                httpRequestMessage.Headers.Range = new(downloadedBytes, toPosition);
                httpRequestMessage.Headers.IfRange = new(lastTime);
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
            }
        }

        public static async Task DownloadFile(string url, string path, bool aria2c, string aria2cProxy)
        {
            LogDebug("Start downloading: {0}", url);
            if (aria2c)
            {
                BBDownAria2c.DownloadFileByAria2c(url, path, aria2cProxy);
                if (File.Exists(path + ".aria2") || !File.Exists(path))
                    throw new Exception("aria2下载可能存在错误");
                Console.WriteLine();
                return;
            }
            string tmpName = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + ".tmp");
            using (var progress = new ProgressBar())
            {
                await RangeDownloadToTmpAsync(0, url, tmpName, 0, null, (_, downloaded, total) => progress.Report((double)downloaded / total));
                File.Move(tmpName, path, true);
            }
        }

        //https://stackoverflow.com/a/25877042
        public static async Task RunWithMaxDegreeOfConcurrency<T>(
            int maxDegreeOfConcurrency, IEnumerable<T> collection, Func<T, Task> taskFactory)
        {
            var activeTasks = new List<Task>(maxDegreeOfConcurrency);
            foreach (var task in collection.Select(taskFactory))
            {
                activeTasks.Add(task);
                if (activeTasks.Count == maxDegreeOfConcurrency)
                {
                    await Task.WhenAny(activeTasks.ToArray());
                    //observe exceptions here
                    activeTasks.RemoveAll(t => t.IsCompleted);
                }
            }
            await Task.WhenAll(activeTasks.ToArray()).ContinueWith(t =>
            {
                //observe exceptions in a manner consistent with the above   
            });
        }

        public static async Task MultiThreadDownloadFileAsync(string url, string path, bool aria2c, string aria2cProxy)
        {
            if (aria2c)
            {
                BBDownAria2c.DownloadFileByAria2c(url, path, aria2cProxy);
                if (File.Exists(path + ".aria2") || !File.Exists(path))
                    throw new Exception("aria2下载可能存在错误");
                Console.WriteLine();
                return;
            }
            long fileSize = await GetFileSizeAsync(url);
            LogDebug("文件大小：{0} bytes", fileSize);
            List<Clip> allClips = GetAllClips(url, fileSize);
            int total = allClips.Count;
            LogDebug("分段数量：{0}", total);
            ConcurrentDictionary<int, long> clipProgress = new();
            foreach (var i in allClips) clipProgress[i.index] = 0;

            using (var progress = new ProgressBar())
            {
                progress.Report(0);
                await RunWithMaxDegreeOfConcurrency(8, allClips, async clip =>
                {
                    int retry = 0;
                    string tmp = Path.Combine(Path.GetDirectoryName(path), clip.index.ToString("00000") + "_" + Path.GetFileNameWithoutExtension(path) + (Path.GetExtension(path).EndsWith(".mp4") ? ".vclip" : ".aclip"));
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
                        throw;
                    }
                    catch
                    {
                        if (++retry == 3) throw new Exception($"Failed to download clip {clip.index}");
                        goto reDown;
                    }
                });
            }
        }

        //此函数主要是切片下载逻辑
        private static List<Clip> GetAllClips(string url, long fileSize)
        {
            List<Clip> clips = new List<Clip>();
            int index = 0;
            long counter = 0;
            int perSize = 5 * 1024 * 1024;
            while (fileSize > 0)
            {
                Clip c = new Clip();
                c.index = index;
                c.from = counter;
                c.to = c.from + perSize;
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
        /// 输入一堆已存在的文件，合并到新文件
        /// </summary>
        /// <param name="files"></param>
        /// <param name="outputFilePath"></param>
        public static void CombineMultipleFilesIntoSingleFile(string[] files, string outputFilePath)
        {
            if (files.Length == 1)
            {
                FileInfo fi = new FileInfo(files[0]);
                fi.MoveTo(outputFilePath);
                return;
            }

            if (!Directory.Exists(Path.GetDirectoryName(outputFilePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));

            string[] inputFilePaths = files;
            using (var outputStream = File.Create(outputFilePath))
            {
                foreach (var inputFilePath in inputFilePaths)
                {
                    if (inputFilePath == "")
                        continue;
                    using (var inputStream = File.OpenRead(inputFilePath))
                    {
                        // Buffer size can be passed as the second argument.
                        inputStream.CopyTo(outputStream);
                    }
                    //Console.WriteLine("The file {0} has been processed.", inputFilePath);
                }
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
            List<string> al = new List<string>();
            StringBuilder sb = new StringBuilder();
            DirectoryInfo d = new DirectoryInfo(dir);
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
                httpRequestMessage.Headers.Add("Referer", "https://www.bilibili.com");
            httpRequestMessage.Headers.Add("User-Agent", "Mozilla/5.0");
            httpRequestMessage.Headers.Add("Cookie", Program.COOKIE);
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
            string redirectedUrl = null;
            using (HttpClient client = new HttpClient(handler))
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

        public static string GetValidFileName(string input, string re = ".")
        {
            string title = input;
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                title = title.Replace(invalidChar.ToString(), re);
            }
            return title;
        }


        /// <summary>    
        /// 获取url字符串参数，返回参数值字符串    
        /// </summary>    
        /// <param name="name">参数名称</param>    
        /// <param name="url">url字符串</param>    
        /// <returns></returns>    
        public static string GetQueryString(string name, string url)
        {
            Regex re = new Regex(@"(^|&)?(\w+)=([^&]+)(&|$)?", System.Text.RegularExpressions.RegexOptions.Compiled);
            MatchCollection mc = re.Matches(url);
            foreach (Match m in mc)
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
            NameValueCollection postValues = new NameValueCollection();
            postValues.Add("oauthKey", oauthKey);
            postValues.Add("gourl", "https%3A%2F%2Fwww.bilibili.com%2F");
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
            MD5 md5 = MD5.Create();
            byte[] bs = Encoding.UTF8.GetBytes(toEncode);
            byte[] hs = md5.ComputeHash(bs);
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hs)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }

        public static string GetTimeStamp(bool bflag)
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            string ret = string.Empty;
            if (bflag)
                ret = Convert.ToInt64(ts.TotalSeconds).ToString();
            else
                ret = Convert.ToInt64(ts.TotalMilliseconds).ToString();

            return ret;
        }

        //https://stackoverflow.com/questions/1344221/how-can-i-generate-random-alphanumeric-strings
        private static Random random = new Random();
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
            return httpValueCollection.ToString();
        }

        public static Dictionary<string, string> ToDictionary(this NameValueCollection nameValueCollection)
        {
            var dict = new Dictionary<string, string>();
            foreach (var key in nameValueCollection.AllKeys)
            {
                dict[key] = nameValueCollection[key];
            }
            return dict;
        }

        public static string GetMaxQn()
        {
            return Program.qualitys.Keys.First();
        }

        public static NameValueCollection GetTVLoginParms()
        {
            NameValueCollection sb = new();
            DateTime now = DateTime.Now;
            string deviceId = GetRandomString(20);
            string buvid = GetRandomString(37);
            string fingerprint = $"{now.ToString("yyyyMMddHHmmssfff")}{GetRandomString(45)}";
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
    }
}
