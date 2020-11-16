using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static BBDown.BBDownLogger;
using static BBDown.BBDownUtil;

namespace BBDown
{
    class BBDownSpaceVideoFetcher : IFetcher
    {
        public BBDownVInfo Fetch(string id)
        {
            id = id.Substring(4);
            string userInfoApi = $"https://api.bilibili.com/x/space/acc/info?mid={id}&jsonp=jsonp";
            string userName = GetValidFileName(JObject.Parse(GetWebSource(userInfoApi))["data"]["name"].ToString());
            List<string> urls = new List<string>();
            int pageSize = 100;
            int pageNumber = 1;
            string api = $"https://api.bilibili.com/x/space/arc/search?mid={id}&ps={pageSize}&tid=0&pn={pageNumber}&keyword=&order=pubdate&jsonp=jsonp";
            string json = GetWebSource(api);
            JObject infoJson = JObject.Parse(json);
            JArray pages = JArray.Parse(infoJson["data"]["list"]["vlist"].ToString());
            foreach (JObject page in pages)
            {
                urls.Add($"https://www.bilibili.com/video/av{page["aid"].ToString()}");
            }
            int totalCount = infoJson["data"]["page"]["count"].Value<int>();
            int totalPage = (int)Math.Ceiling((double)totalCount / pageSize);
            while (pageNumber < totalPage)
            {
                pageNumber++;
                urls.AddRange(GetVideosByPage(pageNumber, pageSize,  id));
            }
            File.WriteAllText($"{userName}的投稿视频.txt", string.Join('\n', urls));
            Log("目前下载器不支持下载用户的全部投稿视频，不过程序已经获取到了该用户的全部投稿视频地址，你可以自行使用批处理脚本等手段调用本程序进行批量下载。如在Windows系统你可以使用如下代码：");
            Console.WriteLine();
            Console.WriteLine(@"@echo Off
For / F %%a in (urls.txt) Do (BBDown.exe ""%%a"")
pause");
            Console.WriteLine();
            throw new Exception("暂不支持该功能");
        }

        List<string> GetVideosByPage(int pageNumber, int pageSize, string mid)
        {
            List<string> urls = new List<string>();
            string api = $"https://api.bilibili.com/x/space/arc/search?mid={mid}&ps={pageSize}&tid=0&pn={pageNumber}&keyword=&order=pubdate&jsonp=jsonp";
            string json = GetWebSource(api);
            JObject infoJson = JObject.Parse(json);
            JArray pages = JArray.Parse(infoJson["data"]["list"]["vlist"].ToString());
            foreach (JObject page in pages)
            {
                urls.Add($"https://www.bilibili.com/video/av{page["aid"].ToString()}");
            }
            return urls;
        }
    }
}
