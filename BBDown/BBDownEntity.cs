using System;
using System.Collections.Generic;
using System.Text;

namespace BBDown
{
    partial class BBDownEntity
    {
    //     public struct Page
    //     {
    //         public int index;
    //         public string aid;
    //         public string cid;
    //         public string epid;
    //         public string title;
    //         public int dur;
    //         public string res;

    //         public Page(int index, string aid, string cid, string epid, string title, int dur, string res)
    //         {
    //             this.aid = aid;
    //             this.index = index;
    //             this.cid = cid;
    //             this.epid = epid;
    //             this.title = title;
    //             this.dur = dur;
    //             this.res = res;
    //         }
    //     }

        public struct Subtitle
        {
            public string lan;
            public string url;
            // public partial struct Video
            // {
            //     public string id;
            //     public string dfn;
            //     public string baseUrl;
            //     public string res;
            //     public string fps;
            //     public string codecs;
            //     public long bandwith;

            //     /// <summary>
            //     /// 视频尺寸
            //     /// </summary>
            //     /// <value></value>
            //     public double Size { get; set; }

            //     /// <summary>
            //     /// 
            //     /// </summary>
            //     /// <value></value>
            //     public double Length { get; set; }


            //     public List<string> Clips { get; set; }
            //     public List<string> Dfns { get; set; }


            //     public override bool Equals(object obj)
            //     {
            //         return obj is Video video &&
            //                id == video.id &&
            //                dfn == video.dfn &&
            //                res == video.res &&
            //                fps == video.fps &&
            //                codecs == video.codecs &&
            //                bandwith == video.bandwith;
            //     }

            //     public override int GetHashCode()
            //     {
            //         return HashCode.Combine(id, dfn, res, fps, codecs, bandwith);
            //     }
            // }

            // public struct Audio
            // {
            //     public string id;
            //     public string dfn;
            //     public string baseUrl;
            //     public string codecs;
            //     public long bandwith;

            //     public override bool Equals(object obj)
            //     {
            //         return obj is Audio audio &&
            //                id == audio.id &&
            //                dfn == audio.dfn &&
            //                codecs == audio.codecs &&
            //                bandwith == audio.bandwith;
            //     }

            //     public override int GetHashCode()
            //     {
            //         return HashCode.Combine(id, dfn, codecs, bandwith);
            //     }
            // }
            public string path;
        }

        public struct Clip
        {
            public int index;
            public long from;
            public long to;
        }
    }
}
