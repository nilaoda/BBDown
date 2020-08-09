using System;
using System.Collections.Generic;
using System.Text;

namespace BBDown
{
    class BBDownEntity
    {
        public struct Page
        {
            public int index;
            public string cid;
            public string title;
            public int dur;
            public string res;

            public Page(int index, string cid, string title, int dur, string res)
            {
                this.index = index;
                this.cid = cid;
                this.title = title;
                this.dur = dur;
                this.res = res;
            }
        }

        public struct Video
        {
            public string id;
            public string dfn;
            public string baseUrl;
            public string res;
            public string fps;
            public string codecs;
            public long bandwith;
        }

        public struct Audio
        {
            public string id;
            public string dfn;
            public string baseUrl;
            public string codecs;
            public long bandwith;
        }

        public struct Subtitle
        {
            public string lan;
            public string url;
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
