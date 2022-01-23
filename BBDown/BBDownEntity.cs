using System;
using System.Collections.Generic;
using System.Text;

namespace BBDown
{
    class BBDownEntity
    {
        public class Page
        {
            public int index;
            public string aid;
            public string cid;
            public string epid;
            public string title;
            public int dur;
            public string res;
            public string cover;
            public List<ViewPoint> points = new List<ViewPoint>();

            public Page(int index, string aid, string cid, string epid, string title, int dur, string res)
            {
                this.aid = aid;
                this.index = index;
                this.cid = cid;
                this.epid = epid;
                this.title = title;
                this.dur = dur;
                this.res = res;
            }

            public Page(int index, string aid, string cid, string epid, string title, int dur, string res, string cover)
            {
                this.aid = aid;
                this.index = index;
                this.cid = cid;
                this.epid = epid;
                this.title = title;
                this.dur = dur;
                this.res = res;
                this.cover = cover;
            }

            public override bool Equals(object obj)
            {
                return obj is Page page &&
                       aid == page.aid &&
                       cid == page.cid &&
                       epid == page.epid;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(aid, cid, epid);
            }
        }

        public class ViewPoint
        {
            public string title;
            public int start;
            public int end;
        }

        public class Video
        {
            public string id;
            public string dfn;
            public string baseUrl;
            public string res;
            public string fps;
            public string codecs;
            public long bandwith;
            public int dur;
            public double size;

            public override bool Equals(object obj)
            {
                return obj is Video video &&
                       id == video.id &&
                       dfn == video.dfn &&
                       res == video.res &&
                       fps == video.fps &&
                       codecs == video.codecs &&
                       bandwith == video.bandwith &&
                       dur == video.dur;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(id, dfn, res, fps, codecs, bandwith, dur);
            }
        }

        public class Audio
        {
            public string id;
            public string dfn;
            public string baseUrl;
            public string codecs;
            public long bandwith;
            public int dur;

            public override bool Equals(object obj)
            {
                return obj is Audio audio &&
                       id == audio.id &&
                       dfn == audio.dfn &&
                       codecs == audio.codecs &&
                       bandwith == audio.bandwith &&
                       dur == audio.dur;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(id, dfn, codecs, bandwith, dur);
            }
        }

        public class Subtitle
        {
            public string lan;
            public string url;
            public string path;
        }

        public class Clip
        {
            public int index;
            public long from;
            public long to;
        }
    }
}
