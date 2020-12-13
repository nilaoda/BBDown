using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using static BBDown.BBDownEntity;
using static BBDown.BBDownUtil;

namespace BBDown
{
    class BBDownBangumiInfoFetcher : IFetcher
    {
        public BBDownVInfo Fetch(string id)
        {
            id = id.Substring(3);
            string index = "";
            string api = $"https://api.bilibili.com/pgc/view/web/season?ep_id={id}";
            string json = GetWebSource(api);
            JObject infoJson = JObject.Parse(json);
            string cover = infoJson["result"]["cover"].ToString();
            string title = infoJson["result"]["title"].ToString();
            string desc = infoJson["result"]["evaluate"].ToString();
            string pubTime = infoJson["result"]["publish"]["pub_time"].ToString();
            JArray pages = JArray.Parse(infoJson["result"]["episodes"].ToString());
            List<Page> pagesInfo = new List<Page>();
            int i = 1;

            if (pages.Count == 0) 
            {
                if (infoJson["result"]["section"] != null)
                {
                    foreach (JObject section in JArray.Parse(infoJson["result"]["section"].ToString()))
                    {
                        if (section.ToString().Contains($"/ep{id}"))
                        {
                            pages = JArray.Parse(section["episodes"].ToString());
                            break;
                        }
                    }
                }
            }

            foreach (JObject page in pages)
            {
                string res = "";
                try
                {
                    res = page["dimension"]["width"].ToString() + "x" + page["dimension"]["height"].ToString();
                }
                catch (Exception) { }
                string _title = page["long_title"].ToString().Trim();
                if(string.IsNullOrEmpty(_title)) _title = page["title"].ToString();
                Page p = new Page(i++,
                    page["aid"].ToString(),
                    page["cid"].ToString(),
                    page["id"].ToString(),
                    _title,
                    0, res);
                if (p.epid == id) index = p.index.ToString();
                pagesInfo.Add(p);
            }


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
