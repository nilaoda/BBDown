using System;

namespace BBDown
{
    [Flags] public enum ItemType
    {
        None = 0,
        Video = 1,
        Audio = 2,
        Subtitle = 4,
        Danmaku = 8,
        Cover = 16
    }

    public static class ItemTypeHelper
    {
        public static ItemType ParseFullName(string[] items)
        {
            var result = ItemType.None;
            foreach (var item in items)
            {
                if (Enum.TryParse<ItemType>(item, true, out var result1))
                    result |= result1;
            }
            return result;
        }

        public static ItemType ParseAbbr(char abbr) => char.ToLower(abbr) switch
        {
            'v' => ItemType.Video,
            'a' => ItemType.Audio,
            's' => ItemType.Subtitle,
            'd' => ItemType.Danmaku,
            'c' => ItemType.Cover,
            _ => ItemType.None,
        };

        public static ItemType ParseMultiAbbrString(string abbrString)
        {
            var result = ItemType.None;
            foreach (var @char in abbrString)
                result |= ParseAbbr(@char);
            return result;
        }
    }
}
