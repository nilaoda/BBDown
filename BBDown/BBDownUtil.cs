using ICSharpCode.SharpZipLib.GZip;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using static BBDown.BBDownEntity;
using static BBDown.BBDownLogger;

namespace BBDown
{
    class BBDownUtil
    {
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
                    return GetAidByBV(Regex.Match(input, "BV(\\w+)").Groups[1].Value);
                }
                else if (input.Contains("video/bv"))
                {
                    return GetAidByBV(Regex.Match(input, "bv(\\w+)").Groups[1].Value);
                }
                else if (input.Contains("/cheese/"))
                {
                    string epId = "";
                    if (input.Contains("/ep"))
                    {
                        epId = Regex.Match(input, "/ep(\\d{1,})").Groups[1].Value;
                    }
                    else if(input.Contains("/ss"))
                    {
                        epId = GetEpidBySSId(Regex.Match(input, "/ss(\\d{1,})").Groups[1].Value);
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
                else
                {
                    string web = GetWebSource(input);
                    Regex regex = new Regex("window.__INITIAL_STATE__=([\\s\\S].*?);\\(function\\(\\)");
                    string json = regex.Match(web).Groups[1].Value;
                    string epId = JObject.Parse(json)["epList"][0]["id"].ToString();
                    return $"ep:{epId}";
                }
            }
            else if (input.StartsWith("BV"))
            {
                return GetAidByBV(input.Substring(2));
            }
            else if (input.StartsWith("bv"))
            {
                return GetAidByBV(input.Substring(2));
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
                string web = GetWebSource("https://www.bilibili.com/bangumi/play/" + input);
                Regex regex = new Regex("window.__INITIAL_STATE__=([\\s\\S].*?);\\(function\\(\\)");
                string json = regex.Match(web).Groups[1].Value;
                string epId = JObject.Parse(json)["epList"][0]["id"].ToString();
                return $"ep:{epId}";
            }
            else
            {
                throw new Exception("输入有误");
            }
        }

        public static String FormatFileSize(Double fileSize)
        {
            if (fileSize < 0)
            {
                throw new ArgumentOutOfRangeException("fileSize");
            }
            else if (fileSize >= 1024 * 1024 * 1024)
            {
                return string.Format("{0:########0.00} GB", ((Double)fileSize) / (1024 * 1024 * 1024));
            }
            else if (fileSize >= 1024 * 1024)
            {
                return string.Format("{0:####0.00} MB", ((Double)fileSize) / (1024 * 1024));
            }
            else if (fileSize >= 1024)
            {
                return string.Format("{0:####0.00} KB", ((Double)fileSize) / 1024);
            }
            else
            {
                return string.Format("{0} bytes", fileSize);
            }
        }

        public static String FormatTime(Int32 time)
        {
            TimeSpan ts = new TimeSpan(0, 0, time);
            string str = "";
            str = (ts.Hours.ToString("00") == "00" ? "" : ts.Hours.ToString("00") + "h") + ts.Minutes.ToString("00") + "m" + ts.Seconds.ToString("00") + "s";
            return str;
        }

        public static string GetWebSource(String url)
        {
            string htmlCode = string.Empty;
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = "GET";
            webRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.102 Safari/537.36 Edge/18.20221";
            webRequest.Headers.Add("Accept-Encoding", "gzip, deflate");
            webRequest.Headers.Add("Cookie", (url.Contains("/ep") || url.Contains("/ss")) ? Program.COOKIE + ";CURRENT_FNVAL=80;" : Program.COOKIE);
            if (url.Contains("api.bilibili.com/pgc/player/web/playurl") || url.Contains("api.bilibili.com/pugv/player/web/playurl"))
                webRequest.Headers.Add("Referer", "https://www.bilibili.com");
            webRequest.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            webRequest.KeepAlive = false;
            webRequest.AllowAutoRedirect = true;  //自动跳转
            LogDebug("获取网页内容：Url: {0}, Headers: {1}", url, webRequest.Headers);

            HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();
            if (webResponse.ContentEncoding != null
                && webResponse.ContentEncoding.ToLower() == "gzip") //如果使用了GZip则先解压
            {
                using (Stream streamReceive = webResponse.GetResponseStream())
                {
                    using (var zipStream =
                        new GZipInputStream(streamReceive))
                    {
                        using (StreamReader sr = new StreamReader(zipStream, Encoding.UTF8))
                        {
                            htmlCode = sr.ReadToEnd();
                        }
                    }
                }
            }
            else
            {
                using (Stream streamReceive = webResponse.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(streamReceive, Encoding.UTF8))
                    {
                        htmlCode = sr.ReadToEnd();
                    }
                }
            }

            if (webResponse != null)
            {
                webResponse.Close();
            }
            if (webRequest != null)
            {
                webRequest.Abort();
            }

