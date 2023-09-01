using BBDown.Core.Util;
using System.Diagnostics.CodeAnalysis;

namespace BBDown.Core.Entity
{
    public class Entity
    {
        public class Page
        {
            public required int index;
            public required string aid;
            public required string cid;
            public required string epid;
            public required string title;
            public required int dur;
            public required string res;
            public required long pubTime;
            public string? cover;
            public string? desc;
            public string? ownerName;
            public string? ownerMid;
            public string bvid
            {
                get => BilibiliBvConverter.Encode(long.Parse(aid));
            }
            public List<ViewPoint> points = new();

            [SetsRequiredMembers]
            public Page(int index, string aid, string cid, string epid, string title, int dur, string res, long pubTime)
            {
                this.aid = aid;
                this.index = index;
                this.cid = cid;
                this.epid = epid;
                this.title = title;
                this.dur = dur;
                this.res = res;
                this.pubTime = pubTime;
            }

            [SetsRequiredMembers]
            public Page(int index, string aid, string cid, string epid, string title, int dur, string res, long pubTime, string cover)
            {
                this.aid = aid;
                this.index = index;
                this.cid = cid;
                this.epid = epid;
                this.title = title;
                this.dur = dur;
                this.res = res;
                this.pubTime = pubTime;
                this.cover = cover;
            }

            [SetsRequiredMembers]
            public Page(int index, string aid, string cid, string epid, string title, int dur, string res, long pubTime, string cover, string desc)
            {
                this.aid = aid;
                this.index = index;
                this.cid = cid;
                this.epid = epid;
                this.title = title;
                this.dur = dur;
                this.res = res;
                this.pubTime = pubTime;
                this.cover = cover;
                this.desc = desc;
            }

            [SetsRequiredMembers]
            public Page(int index, string aid, string cid, string epid, string title, int dur, string res, long pubTime, string cover, string desc, string ownerName, string ownerMid)
            {
                this.aid = aid;
                this.index = index;
                this.cid = cid;
                this.epid = epid;
                this.title = title;
                this.dur = dur;
                this.res = res;
                this.pubTime = pubTime;
                this.cover = cover;
                this.desc = desc;
                this.ownerName = ownerName;
                this.ownerMid = ownerMid;
            }

            [SetsRequiredMembers]
            public Page(int index, Page page)
            {
                this.index = index;
                this.aid = page.aid;
                this.cid = page.cid;
                this.epid = page.epid;
                this.title = page.title;
                this.dur = page.dur;
                this.res = page.res;
                this.pubTime = page.pubTime;
                this.cover = page.cover;
                this.ownerName = page.ownerName;
                this.ownerMid = page.ownerMid;
            }

            public override bool Equals(object? obj)
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
            public required string title;
            public required int start;
            public required int end;
        }

        public class Video
        {
            public required string id;
            public required string dfn;
            public required string baseUrl;
            public string? res;
            public string? fps;
            public required string codecs;
            public long bandwith;
            public int dur;
            public double size;

            public override bool Equals(object? obj)
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
            public required string id;
            public required string dfn;
            public required string baseUrl;
            public required string codecs;
            public required long bandwith;
            public required int dur;

            public override bool Equals(object? obj)
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
            public required string lan;
            public required string url;
            public required string path;
        }

        public class Clip
        {
            public required int index;
            public required long from;
            public required long to;
        }

        public class AudioMaterial
        {
            public required string title;
            public required string personName;
            public required string path;

            [SetsRequiredMembers]
            public AudioMaterial(string title, string personName, string path)
            {
                this.title = title;
                this.personName = personName;
                this.path = path;
            }

            [SetsRequiredMembers]
            public AudioMaterial(AudioMaterialInfo audioMaterialInfo)
            {
                this.title = audioMaterialInfo.title;
                this.personName = audioMaterialInfo.personName;
                this.path = audioMaterialInfo.path;
            }
        }

        public class AudioMaterialInfo
        {
            public required string title;
            public required string personName;
            public required string path;
            public required List<Audio> audio;
        }
    }
}
