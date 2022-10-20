using BBDown.Core.Entity;
using System.Text.Json;
using static BBDown.Core.Util.HTTPUtil;
using static BBDown.Core.Logger;

namespace BBDown.Core.Fetcher
{
    public class SpaceVideoFetcher : IFetcher
    {
        public async Task<VInfo> FetchAsync(string id)
        {
            id = id[4..];
            string userInfoApi = $"https://api.bilibili.com/x/space/acc/info?mid={id}&jsonp=jsonp";
            string userName = GetValidFileName(JsonDocument.Parse(await GetWebSourceAsync(userInfoApi)).RootElement.GetProperty("data").GetProperty("name").ToString(), ".", true);
            List<string> urls = new();
            int pageSize = 50;
            int pageNumber = 1;
            string api = $"https://api.bilibili.com/x/space/arc/search?mid={id}&ps={pageSize}&tid=0&pn={pageNumber}&keyword=&order=pubdate&jsonp=jsonp";
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
                urls.AddRange(await GetVideosByPageAsync(pageNumber, pageSize,  id));
            }
            File.WriteAllText($"{userName}的投稿视频.txt", string.Join('\n', urls));
            Log("目前下载器不支持下载用户的全部投稿视频, 不过程序已经获取到了该用户的全部投稿视频地址, 你可以自行使用批处理脚本等手段调用本程序进行批量下载。如在Windows系统你可以使用如下代码: ");
            Console.WriteLine();
            Console.WriteLine(@"@echo Off
For /F %%a in (urls.txt) Do (BBDown.exe ""%%a"")
pause");
            Console.WriteLine();
            throw new Exception("暂不支持该功能");
        }

        static async Task<List<string>> GetVideosByPageAsync(int pageNumber, int pageSize, string mid)
        {
            List<string> urls = new();
            string api = $"https://api.bilibili.com/x/space/arc/search?mid={mid}&ps={pageSize}&tid=0&pn={pageNumber}&keyword=&order=pubdate&jsonp=jsonp";
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
