using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using static BBDown.BBDownEntity;
using static BBDown.BBDownUtil;

namespace BBDown
{
    class BBDownCheeseInfoFetcher : IFetcher
    {
        public BBDownVInfo Fetch(string id)
        {
            id = id.Substring(7);
            string index = "";
            string api = $"https://api.bilibili.com/pugv/view/web/season?ep_id={id}";
            string json = GetWebSource(api);
            using var infoJson = JsonDocument.Parse(json);
            var data = infoJson.RootElement.GetProperty("data");
            string cover = data.GetProperty("cover").ToString();
            string title = data.GetProperty("title").ToString();
            string desc = data.GetProperty("subtitle").ToString();
            var pages = data.GetProperty("episodes").EnumerateArray().ToList();
            List<Page> pagesInfo = new List<Page>();
            foreach (var page in pages)
            {
                Page p = new Page(page.GetProperty("index").GetInt32(),
                    page.GetProperty("aid").ToString(),
                    page.GetProperty("cid").ToString(),
                    page.GetProperty("id").ToString(),
                    page.GetProperty("title").ToString().Trim(),
                    page.GetProperty("duration").GetInt32(), "");
                if (p.epid == id) index = p.index.ToString();
                pagesInfo.Add(p);
            }
            string pubTime = pagesInfo.Count > 0 ? pages[0].GetProperty("release_date").ToString() : "";
            pubTime = pubTime != "" ? (new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Convert.ToDouble(pubTime)).ToLocalTime().ToString()) : "";

            var info = new BBDownVInfo();
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
