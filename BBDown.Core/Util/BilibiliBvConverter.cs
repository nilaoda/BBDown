using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBDown.Core.Util
{
    //code from: https://www.zhihu.com/question/381784377/answer/1099438784
    internal class BilibiliBvConverter
    {
        private static string table = "fZodR9XQDSUm21yCkr6zBqiveYah8bt4xsWpHnJE7jL5VG3guMTKNPAwcF";
        private static Dictionary<char, long> tr = new Dictionary<char, long>();

        static BilibiliBvConverter()
        {
            for (int i = 0; i < 58; i++)
            {
                tr[table[i]] = i;
            }
        }

        public static long Decode(string x)
        {
            long r = 0;
            for (int i = 0; i < 6; i++)
            {
                r += tr[x[s[i]]] * (long)Math.Pow(58, i);
            }
            return (r - add) ^ xor;
        }

        public static string Encode(long x)
        {
            x = (x ^ xor) + add;
            char[] r = "BV1  4 1 7  ".ToCharArray();
            for (int i = 0; i < 6; i++)
            {
                r[s[i]] = table[(int)(x / (long)Math.Pow(58, i) % 58)];
            }
            return new string(r);
        }

        private static byte[] s = { 9, 8, 1, 6, 2, 4 };
        private static long xor = 177451812;
        private static long add = 8728348608;
    }
}
