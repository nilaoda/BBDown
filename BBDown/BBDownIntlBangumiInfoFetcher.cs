using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
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
            string api = $"https://api.global.bilibili.com/intl/gateway/v2/ogv/view/app/season?ep_id={id}&platform=android&s_locale=zh_SG&mobi_app=bstar_a" + (Program.TOKEN != "" ? $"&access_key={Program.TOKEN}" : "");
            string json = GetWebSource(api);
            using var infoJson = JsonDocument.Parse(json);
            var result = infoJson.RootElement.GetProperty("result");
            string seasonId = result.GetProperty("season_id").ToString();
            string cover = result.GetProperty("cover").ToString();
            string title = result.GetProperty("title").ToString();
            string desc = result.GetProperty("evaluate").ToString();


            if (cover == "")
            {
                string animeUrl = $"https://bangumi.bilibili.com/anime/{seasonId}";
                var web = GetWebSource(animeUrl);
                if (web != "")
                {
                    Regex regex = new Regex("window.__INITIAL_STATE__=([\\s\\S].*?);\\(function\\(\\)");
                    string _json = regex.Match(web).Groups[1].Value;
                    using var _tempJson = JsonDocument.Parse(_json);
                    cover = _tempJson.RootElement.GetProperty("mediaInfo").GetProperty("cover").ToString();
                    title = _tempJson.RootElement.GetProperty("mediaInfo").GetProperty("title").ToString();
                    desc = _tempJson.RootElement.GetProperty("mediaInfo").GetProperty("evaluate").ToString();
                }
            }

            string pubTime = result.GetProperty("publish").GetProperty("pub_time").ToString();
            var pages = new List<JsonElement>();
            if (result.TryGetProperty("episodes", out _))
            {
                pages = result.GetProperty("episodes").EnumerateArray().ToList();
            }
            List<Page> pagesInfo = new List<Page>();
            int i = 1;

            JsonElement modules;
            if (result.TryGetProperty("modules", out modules))
            {
                foreach (var section in modules.EnumerateArray())
                {
                    if (section.ToString().Contains($"/{id}"))
                    {
                        pages = section.GetProperty("data").GetProperty("episodes").EnumerateArray().ToList();
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

            foreach (var page in pages)
            {
                //跳过预告
                JsonElement badge;
                if (page.TryGetProperty("badge", out badge) && badge.ToString() == "预告") continue;
                string res = "";
                try
                {
                    res = page.GetProperty("dimension").GetProperty("width").ToString() + "x" + page.GetProperty("dimension").GetProperty("height").ToString();
                }
                catch (Exception) { }
                string _title = page.GetProperty("title").ToString() + " " + page.GetProperty("long_title").ToString();
                _title = _title.Trim();
                Page p = new Page(i++,
                    page.GetProperty("aid").ToString(),
                    page.GetProperty("cid").ToString(),
                    page.GetProperty("id").ToString(),
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
