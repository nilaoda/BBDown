using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using static BBDown.Core.Logger;
using static BBDown.Core.Util.HTTPUtil;
using static BBDown.Core.Entity.Entity;
using System.Security.Cryptography;

namespace BBDown.Core
{
    public partial class Parser
    {
        public static string WbiSign(string api)
        {
            return $"{api}&w_rid=" + string.Concat(MD5.HashData(Encoding.UTF8.GetBytes(api + Config.WBI)).Select(i => i.ToString("x2")).ToArray());
        }

        private static async Task<string> GetPlayJsonAsync(bool onlyAvc, string aidOri, string aid, string cid, string epId, bool tvApi, bool intl, bool appApi, string qn = "0")
        {
            LogDebug("aid={0},cid={1},epId={2},tvApi={3},IntlApi={4},appApi={5},qn={6}", aid, cid, epId, tvApi, intl, appApi, qn);

            if (intl) return await GetPlayJsonAsync(aid, cid, epId, qn);


            bool cheese = aidOri.StartsWith("cheese:");
            bool bangumi = cheese || aidOri.StartsWith("ep:");
            LogDebug("bangumi={0},cheese={1}", bangumi, cheese);

            if (appApi) return await AppHelper.DoReqAsync(aid, cid, epId, qn, bangumi, onlyAvc, Config.TOKEN);

            string prefix = tvApi ? bangumi ? "api.snm0516.aisee.tv/pgc/player/api/playurltv" : "api.snm0516.aisee.tv/x/tv/ugc/playurl"
                        : bangumi ? $"{Config.HOST}/pgc/player/web/playurl" : "api.bilibili.com/x/player/wbi/playurl";

            string api = $"avid={aid}&cid={cid}&fnval=4048&fnver=0" +
                (tvApi ? "" : "&fourk=1") + (Config.AREA != "" ? Config.TOKEN != "" ? $"&access_key={Config.TOKEN}&area={Config.AREA}" : $"&area={Config.AREA}" : "") +
                (tvApi ? "&device=android&platform=android&mobi_app=android_tv_yst&npcybs=0&force_host=2&build=102801" +
                (Config.TOKEN != "" ? $"&access_key={Config.TOKEN}" : "") : "") + $"&otype=json&qn={qn}" +
                (bangumi ? $"&module=bangumi&ep_id={epId}&fourk=1&session=" : Config.COOKIE != "" ? $"&wts={GetTimeStamp(true)}" : $"&try_look=1&wts={GetTimeStamp(true)}");
            api = $"https://{prefix}?{(bangumi ? api : WbiSign(api))}";
            if (tvApi && bangumi)
            {
                api = (Config.TOKEN != "" ? $"access_key={Config.TOKEN}&" : "") +
                    $"aid={aid}&appkey=4409e2ce8ffd12b8&build=102801" +
                    $"&cid={cid}&device=android&ep_id={epId}&expire=0" +
                    $"&fnval=80&fnver=0&fourk=1" +
                    $"&mid=0&mobi_app=android_tv_yst" +
                    $"&module=bangumi&npcybs=0&otype=json&platform=android" +
                    $"&qn={qn}&ts={GetTimeStamp(true)}";
                api = $"https://{prefix}?" + api + (bangumi ? $"&sign={GetSign(api, false)}" : "");
            }

            //课程接口
            if (cheese) api = api.Replace("/pgc/", "/pugv/");

            //Console.WriteLine(api);
            string webJson = await GetWebSourceAsync(api);
            //以下情况从网页源代码尝试解析
            if (webJson.Contains("\"大会员专享限制\""))
            {
                Log("此视频需要大会员，您大概率需要登录一个有大会员的账号才可以下载，尝试从网页源码解析");
                string webUrl = "https://www.bilibili.com/bangumi/play/ep" + epId;
                string webSource = await GetWebSourceAsync(webUrl);
                webJson = PlayerJsonRegex().Match(webSource).Groups[1].Value;
            }
            return webJson;
        }

        private static async Task<string> GetPlayJsonAsync(string aid, string cid, string epId, string qn, string code = "0")
        {

            string api = "";
            if (Config.HOST == "api.bilibili.com")
            {
                api = "https://api.biliintl.com/intl/gateway/v2/ogv/playurl" +
                    $"?aid={aid}&cid={cid}&ep_id={epId}&platform=android&s_locale=zh_SG&prefer_code_type={code}&qn={qn}" +
                    (Config.TOKEN != "" ? $"&access_key={Config.TOKEN}" : "");
            }
            else
            {
                string param = (Config.TOKEN != "" ? $"access_key={Config.TOKEN}&" : "") +
                    $"aid={aid}&appkey=7d089525d3611b1c&area={Config.AREA}&cid={cid}&ep_id={epId}&platform=android&prefer_code_type={code}&qn={qn}&s_locale=zh_SG&ts={GetTimeStamp(true)}";
                api = $"https://{Config.HOST}/intl/gateway/v2/ogv/playurl?" + param + $"&sign={GetSign(param, true)}";
            }
            string webJson = await GetWebSourceAsync(api);
            return webJson;
        }

