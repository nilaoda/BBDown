using BBDown.Core.Entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static BBDown.Core.Entity.Entity;
using static BBDown.Core.Util.HTTPUtil;
using static BBDown.Core.Logger;

namespace BBDown.Core.Fetcher
{
    public class SpaceVideoFetcher : IFetcher
    {
        public async Task<VInfo> FetchAsync(string id)
        {
            id = id.Substring(4);
            string userInfoApi = $"https://api.bilibili.com/x/space/acc/info?mid={id}&jsonp=jsonp";
            string userName = GetValidFileName(JsonDocument.Parse(await GetWebSourceAsync(userInfoApi)).RootElement.GetProperty("data").GetProperty("name").ToString(), ".", true);
            List<string> aidList = new List<string>();
            int pageSize = 50;
            int pageNumber = 1;
            string api = $"https://api.bilibili.com/x/space/arc/search?mid={id}&ps={pageSize}&tid=0&pn={pageNumber}&keyword=&order=pubdate&jsonp=jsonp";
            string json = await GetWebSourceAsync(api);
            var infoJson = JsonDocument.Parse(json);
            var pages = infoJson.RootElement.GetProperty("data").GetProperty("list").GetProperty("vlist").EnumerateArray();
            int totalCount = infoJson.RootElement.GetProperty("data").GetProperty("page").GetProperty("count").GetInt32();
            int totalPage = (int)Math.Ceiling((double)totalCount / pageSize);

            while (pageNumber <= totalPage)
            {
                aidList.AddRange(await GetVideosByPageAsync(pageNumber, pageSize, id));
                pageNumber++;
            }

            List<Page> pagesInfo = new List<Page>();
            int index = 1;
            foreach (var aid in aidList)
            {
                var tmpInfo = await new NormalInfoFetcher().FetchAsync(aid);
                Thread.Sleep(100);
                foreach (var item in tmpInfo.PagesInfo)
                {
                    Page p = new Page(index++, item);
                    if (!pagesInfo.Contains(p)) pagesInfo.Add(p);
                }
            }

            var info = new VInfo();
            // 空间标题，取作者名称
            info.Title = userName;
            // todo, PubTime 理论应该跟着视频走
            info.PubTime = "";  
            info.PagesInfo = pagesInfo;
            info.IsBangumi = false;
            info.IsFavList = true;

            return info;


        }

        async Task<List<string>> GetVideosByPageAsync(int pageNumber, int pageSize, string mid)
        {
            List<string> aidList = new List<string>();
            string api = $"https://api.bilibili.com/x/space/arc/search?mid={mid}&ps={pageSize}&tid=0&pn={pageNumber}&keyword=&order=pubdate&jsonp=jsonp";
            string json = await GetWebSourceAsync(api);
            var infoJson = JsonDocument.Parse(json);
            var pages = infoJson.RootElement.GetProperty("data").GetProperty("list").GetProperty("vlist").EnumerateArray();
            foreach (var page in pages)
            {
                aidList.Add(page.GetProperty("aid").ToString());
            }
            return aidList;
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
