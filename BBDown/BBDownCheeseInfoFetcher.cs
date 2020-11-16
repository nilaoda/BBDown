using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
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
            JObject infoJson = JObject.Parse(json);
            string cover = infoJson["data"]["cover"].ToString();
            string title = infoJson["data"]["title"].ToString();
            string desc = infoJson["data"]["subtitle"].ToString();
            JArray pages = JArray.Parse(infoJson["data"]["episodes"].ToString());
            List<Page> pagesInfo = new List<Page>();
            foreach (JObject page in pages)
            {
                Page p = new Page(page["index"].Value<int>(),
                    page["aid"].ToString(),
                    page["cid"].ToString(),
                    page["id"].ToString(),
                    page["title"].ToString().Trim(),
                    page["duration"].Value<int>(), "");
                if (p.epid == id) index = p.index.ToString();
                pagesInfo.Add(p);
            }
            string pubTime = pagesInfo.Count > 0 ? pages[0]["release_date"].ToString() : "";
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
