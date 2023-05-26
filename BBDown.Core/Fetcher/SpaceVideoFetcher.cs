using BBDown.Core.Entity;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using static BBDown.Core.Util.HTTPUtil;
using static BBDown.Core.Logger;

namespace BBDown.Core.Fetcher
{
    public class SpaceVideoFetcher : IFetcher
    {
        public static string WbiSign(string api, string wbi)
        {
            return $"{api}&w_rid=" + string.Concat(MD5.HashData(Encoding.UTF8.GetBytes(api + wbi)).Select(i => i.ToString("x2")).ToArray());
        }

        public async Task<VInfo> FetchAsync(string id)
        {
            id = id[4..];
            string[] tmp = id.Split("|");
            id = tmp[0];
            var wbi = tmp[1];
            // using the live API can bypass w_rid
            string userInfoApi = $"https://api.live.bilibili.com/live_user/v1/Master/info?uid={id}";
            string userName = GetValidFileName(JsonDocument.Parse(await GetWebSourceAsync(userInfoApi)).RootElement.GetProperty("data").GetProperty("info").GetProperty("uname").ToString(), ".", true);
            List<string> urls = new();
            int pageSize = 50;
            int pageNumber = 1;
            var api = WbiSign($"mid={id}&order=pubdate&pn={pageNumber}&ps={pageSize}&tid=0&wts={DateTimeOffset.Now.ToUnixTimeSeconds().ToString()}", wbi);
            api = $"https://api.bilibili.com/x/space/wbi/arc/search?{api}";
            string json = await GetWebSourceAsync(api);
            var infoJson = JsonDocument.Parse(json);
            var pages = infoJson.RootElement.GetProperty("data").GetProperty("list").GetProperty("vlist").EnumerateArray();
            foreach (var page in pages)
            {
                urls.Add($"https://www.bilibili.com/video/av{page.GetProperty("aid")}");
            }
            int totalCount = infoJson.RootElement.GetProperty("data").GetProperty("page").GetProperty("count").GetInt32();
            int totalPage = (int)Math.Ceiling((double)totalCount / pageSize);
            while (pageNumber < totalPage)
            {
                pageNumber++;
                urls.AddRange(await GetVideosByPageAsync(pageNumber, pageSize, id, wbi));
            }
            File.WriteAllText($"{userName}的投稿视频.txt", string.Join('\n', urls));
            Log("目前下载器不支持下载用户的全部投稿视频，不过程序已经获取到了该用户的全部投稿视频地址，你可以自行使用批处理脚本等手段调用本程序进行批量下载。如在Windows系统你可以使用如下代码：");
            Console.WriteLine();
            Console.WriteLine(@"@echo Off
For /F %%a in (urls.txt) Do (BBDown.exe ""%%a"")
pause");
            Console.WriteLine();
            throw new Exception("暂不支持该功能");
        }

        static async Task<List<string>> GetVideosByPageAsync(int pageNumber, int pageSize, string mid, string wbi)
        {
            List<string> urls = new();
            var api = WbiSign($"mid={mid}&order=pubdate&pn={pageNumber}&ps={pageSize}&tid=0&wts={DateTimeOffset.Now.ToUnixTimeSeconds().ToString()}", wbi);
            api = $"https://api.bilibili.com/x/space/wbi/arc/search?{api}";
            string json = await GetWebSourceAsync(api);
            var infoJson = JsonDocument.Parse(json);
            var pages = infoJson.RootElement.GetProperty("data").GetProperty("list").GetProperty("vlist").EnumerateArray();
            foreach (var page in pages)
            {
                urls.Add($"https://www.bilibili.com/video/av{page.GetProperty("aid")}");
            }
            return urls;
        }

        private static string GetValidFileName(string input, string re = ".", bool filterSlash = false)
        {
            string title = input;
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
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
    }
}
