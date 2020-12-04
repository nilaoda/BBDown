using System.Collections.Generic;
using Newtonsoft.Json;
using static BBDown.BBDownEntity;
using static BBDown.Program;

namespace BBDown
{

    class TVResponse : ResponseBase<string, string>, IBBDownParse
    {
        [JsonProperty("dash")]
        public Dash Dash { get; set; }

        [JsonProperty("timelength")]
        public int Timelength { get; set; }
        [JsonProperty("accept_description")]
        public string[] AcceptDescription { get; set; }

        [JsonProperty("accept_format")]
        public string AcceptFormat { get; set; }

        [JsonProperty("accept_quality")]
        public long[] AcceptQuality { get; set; }

        [JsonProperty("accept_watermark")]
        public bool[] AcceptWatermark { get; set; }

        [JsonProperty("durl")]
        public Durl[] Durl { get; set; }

        [JsonProperty("format")]
        public string Format { get; set; }

        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("qn_extras")]
        public QnExtra[] QnExtras { get; set; }

        [JsonProperty("quality")]
        public string Quality { get; set; }

        [JsonProperty("seek_param")]
        public string SeekParam { get; set; }

        [JsonProperty("seek_type")]
        public string SeekType { get; set; }
        [JsonProperty("support_formats")]
        public SupportFormat[] SupportFormats { get; set; }

        [JsonProperty("type")]
        public long Type { get; set; }

        [JsonProperty("video_codecid")]
        public string VideoCodecid { get; set; }

        [JsonProperty("video_project")]
        public bool VideoProject { get; set; }

        public string GetHightestQN()
        {
            return "125";
        }


        public (List<Video>, Video) GetVideoInfo(MyOption myOption)
        {


            List<Video> ls = new List<Video>();
            Video v1 = new Video();
            v1.Clips = new List<string>();
            v1.Dfns = new List<string>();

            v1.Format = Format;
            v1.id = Quality;
            v1.codecs = VideoCodecid == "12" ? "HEVC" : "AVC";
            v1.baseUrl = "";
            foreach (var item in SupportFormats)
            {
                if (item.Quality.ToString() == v1.id)
                {
                    v1.dfn = item.NewDescription;
                    break;
                }
            }

            //获取所有分段
            foreach (var item in Durl)
            {
                v1.Clips.Add(item.Url);
                v1.Size += item.Size;
                v1.Length += item.Length;
            }
            // foreach (var item in SupportFormats)
            // {
            //     v1.Dfns.Add(item.NewDescription);
            // }
            if (QnExtras != null)
                //获取可用清晰度
                foreach (var node in QnExtras)
                {
                    v1.Dfns.Add(node.Qn);
                }


            if (myOption.OnlyHevc && v1.codecs == "AVC")
            {
                // Log(hevc && v1.codecs == "AVC");
            }
            else
            {
                ls.Add(v1);
            }

            return (ls, v1);
        }



        public (int, List<Video>, List<Audio>) GetVideoInfos(Page p, MyOption myOption)
        {
            List<Video> videoTracks = new List<Video>();
            List<Audio> audioTracks = new List<Audio>();
            Dictionary<string, string> dir = new Dictionary<string, string>();

            foreach (var item in SupportFormats)
            {
                dir.Add(item.Quality.ToString(), item.NewDescription);
            }

            int pDur = p.dur;
            //处理未获取到视频时长的情况
            if (p.dur == 0)
                p.dur = Dash.Duration;

            if (pDur == 0)
                pDur = Timelength / 1000;


            bool reParse = false;
        reParse:
            // webJson = GetPlayJson(aidOri, p.aid, p.cid, p.epid, tvApi, "125");
            // playData = webResp.Data;
            // if (webResp.Data == null)
            // {
            // playData = webResp.Result;
            // }
            // if (reParse)
            //     webJson = BBDownParser.GetPlayJson(aidOri, p.aid, p.cid, p.epid, tvApi, "125");
            if (Dash.Video != null)
                foreach (var node in Dash.Video)
                {
                    Video v = new Video();
                    v.id = node.Id;
                    if (dir.ContainsKey(node.Id))
                    {
                        v.dfn = dir[node.Id];
                    }

                    v.bandwith = node.Bandwidth / 1000;
                    v.baseUrl = node.BaseUrl;
                    v.codecs = node.Codecid == "12" ? "HEVC" : "AVC";

                    if (myOption.OnlyHevc && v.codecs == "AVC") continue;
                    if (!videoTracks.Contains(v)) videoTracks.Add(v);
                }

            //此处处理免二压视频，需要单独再请求一次
            if (!reParse)
            {
                reParse = true;
                goto reParse;
            }

            if (Dash.Audio != null)
                foreach (var node in Dash.Audio)
                {
                    Audio a = new Audio();
                    a.id = node.Id;
                    a.dfn = node.Id;
                    a.bandwith = node.Bandwidth / 1000;
                    a.baseUrl = node.BaseUrl;
                    a.codecs = "M4A";
                    audioTracks.Add(a);
                }

            return (pDur, videoTracks, audioTracks);
        }


    }
    public partial class QnExtra
    {
        [JsonProperty("attribute")]
        public long Attribute { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("icon2")]
        public string Icon2 { get; set; }

        [JsonProperty("need_login")]
        public bool NeedLogin { get; set; }

        [JsonProperty("need_vip")]
        public bool NeedVip { get; set; }

        [JsonProperty("qn")]
        public string Qn { get; set; }
    }
}