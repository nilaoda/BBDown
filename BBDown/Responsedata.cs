using System.Collections.Generic;
using Newtonsoft.Json;
using static BBDown.Program;

namespace BBDown
{

    public interface IBBDownParse
    {
        string GetHightestQN();
        (List<Video>, Video) GetVideoInfo(MyOption myOption);
        (int, List<Video>, List<Audio>) GetVideoInfos(Page p, MyOption myOption);
    }
    class ResponseBase<T, F>
    {

        [JsonProperty("code")]
        public string Code
        {
            get; set;
        }
        [JsonProperty("message")]
        public string Message
        {
            get; set;
        }
        [JsonProperty("ttl")]
        public string Ttl
        {
            get; set;
        }
        [JsonProperty("result")]
        public T Result { get; set; }

        [JsonProperty("data")]
        public T Data { get; set; }

    }
    class PlayInfoResp : ResponseBase<PlayInfo, Data>, IBBDownParse
    {

        public string GetHightestQN()
        {
            List<int> ls = new List<int>();
            if (Data != null)
            {
                ls = Data.AcceptQuality;
            }
            else if (Result != null)
            {
                ls = Result.AcceptQuality;
            }

            if (ls == null || ls.Count == 0)
                return "125";

            ls.Sort();
            ls.Reverse();
            return ls[0].ToString();
        }
        public (List<Video>, Video) GetVideoInfo(MyOption myOption)
        {
            Video v1 = new Video();
            v1.Clips = new List<string>();
            v1.Dfns = new List<string>();
            Data data = Data;
            if (data == null)
            {
                data = Result;
            }

            v1.Format = data.Format;
            v1.id = data.Quality;
            v1.codecs = data.VideoCodecid == "12" ? "HEVC" : "AVC";
            v1.baseUrl = "";

            foreach (var item in data.SupportFormats)
            {
                if (item.Quality.ToString() == v1.id)
                {
                    v1.dfn = item.NewDescription;
                    break;
                }
            }

            //获取所有分段
            foreach (var item in data.Durl)
            {
                v1.Clips.Add(item.Url);
                v1.Size += item.Size;
                v1.Length += item.Length;
            }
            foreach (var item in data.SupportFormats)
            {
                v1.Dfns.Add(item.Quality.ToString());
            }

            List<Video> ls = new List<Video>();
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

            Data playData = Data;
            if (Data == null)
                playData = Result;

            Dictionary<string, string> dir = new Dictionary<string, string>();

            foreach (var item in playData.SupportFormats)
            {
                dir.Add(item.Quality.ToString(), item.NewDescription);
            }

            int pDur = p.dur;
            //处理未获取到视频时长的情况
            if (p.dur == 0)
                pDur = playData.Dash.Duration;
            if (pDur == 0)
                pDur = playData.Timelength / 1000;

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
            if (playData.Dash.Video != null)
                foreach (var node in playData.Dash.Video)
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
                    v.res = $"{node.Width}x{node.Height}";
                    v.fps = node.FrameRate;
                    if (myOption.OnlyHevc && v.codecs == "AVC") continue;
                    if (!videoTracks.Contains(v)) videoTracks.Add(v);
                }

            //此处处理免二压视频，需要单独再请求一次
            if (!reParse)
            {
                reParse = true;
                goto reParse;
            }

            if (playData.Dash.Audio != null)
                foreach (var node in playData.Dash.Audio)
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


        public Dictionary<int, Selector> GetSelector()
        {
            Dictionary<int, Selector> dir = new Dictionary<int, Selector>();
            SupportFormat[] formats;
            if (Data != null)
            {
                formats = Data.SupportFormats;
            }
            else if (Result != null)
            {
                formats = Result.SupportFormats;
            }
            else
            {
                formats = new SupportFormat[0];
            }
            for (int i = 0; i < formats.Length; i++)
            {
                dir.Add(i, new Selector()
                {
                    Quality = formats[i].Quality,
                    Description = formats[i].NewDescription,
                });
            }
            return dir;
        }


        public class Selector
        {
            public int Index { get; set; }

            public int Quality { get; set; }

            public string Description { get; set; }

            public override string ToString()
            {

                return $"{Index}:{Description}";
            }
        }

    }

    public class SupportFormat
    {
        [JsonProperty("display_desc")]
        public string DisplayDesc { get; set; }

        [JsonProperty("superscript")]
        public string Superscript { get; set; }

        [JsonProperty("need_login", NullValueHandling = NullValueHandling.Ignore)]
        public bool? NeedLogin { get; set; }

        [JsonProperty("format")]
        public string Format { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("need_vip", NullValueHandling = NullValueHandling.Ignore)]
        public bool? NeedVip { get; set; }

        [JsonProperty("quality")]
        public int Quality { get; set; }

        [JsonProperty("new_description")]
        public string NewDescription { get; set; }

    }

    public class Durl
    {
        [JsonProperty("size")]
        public double Size { get; set; }

        [JsonProperty("ahead")]
        public string Ahead { get; set; }

        [JsonProperty("length")]
        public double Length { get; set; }

        [JsonProperty("vhead")]
        public string Vhead { get; set; }

        [JsonProperty("backup_url")]
        public string[] BackupUrl { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("order")]
        public int Order { get; set; }

        [JsonProperty("md5")]
        public string Md5 { get; set; }
    }

    public class Dash
    {
        [JsonProperty("duration")]
        public int Duration { get; set; }

        [JsonProperty("minBufferTime")]
        public double MinBufferTime { get; set; }

        [JsonProperty("min_buffer_time")]
        public double DashMinBufferTime { get; set; }


        [JsonProperty("video")]
        public AudioEntry[] Video { get; set; }

        [JsonProperty("audio")]
        public AudioEntry[] Audio { get; set; }
    }

}