using System;
using System.Collections.Generic;
using System.Text;
using static BBDown.Core.Entity.Entity;

namespace BBDown.Core.Entity
{
    public class VInfo
    {
        /// <summary>
        /// 视频index 用于番剧或课程判断当前选择的是第几集
        /// </summary>
        private string index;

        /// <summary>
        /// 视频标题
        /// </summary>
        private string title;

        /// <summary>
        /// 视频描述
        /// </summary>
        private string desc;

        /// <summary>
        /// 视频封面
        /// </summary>
        private string pic;

        /// <summary>
        /// 视频发布时间
        /// </summary>
        private string pubTime;

        private bool isBangumi;
        private bool isCheese;

        /// <summary>
        /// 番剧是否完结
        /// </summary>
        private bool isBangumiEnd;

        /// <summary>
        /// 视频分P信息
        /// </summary>
        private List<Page> pagesInfo;

        public string Title { get => title; set => title = value; }
        public string Desc { get => desc; set => desc = value; }
        public string Pic { get => pic; set => pic = value; }
        public string PubTime { get => pubTime; set => pubTime = value; }
        public bool IsBangumi { get => isBangumi; set => isBangumi = value; }
        public bool IsCheese { get => isCheese; set => isCheese = value; }
        public bool IsBangumiEnd { get => isBangumiEnd; set => isBangumiEnd = value; }
        public string Index { get => index; set => index = value; }
        public List<Page> PagesInfo { get => pagesInfo; set => pagesInfo = value; }
    }
}
