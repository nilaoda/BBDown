using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using static BBDown.BBDownUtil;
using static BBDown.BBDownLogger;
using static BBDown.BBDownEntity;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BBDown
{
    class BBDownParser
    {
        private static async Task<string> GetPlayJsonAsync(bool onlyAvc, string aidOri, string aid, string cid, string epId, bool tvApi, bool intl, bool appApi, string qn = "0")
        {
            LogDebug("aid={0},cid={1},epId={2},tvApi={3},IntlApi={4},appApi={5},qn={6}", aid, cid, epId, tvApi, intl, appApi, qn);

            if (intl) return await GetPlayJsonAsync(aid, cid, epId, qn);


            bool cheese = aidOri.StartsWith("cheese:");
            bool bangumi = cheese || aidOri.StartsWith("ep:");
            LogDebug("bangumi={0},cheese={1}", bangumi, cheese);

            if (appApi) return await BBDownAppHelper.DoReqAsync(aid, cid, epId, qn, bangumi, onlyAvc, Program.TOKEN);

            string prefix = tvApi ? (bangumi ? "api.snm0516.aisee.tv/pgc/player/api/playurltv" : "api.snm0516.aisee.tv/x/tv/ugc/playurl")
                        : (bangumi ? "api.bilibili.com/pgc/player/web/playurl" : "api.bilibili.com/x/player/playurl");
            string api = $"https://{prefix}?avid={aid}&cid={cid}&qn={qn}&type=&otype=json" + (tvApi ? "" : "&fourk=1") +
                $"&fnver=0&fnval=976" + (tvApi ? "&device=android&platform=android" +
                "&mobi_app=android_tv_yst&npcybs=0&force_host=0&build=102801" +
                (Program.TOKEN != "" ? $"&access_key={Program.TOKEN}" : "") : "") +
                (bangumi ? $"&module=bangumi&ep_id={epId}&fourk=1" + "&session=" : "");
            if (tvApi && bangumi)
            {
                api = (Program.TOKEN != "" ? $"access_key={Program.TOKEN}&" : "") +
                    $"aid={aid}&appkey=4409e2ce8ffd12b8&build=102801" +
                    $"&cid={cid}&device=android&ep_id={epId}&expire=0" +
                    $"&fnval=80&fnver=0&fourk=1" +
                    $"&mid=0&mobi_app=android_tv_yst" +
                    $"&module=bangumi&npcybs=0&otype=json&platform=android" +
                    $"&qn={qn}&ts={GetTimeStamp(true)}";
                api = $"https://{prefix}?" + api + (bangumi ? $"&sign={GetSign(api)}" : "");
            }

            //课程接口
            if (cheese) api = api.Replace("/pgc/", "/pugv/");

            //Console.WriteLine(api);
            string webJson = await GetWebSourceAsync(api);
            //以下情况从网页源代码尝试解析
            if (webJson.Contains("\"大会员专享限制\""))
            {
                string webUrl = "https://www.bilibili.com/bangumi/play/ep" + epId;
                string webSource = await GetWebSourceAsync(webUrl);
                webJson = Regex.Match(webSource, @"window.__playinfo__=([\s\S]*?)<\/script>").Groups[1].Value;
            }
            return webJson;
        }

        private static async Task<string> GetPlayJsonAsync(string aid, string cid, string epId, string qn)
        {
            string api = $"https://api.global.bilibili.com/intl/gateway/v2/ogv/playurl?" +
                $"aid={aid}&cid={cid}&ep_id={epId}&platform=android&prefer_code_type=0&qn={qn}" + (Program.TOKEN != "" ? $"&access_key={Program.TOKEN}" : "");
            string webJson = await GetWebSourceAsync(api);
            return webJson;
        }

        public static async Task<(string, List<Video>, List<Audio>, List<string>, List<string>)> ExtractTracksAsync(bool onlyHevc, bool onlyAvc, string aidOri, string aid, string cid, string epId, bool tvApi, bool intlApi, bool appApi, string qn = "0")
        {
            List<Video> videoTracks = new List<Video>();
            List<Audio> audioTracks = new List<Audio>();
            List<string> clips = new List<string>();
            List<string> dfns = new List<string>();

            //调用解析
            string webJsonStr = await GetPlayJsonAsync(onlyAvc, aidOri, aid, cid, epId, tvApi, intlApi, appApi, qn);

            var respJson = JsonDocument.Parse(webJsonStr);
            var data = respJson.RootElement;

            //intl接口
            if (webJsonStr.Contains("\"stream_list\""))
            {
                int pDur = data.GetProperty("data").GetProperty("video_info").GetProperty("timelength").GetInt32() / 1000;
                var audio = data.GetProperty("data").GetProperty("video_info").GetProperty("dash_audio").EnumerateArray().ToList();
                foreach(var stream in data.GetProperty("data").GetProperty("video_info").GetProperty("stream_list").EnumerateArray())
                {
                    JsonElement dashVideo;
                    if (stream.TryGetProperty("dash_video", out dashVideo))
                    {
                        if (dashVideo.GetProperty("base_url").ToString() != "")
                        {
                            Video v = new Video();
                            v.dur = pDur;
                            v.id = stream.GetProperty("stream_info").GetProperty("quality").ToString();
                            v.dfn = Program.qualitys[v.id];
                            v.bandwith = Convert.ToInt64(dashVideo.GetProperty("bandwidth").ToString()) / 1000;
                            v.baseUrl = dashVideo.GetProperty("base_url").ToString();
                            v.codecs = dashVideo.GetProperty("codecid").ToString() == "12" ? "HEVC" : "AVC";
                            if (onlyHevc && v.codecs == "AVC") continue;
                            if (!videoTracks.Contains(v)) videoTracks.Add(v);
                        }
                    }
                }

                foreach(var node in audio)
                {
                    Audio a = new Audio();
                    a.id = node.GetProperty("id").ToString();
                    a.dfn = node.GetProperty("id").ToString();
                    a.dur = pDur;
                    a.bandwith = Convert.ToInt64(node.GetProperty("bandwidth").ToString()) / 1000;
                    a.baseUrl = node.GetProperty("base_url").ToString();
                    a.codecs = "M4A";
                    audioTracks.Add(a);
                }

                return (webJsonStr, videoTracks, audioTracks, clips, dfns);
            }

            if (webJsonStr.Contains("\"dash\":{")) //dash
            {
                List<JsonElement> audio = null;
                List<JsonElement> video = null;
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
                    webJsonStr = await GetPlayJsonAsync(onlyAvc, aidOri, aid, cid, epId, tvApi, intlApi, appApi, GetMaxQn());
                    respJson = JsonDocument.Parse(webJsonStr);
                }
                try { video = !tvApi ? respJson.RootElement.GetProperty(nodeName).GetProperty("dash").GetProperty("video").EnumerateArray().ToList() : respJson.RootElement.GetProperty("dash").GetProperty("video").EnumerateArray().ToList(); } catch { }
                try { audio = !tvApi ? respJson.RootElement.GetProperty(nodeName).GetProperty("dash").GetProperty("audio").EnumerateArray().ToList() : respJson.RootElement.GetProperty("dash").GetProperty("audio").EnumerateArray().ToList(); } catch { }
                //处理杜比音频
                try
                {
                    if (!tvApi && respJson.RootElement.GetProperty(nodeName).GetProperty("dash").TryGetProperty("dolby", out JsonElement dolby))
                    {
                        if(dolby.TryGetProperty("audio", out JsonElement db))
                        {
                            audio.AddRange(db.EnumerateArray().ToList());
                        }
                    }
                }
                catch (Exception) { ; }
                
                if (video != null)
                {
                    foreach (var node in video)
                    {
                        Video v = new Video();
                        v.dur = pDur;
                        v.id = node.GetProperty("id").ToString();
                        v.dfn = Program.qualitys[v.id];
                        v.bandwith = Convert.ToInt64(node.GetProperty("bandwidth").ToString()) / 1000;
                        v.baseUrl = node.GetProperty("base_url").ToString();
                        v.codecs = node.GetProperty("codecid").ToString() == "12" ? "HEVC" : "AVC";
                        if (!tvApi && !appApi)
                        {
                            v.res = node.GetProperty("width").ToString() + "x" + node.GetProperty("height").ToString();
                            v.fps = node.GetProperty("frame_rate").ToString();
                        }
                        if (onlyHevc && v.codecs == "AVC") continue;
                        if (onlyAvc && v.codecs == "HEVC") continue;
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
                        Audio a = new Audio();
                        a.id = node.GetProperty("id").ToString();
                        a.dfn = a.id;
                        a.dur = pDur;
                        a.bandwith = Convert.ToInt64(node.GetProperty("bandwidth").ToString()) / 1000;
                        a.baseUrl = node.GetProperty("base_url").ToString();
                        a.codecs = node.GetProperty("codecs").ToString().Replace("mp4a.40.2", "M4A").Replace("ec-3", "E-AC-3");
                        audioTracks.Add(a);
                    }
                }
            }
            else if (webJsonStr.Contains("\"durl\":[")) //flv
            {
                //默认以最高清晰度解析
                webJsonStr = await GetPlayJsonAsync(onlyAvc, aidOri, aid, cid, epId, tvApi, intlApi, appApi, GetMaxQn());
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
                    JsonElement qnExtras;
                    JsonElement acceptQuality;
                    if (dataNode.TryGetProperty("qn_extras", out qnExtras)) 
                    {
                        foreach (var node in qnExtras.EnumerateArray())
                        {
                            dfns.Add(node.GetProperty("qn").ToString());
                        }
                    }
                    else if (dataNode.TryGetProperty("accept_quality", out acceptQuality)) //非tv模式可用清晰度
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
                    JsonElement qnExtras;
                    JsonElement acceptQuality;
                    if (nodeJson.TryGetProperty("qn_extras", out qnExtras))
                    {
                        //获取可用清晰度
                        foreach (var node in qnExtras.EnumerateArray())
                        {
                            dfns.Add(node.GetProperty("qn").ToString());
                        }
                    }                   
                    else if (nodeJson.TryGetProperty("accept_quality", out acceptQuality)) //非tv模式可用清晰度
                    {
                        foreach (var node in acceptQuality.EnumerateArray())
                        {
                            string _qn = node.ToString();
                            if (_qn != null && _qn.Length > 0)
                                dfns.Add(node.ToString());
                        }
                    }
                }

                Video v = new Video();
                v.id = quality;
                v.dfn = Program.qualitys[quality];
                v.baseUrl = url;
                v.codecs = videoCodecid == "12" ? "HEVC" : "AVC";
                v.dur = (int)length / 1000;
                v.size = size;
                if (onlyHevc && v.codecs == "AVC") { }
                if (!videoTracks.Contains(v)) videoTracks.Add(v);
            }

            return (webJsonStr, videoTracks, audioTracks, clips, dfns);
        }
    }
}
