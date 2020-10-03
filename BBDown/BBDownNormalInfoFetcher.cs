using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using static BBDown.BBDownEntity;
using static BBDown.BBDownUtil;

namespace BBDown
{
    class BBDownNormalInfoFetcher : IFetcher
    {
        public BBDownVInfo Fetch(string id)
        {
            string api = $"https://api.bilibili.com/x/web-interface/view?aid={id}";
            string json = GetWebSource(api);
            JObject infoJson = JObject.Parse(json);
            string title = infoJson["data"]["title"].ToString();
            string desc = infoJson["data"]["desc"].ToString();
            string pic = infoJson["data"]["pic"].ToString();
            string pubTime = infoJson["data"]["pubdate"].ToString();
            pubTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Convert.ToDouble(pubTime)).ToLocalTime().ToString();
            bool bangumi = false;

            JArray pages = JArray.Parse(infoJson["data"]["pages"].ToString());
            List<Page> pagesInfo = new List<Page>();
            foreach (JObject page in pages)
            {
                Page p = new Page(page["page"].Value<int>(),
                    id,
                    page["cid"].ToString(),
                    "", //epid
                    GetValidFileName(page["part"].ToString()),
                    page["duration"].Value<int>(),
                    page["dimension"]["width"] + "x" + page["dimension"]["height"]);
                pagesInfo.Add(p);
            }

            try
            {
                if (infoJson["data"]["redirect_url"].ToString().Contains("bangumi"))
                {
                    bangumi = true;
                    string epId = Regex.Match(infoJson["data"]["redirect_url"].ToString(), "ep(\\d+)").Groups[1].Value;
                    //番剧内容通常不会有分P，如果有分P则不需要epId参数
                    if (pages.Count == 1)
                    {
                        pagesInfo.ForEach(p => p.epid = epId);
                    }
                }
            }
            catch { }

            var info = new BBDownVInfo();
            info.Title = GetValidFileName(title).Trim();
            info.Desc = GetValidFileName(desc).Trim();
            info.Pic = pic;
            info.PubTime = pubTime;
            info.PagesInfo = pagesInfo;
            info.IsBangumi = bangumi;

            return info;
        }
    }
}
