using System;
using System.Collections.Generic;
using System.Text;

namespace BBDown.Core.Entity
{
    public class Entity
    {
        public class Page
        {
            public int index;
            public string aid;
            public string cid;
            public string epid;
            public string videoTitle;
            public string pageTitle;
            public int dur;
            public string res;
            public string cover;
            public string desc;
            public string ownerName;
            public string ownerMid;
            public bool isSingleP;
            public List<ViewPoint> points = new List<ViewPoint>();

            public Page(int index, string aid, string cid, string epid, string videoTitle, string pageTitle, int dur, string res)
            {
                this.aid = aid;
                this.index = index;
                this.cid = cid;
                this.epid = epid;
                this.videoTitle = videoTitle;
                this.pageTitle = pageTitle;
                this.dur = dur;
                this.res = res;
            }

            public Page(int index, string aid, string cid, string epid, string videoTitle, string pageTitle, int dur, string res, string cover, string desc)
            {
                this.aid = aid;
                this.index = index;
                this.cid = cid;
                this.epid = epid;
                this.videoTitle = videoTitle;
                this.pageTitle = pageTitle;
                this.dur = dur;
                this.res = res;
                this.cover = cover;
                this.desc = desc;
            }

            public Page(int index, string aid, string cid, string epid, string videoTitle, string pageTitle, int dur, string res, string cover, string desc, string ownerName, string ownerMid)
            {
                this.aid = aid;
                this.index = index;
                this.cid = cid;
                this.epid = epid;
                this.videoTitle = videoTitle;
                this.pageTitle = pageTitle;
                this.dur = dur;
                this.res = res;
                this.cover = cover;
                this.desc = desc;
                this.ownerName = ownerName;
                this.ownerMid = ownerMid;
            }

            public Page(int index, Page page)
            {
                this.index = index;
                this.aid = page.aid;
                this.cid = page.cid;
                this.epid = page.epid;
                this.videoTitle = page.videoTitle;
                this.pageTitle = page.pageTitle;
                this.dur = page.dur;
                this.res = page.res;
                this.cover = page.cover;
                this.desc = page.desc;
                this.ownerName = page.ownerName;
                this.ownerMid = page.ownerMid;
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