            return htmlCode;
        }

        public static string GetPostResponse(string Url, byte[] postData)
        {
            LogDebug("Post to: {0}, data: {1}", Url, Convert.ToBase64String(postData));
            string htmlCode = string.Empty;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            request.Method = "POST";
            request.ContentType = "application/grpc";
            request.ContentLength = postData.Length;
            request.UserAgent = "Dalvik/2.1.0 (Linux; U; Android 6.0.1; oneplus a5010 Build/V417IR) 6.10.0 os/android model/oneplus a5010 mobi_app/android build/6100500 channel/bili innerVer/6100500 osVer/6.0.1 network/2";
            request.Headers.Add("Cookie", Program.COOKIE);
            Stream myRequestStream = request.GetRequestStream();
            myRequestStream.Write(postData);
            myRequestStream.Close();
            HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse();
            if (webResponse.ContentEncoding != null
                && webResponse.ContentEncoding.ToLower() == "gzip") //如果使用了GZip则先解压
            {
                using (Stream streamReceive = webResponse.GetResponseStream())
                {
                    using (var zipStream =
                        new GZipInputStream(streamReceive))
                    {
                        using (StreamReader sr = new StreamReader(zipStream, Encoding.UTF8))
                        {
                            htmlCode = sr.ReadToEnd();
                        }
                    }
                }
            }
            else
            {
                using (Stream streamReceive = webResponse.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(streamReceive, Encoding.UTF8))
                    {
                        htmlCode = sr.ReadToEnd();
                    }
                }
            }

            if (webResponse != null)
            {
                webResponse.Close();
            }
            if (request != null)
            {
                request.Abort();
            }
            return htmlCode;
        }

        public static string GetAidByBV(string bv)
        {
            string api = $"https://api.bilibili.com/x/web-interface/archive/stat?bvid={bv}";
            string json = GetWebSource(api);
            string aid = JObject.Parse(json)["data"]["aid"].ToString();
            return aid;
        }

        public static string GetEpidBySSId(string ssid)
        {
            string api = $"https://api.bilibili.com/pugv/view/web/season?season_id={ssid}";
            string json = GetWebSource(api);
            string epId = JObject.Parse(json)["data"]["episodes"][0]["id"].ToString();
            return epId;
        }

        public static async Task DownloadFile(string url, string path, bool aria2c)
        {
            LogDebug("Start downloading: {0}", url);
            if (aria2c)
            {
                BBDownAria2c.DownloadFileByAria2c(url, path);
                if (File.Exists(path + ".aria2") || !File.Exists(path))
                    throw new Exception("aria2下载可能存在错误");
                Console.WriteLine();
                return;
            }
            string tmpName = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + ".tmp");
            using (var progress = new ProgressBar())
            {
                long totalLength = -1;
                WebClient client = new WebClient();
                if (!url.Contains("platform=android_tv_yst"))
                    client.Headers.Add("Referer", "https://www.bilibili.com");
                client.Headers.Add("User-Agent", "Mozilla/5.0");
                client.Headers.Add("Cookie", Program.COOKIE);
                client.Credentials = CredentialCache.DefaultCredentials;
                Uri uri = new Uri(url);
                client.DownloadProgressChanged += delegate (object sender, DownloadProgressChangedEventArgs e)
                {
                    if (totalLength == -1) totalLength = e.TotalBytesToReceive;
                    progress.Report((double)e.BytesReceived / e.TotalBytesToReceive);
                };
                await client.DownloadFileTaskAsync(uri, tmpName);
                if (new FileInfo(tmpName).Length == totalLength)
                    File.Move(tmpName, path, true);
                else
                    throw new Exception("文件下载可能不完整, 请重新下载");
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

        public static async Task MultiThreadDownloadFileAsync(string url, string path, bool aria2c)
        {
            if (aria2c)
            {
                BBDownAria2c.DownloadFileByAria2c(url, path);
                if (File.Exists(path + ".aria2") || !File.Exists(path))
                    throw new Exception("aria2下载可能存在错误");
                Console.WriteLine();
                return;
            }
            long fileSize = GetFileSize(url);
            LogDebug("文件大小：{0} bytes", fileSize);
            List<Clip> allClips = GetAllClips(url, fileSize);
            int total = allClips.Count;
            LogDebug("分段数量：{0}", total);
            long done = 0;
            using (var progress = new ProgressBar())
            {
                progress.Report(0);
                await RunWithMaxDegreeOfConcurrency(8, allClips, async clip =>
                 {
                 string tmp = Path.Combine(Path.GetDirectoryName(path), clip.index.ToString("00000") + "_" + Path.GetFileNameWithoutExtension(path) + (Path.GetExtension(path).EndsWith(".mp4") ? ".vclip" : ".aclip"));
                     if (!(File.Exists(tmp) && new FileInfo(tmp).Length == clip.to - clip.from + 1))
                     {
                     reDown:
                         try
                         {
                             using (var client = new HttpClient())
                             {
                                 client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
                                 client.DefaultRequestHeaders.Add("Cookie", Program.COOKIE);
                                 if (clip.to != -1)
                                     client.DefaultRequestHeaders.Add("Range", $"bytes={clip.from}-{clip.to}");
                                 else
                                     client.DefaultRequestHeaders.Add("Range", $"bytes={clip.from}-");
                                 if (!url.Contains("platform=android_tv_yst"))
                                     client.DefaultRequestHeaders.Referrer = new Uri("https://www.bilibili.com");

                                 var response = await client.GetAsync(url);

                                 using (var stream = await response.Content.ReadAsStreamAsync())
                                 {
                                     using (var fileStream = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                                     {
                                         var buffer = new byte[8192];
                                         int bytesRead;
                                         while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                         {
                                             await fileStream.WriteAsync(buffer, 0, bytesRead);
                                             done += bytesRead;
                                             progress.Report((double)done / fileSize);
                                         }
                                         //await stream.CopyToAsync(fileStream);
                                     }
                                 }
                             }
                         }
                         catch { goto reDown; }
                     }
                     else
                     {
                         done += new FileInfo(tmp).Length;
                         progress.Report((double)done / fileSize);
                     }
                 });
                /*//多线程设置
                ParallelOptions parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 8
                };
                Parallel.ForEach(allClips, parallelOptions, async clip =>
                {
                    string tmp = Path.Combine(Path.GetDirectoryName(path), clip.index.ToString("00000") + "_" + Path.GetFileNameWithoutExtension(path) + (Path.GetExtension(path).EndsWith(".mp4") ? ".vclip" : ".aclip"));
                    if (!(File.Exists(tmp) && new FileInfo(tmp).Length == clip.to - clip.from + 1))
                    {
                    reDown:
                        try
                        {
                            *//*HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                            request.Timeout = 30000;
                            request.ReadWriteTimeout = 30000; //重要
                            request.AllowAutoRedirect = true;
                            request.KeepAlive = false;
                            request.Method = "GET";
                            if (!url.Contains("platform=android_tv_yst"))
                                request.Referer = "https://www.bilibili.com";
                            request.UserAgent = "Mozilla/5.0";
                            request.Headers.Add("Cookie", Program.COOKIE);
                            if (clip.to != -1)
                                request.AddRange("bytes", clip.from, clip.to);
                            else
                                request.AddRange("bytes", clip.from);
                            using (var response = (HttpWebResponse)request.GetResponse())
                            {
                                using (var responseStream = response.GetResponseStream())
                                {
                                    using (var stream = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.Write))
                                    {
                                        byte[] bArr = new byte[1024];
                                        int size = responseStream.Read(bArr, 0, (int)bArr.Length);
                                        while (size > 0)
                                        {
                                            stream.Write(bArr, 0, size);
                                            done += size;
                                            progress.Report((double)done / fileSize);
                                            size = responseStream.Read(bArr, 0, (int)bArr.Length);
                                        }
                                    }
                                }
                            }*//*
                        }
                        catch { goto reDown; }
                    }
                    else
                    {
                        done += new FileInfo(tmp).Length;
                        progress.Report((double)done / fileSize);
                    }
                });*/
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
            ArrayList al = new ArrayList();
            StringBuilder sb = new StringBuilder();
            DirectoryInfo d = new DirectoryInfo(dir);
            foreach (FileInfo fi in d.GetFiles())
            {
                if (fi.Extension.ToUpper() == ext.ToUpper())
                {
                    al.Add(fi.FullName);
                }
            }
            string[] res = (string[])al.ToArray(typeof(string));
            Array.Sort(res); //排序
            return res;
        }

        private static long GetFileSize(string url)
        {
            WebClient webClient = new WebClient();
            if (!url.Contains("platform=android_tv_yst"))
                webClient.Headers.Add("Referer", "https://www.bilibili.com");
            webClient.Headers.Add("User-Agent", "Mozilla/5.0");
            webClient.OpenRead(url);
            long totalSizeBytes = Convert.ToInt64(webClient.ResponseHeaders["Content-Length"]);

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

        public static string GetLoginStatus(string oauthKey)
        {
            string queryUrl = "https://passport.bilibili.com/qrcode/getLoginInfo";
            WebClient webClient = new WebClient();
            NameValueCollection postValues = new NameValueCollection();
            postValues.Add("oauthKey", oauthKey);
            postValues.Add("gourl", "https%3A%2F%2Fwww.bilibili.com%2F");
            byte[] responseArray = webClient.UploadValues(queryUrl, postValues);
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
            NameValueCollection httpValueCollection = HttpUtility.ParseQueryString(String.Empty);
            httpValueCollection.Add(nameValueCollection);
            return httpValueCollection.ToString();
        }

        public static NameValueCollection GetTVLoginParms()
        {
            NameValueCollection sb = new NameValueCollection();
            DateTime now = DateTime.Now;
            string deviceId = GetRandomString(20);
            string buvid = GetRandomString(37);
            string fingerprint = $"{now.ToString("yyyyMMddHHmmssfff")}{GetRandomString(45)}";
            sb.Add("appkey","4409e2ce8ffd12b8");
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
