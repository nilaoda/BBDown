using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static BBDown.BBDownLogger;
using static BBDown.BBDownUtil;

namespace BBDown
{
    class BBDownSpaceVideoFetcher : IFetcher
    {
        public async Task<BBDownVInfo> FetchAsync(string id)
        {
            id = id.Substring(4);
            string userInfoApi = $"https://api.bilibili.com/x/space/acc/info?mid={id}&jsonp=jsonp";
            string userName = GetValidFileName(JsonDocument.Parse(await GetWebSourceAsync(userInfoApi)).RootElement.GetProperty("data").GetProperty("name").ToString());
            List<string> urls = new List<string>();
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
            Log("目前下载器不支持下载用户的全部投稿视频，不过程序已经获取到了该用户的全部投稿视频地址，你可以自行使用批处理脚本等手段调用本程序进行批量下载。如在Windows系统你可以使用如下代码：");
            Console.WriteLine();
            Console.WriteLine(@"@echo Off
For /F %%a in (urls.txt) Do (BBDown.exe ""%%a"")
pause");
            Console.WriteLine();
            throw new Exception("暂不支持该功能");
        }

        async Task<List<string>> GetVideosByPageAsync(int pageNumber, int pageSize, string mid)
        {
            List<string> urls = new List<string>();
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
    }
}