        public static string SkiPcdn(List<string> urlList, string baseUrl)
        {
            urlList.Add(baseUrl);
            return urlList.FirstOrDefault(i => !PcdnRegex().IsMatch(i), urlList.First());
        }

        public static async Task<(string, List<Video>, List<Audio>, List<string>, List<string>)> ExtractTracksAsync(string aidOri, string aid, string cid, string epId, bool tvApi, bool intlApi, bool appApi, string qn = "0")
        {
            List<Video> videoTracks = new();
            List<Audio> audioTracks = new();
            List<string> clips = new();
            List<string> dfns = new();
            var intlCode = "0";

            //调用解析
            string webJsonStr = await GetPlayJsonAsync(false, aidOri, aid, cid, epId, tvApi, intlApi, appApi, qn);

        startParsing:
            var respJson = JsonDocument.Parse(webJsonStr);
            var data = respJson.RootElement;

            //intl接口
            if (webJsonStr.Contains("\"stream_list\""))
            {
                int pDur = data.GetProperty("data").GetProperty("video_info").GetProperty("timelength").GetInt32() / 1000;
                var audio = data.GetProperty("data").GetProperty("video_info").GetProperty("dash_audio").EnumerateArray().ToList();
                foreach(var stream in data.GetProperty("data").GetProperty("video_info").GetProperty("stream_list").EnumerateArray())
                {
                    if (stream.TryGetProperty("dash_video", out JsonElement dashVideo))
                    {
                        if (dashVideo.GetProperty("base_url").ToString() != "")
                        {
                            var videoId = stream.GetProperty("stream_info").GetProperty("quality").ToString();
                            var urlList = new List<string>() { dashVideo.GetProperty("base_url").ToString() };
                            urlList.AddRange(dashVideo.GetProperty("backup_url").EnumerateArray().ToList().Select(i => i.ToString()));
                            Video v = new()
                            {
                                dur = pDur,
                                id = videoId,
                                dfn = Config.qualitys[videoId],
                                bandwith = Convert.ToInt64(dashVideo.GetProperty("bandwidth").ToString()) / 1000,
                                baseUrl = urlList.FirstOrDefault(i => !BaseUrlRegex().IsMatch(i), urlList.First()),
                                codecs = GetVideoCodec(dashVideo.GetProperty("codecid").ToString()),
                                size = dashVideo.TryGetProperty("size", out var sizeNode) ? Convert.ToDouble(sizeNode.ToString()) : 0
                            };
                            if (!videoTracks.Contains(v)) videoTracks.Add(v);
                        }
                    }
                }

                foreach(var node in audio)
                {
                    var urlList = new List<string>() { node.GetProperty("base_url").ToString() };
                    urlList.AddRange(node.GetProperty("backup_url").EnumerateArray().ToList().Select(i => i.ToString()));
                    Audio a = new()
                    {
                        id = node.GetProperty("id").ToString(),
                        dfn = node.GetProperty("id").ToString(),
                        dur = pDur,
                        bandwith = Convert.ToInt64(node.GetProperty("bandwidth").ToString()) / 1000,
                        baseUrl = urlList.FirstOrDefault(i => !BaseUrlRegex().IsMatch(i), urlList.First()),
                        codecs = "M4A"
                    };
                    if (!audioTracks.Contains(a)) audioTracks.Add(a);
                }

                if (intlCode == "0")
                {
                    intlCode = "1";
                    webJsonStr = await GetPlayJsonAsync(aid, cid, epId, qn, intlCode);
                    goto startParsing;
                }

                return (webJsonStr, videoTracks, audioTracks, clips, dfns);
            }

            if (webJsonStr.Contains("\"dash\":{")) //dash
            {
                List<JsonElement>? audio = null;
                List<JsonElement>? video = null;
                int pDur = 0;
                string nodeName = "data";
                if (webJsonStr.Contains("\"result\":{"))
                {
                    nodeName = "result";
                }

                try { pDur = !tvApi ? respJson.RootElement.GetProperty(nodeName).GetProperty("dash").GetProperty("duration").GetInt32() : respJson.RootElement.GetProperty("dash").GetProperty("duration").GetInt32(); } catch { }
                try { pDur = !tvApi ? respJson.RootElement.GetProperty(nodeName).GetProperty("timelength").GetInt32() / 1000 : respJson.RootElement.GetProperty("timelength").GetInt32() / 1000; } catch { }

                bool reParse = false;
            reParse:
                if (reParse)
                {
                    webJsonStr = await GetPlayJsonAsync(false, aidOri, aid, cid, epId, tvApi, intlApi, appApi, GetMaxQn());
                    respJson = JsonDocument.Parse(webJsonStr);
                }
                try { video = !tvApi ? respJson.RootElement.GetProperty(nodeName).GetProperty("dash").GetProperty("video").EnumerateArray().ToList() : respJson.RootElement.GetProperty("dash").GetProperty("video").EnumerateArray().ToList(); } catch { }
                try { audio = !tvApi ? respJson.RootElement.GetProperty(nodeName).GetProperty("dash").GetProperty("audio").EnumerateArray().ToList() : respJson.RootElement.GetProperty("dash").GetProperty("audio").EnumerateArray().ToList(); } catch { }
                //处理杜比音频
                try
                {
                    if (audio != null)
                    {
                        if (!tvApi && respJson.RootElement.GetProperty(nodeName).GetProperty("dash").TryGetProperty("dolby", out JsonElement dolby))
                        {
                            if (dolby.TryGetProperty("audio", out JsonElement db))
                            {
                                audio.AddRange(db.EnumerateArray().ToList());
                            }
                        }
                    }
                }
                catch (Exception) { ; }

                //处理Hi-Res无损
                try
                {
                    if (audio != null)
                    {
                        if (!tvApi && respJson.RootElement.GetProperty(nodeName).GetProperty("dash").TryGetProperty("flac", out JsonElement hiRes))
                        {
                            if (hiRes.TryGetProperty("audio", out JsonElement db))
                            {
                                if (db.ValueKind != JsonValueKind.Null)
                                    audio.Add(db);
                            }
                        }
                    }
                }
                catch (Exception) {; }

                if (video != null)
                {
                    foreach (var node in video)
                    {
                        var urlList = new List<string>() { node.GetProperty("base_url").ToString() };
                        if(node.TryGetProperty("backup_url", out JsonElement element) && element.ValueKind != JsonValueKind.Null)
                        {
                            urlList.AddRange(element.EnumerateArray().ToList().Select(i => i.ToString()));
                        }
                        var videoId = node.GetProperty("id").ToString();
                        Video v = new()
                        {
                            dur = pDur,
                            id = videoId,
                            dfn = Config.qualitys[videoId],
                            bandwith = Convert.ToInt64(node.GetProperty("bandwidth").ToString()) / 1000,
                            baseUrl = urlList.FirstOrDefault(i => !BaseUrlRegex().IsMatch(i), urlList.First()),
                            codecs = GetVideoCodec(node.GetProperty("codecid").ToString()),
                            size = node.TryGetProperty("size", out var sizeNode) ? Convert.ToDouble(sizeNode.ToString()) : 0
                        };
                        if (!tvApi && !appApi)
                        {
                            v.res = node.GetProperty("width").ToString() + "x" + node.GetProperty("height").ToString();
                            v.fps = node.GetProperty("frame_rate").ToString();
                        }
                        if (!videoTracks.Contains(v)) videoTracks.Add(v);
                    }
                }

                //此处处理免二压视频，需要单独再请求一次
                if (!reParse && !appApi)
                {
                    reParse = true;
                    goto reParse;
                }

                if (audio != null)
                {
                    foreach (var node in audio)
                    {
                        var urlList = new List<string>() { node.GetProperty("base_url").ToString() };
                        if (node.TryGetProperty("backup_url", out JsonElement element) && element.ValueKind != JsonValueKind.Null)
                        {
                            urlList.AddRange(element.EnumerateArray().ToList().Select(i => i.ToString()));
                        }
                        var audioId = node.GetProperty("id").ToString();
                        Audio a = new()
                        {
                            id = audioId,
                            dfn = audioId,
                            dur = pDur,
                            bandwith = Convert.ToInt64(node.GetProperty("bandwidth").ToString()) / 1000,
                            baseUrl = urlList.FirstOrDefault(i => !BaseUrlRegex().IsMatch(i), urlList.First()),
                            codecs = node.GetProperty("codecs").ToString().Replace("mp4a.40.2", "M4A").Replace("ec-3", "E-AC-3").Replace("fLaC", "FLAC")
                        };
                        audioTracks.Add(a);
                    }
                }
            }
            else if (webJsonStr.Contains("\"durl\":[")) //flv
            {
                //默认以最高清晰度解析
                webJsonStr = await GetPlayJsonAsync(false, aidOri, aid, cid, epId, tvApi, intlApi, appApi, GetMaxQn());
                respJson = JsonDocument.Parse(webJsonStr);
                string quality = "";
                string videoCodecid = "";
                string url = "";
                double size = 0;
                double length = 0;
                var nodeName = "data";
            flvParse:
                if (webJsonStr.Contains($"\"{nodeName}\":{{"))
                {
                    var dataNode = respJson.RootElement.GetProperty(nodeName);
                    quality = dataNode.GetProperty("quality").ToString();
                    videoCodecid = dataNode.GetProperty("video_codecid").ToString();
                    //获取所有分段
                    foreach (var node in dataNode.GetProperty("durl").EnumerateArray().ToList())
                    {
                        clips.Add(node.GetProperty("url").ToString());
                        size += node.GetProperty("size").GetDouble();
                        length += node.GetProperty("length").GetDouble();
                    }
                    //TV模式可用清晰度
                    if (dataNode.TryGetProperty("qn_extras", out JsonElement qnExtras))
                    {
                        foreach (var node in qnExtras.EnumerateArray())
                        {
                            dfns.Add(node.GetProperty("qn").ToString());
                        }
                    }
                    else if (dataNode.TryGetProperty("accept_quality", out JsonElement acceptQuality)) //非tv模式可用清晰度
                    {
                        foreach (var node in acceptQuality.EnumerateArray())
                        {
                            string _qn = node.ToString();
                            if (_qn != null && _qn.Length > 0)
                                dfns.Add(node.ToString());
                        }
                    }
                }
                else if (webJsonStr.Contains("\"result\":{"))
                {
                    nodeName = "result";
                    goto flvParse;
                }
                else
                {
                    //如果获取数据失败，尝试从根路径获取数据
                    string nodeinfo = respJson.RootElement.ToString();
                    var nodeJson = JsonDocument.Parse(nodeinfo).RootElement;
                    quality = nodeJson.GetProperty("quality").ToString();
                    videoCodecid = nodeJson.GetProperty("video_codecid").ToString();
                    //获取所有分段
                    foreach (var node in nodeJson.GetProperty("durl").EnumerateArray())
                    {
                        clips.Add(node.GetProperty("url").ToString());
                        size += node.GetProperty("size").GetDouble();
                        length += node.GetProperty("length").GetDouble();
                    }
                    //TV模式可用清晰度
                    if (nodeJson.TryGetProperty("qn_extras", out JsonElement qnExtras))
                    {
                        //获取可用清晰度
                        foreach (var node in qnExtras.EnumerateArray())
                        {
                            dfns.Add(node.GetProperty("qn").ToString());
                        }
                    }
                    else if (nodeJson.TryGetProperty("accept_quality", out JsonElement acceptQuality)) //非tv模式可用清晰度
                    {
                        foreach (var node in acceptQuality.EnumerateArray())
                        {
                            string _qn = node.ToString();
                            if (_qn != null && _qn.Length > 0)
                                dfns.Add(node.ToString());
                        }
                    }
                }

                Video v = new()
                {
                    id = quality,
                    dfn = Config.qualitys[quality],
                    baseUrl = url,
                    codecs = GetVideoCodec(videoCodecid),
                    dur = (int)length / 1000,
                    size = size
                };
                if (!videoTracks.Contains(v)) videoTracks.Add(v);
            }

            return (webJsonStr, videoTracks, audioTracks, clips, dfns);
        }

