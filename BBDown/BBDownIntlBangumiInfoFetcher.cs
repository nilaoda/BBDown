using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using static BBDown.BBDownEntity;
using static BBDown.BBDownUtil;

namespace BBDown
{
    class BBDownIntlBangumiInfoFetcher : IFetcher
    {
        public BBDownVInfo Fetch(string id)
        {
            id = id.Substring(3);
            string index = "";
            //string api = $"https://api.global.bilibili.com/intl/gateway/ogv/m/view?ep_id={id}&s_locale=ja_JP";
            string api = $"https://api.global.bilibili.com/intl/gateway/v2/ogv/view/app/season?ep_id={id}&s_locale=zh_SG";
            string json = GetWebSource(api);
            JObject infoJson = JObject.Parse(json);
            string seasonId = infoJson["result"]["season_id"].ToString();
            string cover = infoJson["result"]["refine_cover"].ToString();
            string title = infoJson["result"]["title"].ToString();
            string desc = infoJson["result"]["evaluate"].ToString();


            string animeUrl = $"https://bangumi.bilibili.com/anime/{seasonId}";
            var web = GetWebSource(animeUrl);
            if (web != "")
            {
                Regex regex = new Regex("window.__INITIAL_STATE__=([\\s\\S].*?);\\(function\\(\\)");
                string _json = regex.Match(web).Groups[1].Value;
                cover = JObject.Parse(_json)["mediaInfo"]["cover"].ToString();
                title = JObject.Parse(_json)["mediaInfo"]["title"].ToString();
                desc = JObject.Parse(_json)["mediaInfo"]["evaluate"].ToString();
            }

            string pubTime = infoJson["result"]["publish"]["pub_time"].ToString();
            JArray pages = infoJson["result"]["episodes"].ToString() != "" ? JArray.Parse(infoJson["result"]["episodes"].ToString()) : new JArray();
            List<Page> pagesInfo = new List<Page>();
            int i = 1;

            if (infoJson["result"]["modules"] != null)
            {
                foreach (JObject section in JArray.Parse(infoJson["result"]["modules"].ToString()))
                {
                    if (section.ToString().Contains($"/{id}"))
                    {
                        pages = JArray.Parse(section["data"]["episodes"].ToString());
                        break;
                    }
                }
            }

            /*if (pages.Count == 0)
            {
                if (web != "") 
                {
                    string epApi = $"https://api.bilibili.com/pgc/web/season/section?season_id={seasonId}";
                    var _web = GetWebSource(epApi);
                    pages = JArray.Parse(JObject.Parse(_web)["result"]["main_section"]["episodes"].ToString());
                }
                else if (infoJson["data"]["modules"] != null)
                {
                    foreach (JObject section in JArray.Parse(infoJson["data"]["modules"].ToString()))
                    {
                        if (section.ToString().Contains($"ep_id={id}"))
                        {
                            pages = JArray.Parse(section["data"]["episodes"].ToString());
                            break;
                        }
                    }
                }
            }*/

            foreach (JObject page in pages)
            {
                string res = "";
                try
                {
                    res = page["dimension"]["width"].ToString() + "x" + page["dimension"]["height"].ToString();
                }
                catch (Exception) { }
                string _title = page["long_title"].ToString().Trim();
                if (string.IsNullOrEmpty(_title)) _title = page["title"].ToString();
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
