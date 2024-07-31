using static BBDown.Core.Entity.Entity;

namespace BBDown.Core.Entity
{
    public class VInfo
    {
        /// <summary>
        /// 视频标题
        /// </summary>
        public required string Title { get; set; }

        /// <summary>
        /// 视频描述
        /// </summary>
        public required string Desc { get; set; }

        /// <summary>
        /// 视频封面
        /// </summary>
        public required string Pic { get; set; }

        /// <summary>
        /// 视频发布时间
        /// </summary>
        public required long PubTime { get; set; }
        public bool IsBangumi { get; set; }
        public bool IsCheese { get; set; }

        /// <summary>
        /// 番剧是否完结
        /// </summary>
        public bool IsBangumiEnd { get; set; }

        /// <summary>
        /// 视频index 用于番剧或课程判断当前选择的是第几集
        /// </summary>
        public string? Index { get; set; }

        /// <summary>
        /// 视频分P信息
        /// </summary>
        public required List<Page> PagesInfo { get; set; }

        /// <summary>
        /// 是否为互动视频
        /// </summary>
        public bool IsSteinGate { get; set; }
    }
}