        /// <summary>
        /// 编码转换
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        private static string GetVideoCodec(string code)
        {
            return code switch
            {
                "13" => "AV1",
                "12" => "HEVC",
                "7" => "AVC",
                _ => "UNKNOWN"
            };
        }

        private static string GetMaxQn()
        {
            return Config.qualitys.Keys.First();
        }

        private static string GetTimeStamp(bool bflag)
        {
            DateTimeOffset ts = DateTimeOffset.Now;
            return bflag ? ts.ToUnixTimeSeconds().ToString() : ts.ToUnixTimeMilliseconds().ToString();
        }

        private static string GetSign(string parms, bool isBiliPlus)
        {
            string toEncode = parms + (isBiliPlus ? "acd495b248ec528c2eed1e862d393126" : "59b43e04ad6965f34319062b478f83dd");
            return string.Concat(MD5.HashData(Encoding.UTF8.GetBytes(toEncode)).Select(i => i.ToString("x2")).ToArray());
        }

        [GeneratedRegex("window.__playinfo__=([\\s\\S]*?)<\\/script>")]
        private static partial Regex PlayerJsonRegex();
        [GeneratedRegex("http.*:\\d+")]
        private static partial Regex BaseUrlRegex();
        [GeneratedRegex("://.*:\\d+/")]
        private static partial Regex PcdnRegex();
    }
}
