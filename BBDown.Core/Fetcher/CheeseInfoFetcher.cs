using BBDown.Core.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static BBDown.Core.Entity.Entity;
using static BBDown.Core.Util.HTTPUtil;

namespace BBDown.Core.Fetcher
{
    public class CheeseInfoFetcher : IFetcher
    {
        /// <summary>
        /// 课程解析
        /// https://www.bilibili.com/cheese/play/ep5662?csource=Detail_relevantclass
        /// 
        /// </summary>
        public async Task<VInfo> FetchAsync(string id)
        {
            id = id.Substring(7);
            string index = "";
            string api = $"https://api.bilibili.com/pugv/view/web/season?ep_id={id}";
            string json = await GetWebSourceAsync(api);
            using var infoJson = JsonDocument.Parse(json);
            var data = infoJson.RootElement.GetProperty("data");
            string cover = data.GetProperty("cover").ToString();
            string title = data.GetProperty("title").ToString();
            string desc = data.GetProperty("subtitle").ToString();
            string ownerName = data.GetProperty("up_info").GetProperty("uname").ToString();
            string ownerMid = data.GetProperty("up_info").GetProperty("mid").ToString();
            var pages = data.GetProperty("episodes").EnumerateArray().ToList();
            List<Page> pagesInfo = new List<Page>();
            foreach (var page in pages)
            {
                Page p = new Page(page.GetProperty("index").GetInt32(),
                    page.GetProperty("aid").ToString(),
                    page.GetProperty("cid").ToString(),
                    page.GetProperty("id").ToString(),
                    page.GetProperty("title").ToString().Trim(),
                    page.GetProperty("duration").GetInt32(),
                    "",
                    "",
                    "",
                    ownerName,
                    ownerMid);
                if (p.epid == id) index = p.index.ToString();
                pagesInfo.Add(p);
            }
            string pubTime = pagesInfo.Count > 0 ? pages[0].GetProperty("release_date").ToString() : "";
            pubTime = pubTime != "" ? (new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Convert.ToDouble(pubTime)).ToLocalTime().ToString()) : "";

            var info = new VInfo();
            info.Title = title.Trim();
            info.Desc = desc.Trim();
            info.Pic = cover;
            info.PubTime = pubTime;
            info.PagesInfo = pagesInfo;
            info.IsBangumi = true;
            info.IsCheese = true;
            info.Index = index;

            return info;
        }
    }
}
