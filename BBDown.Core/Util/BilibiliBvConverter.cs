using System;
using System.Text;

namespace BBDown.Core.Util
{
    //code from: https://github.com/Colerar/abv/blob/main/src/lib.rs
    public static class BilibiliBvConverter
    {
        private const long XOR_CODE = 23442827791579L;
        private const long MASK_CODE = (1L << 51) - 1;

        private const long MAX_AID = MASK_CODE + 1;
        private const long MIN_AID = 1L;

        private const long BASE = 58L;
        private const byte BV_LEN = 9;

        private static readonly byte[] ALPHABET = Encoding.ASCII.GetBytes("FcwAPNKTMug3GV5Lj7EJnHpWsx4tb8haYeviqBz6rkCy12mUSDQX9RdoZf");

        private static readonly Dictionary<byte, long> REV_ALPHABETA = new Dictionary<byte, long>();

        static BilibiliBvConverter()
        {
            for (byte i = 0; i < ALPHABET.Length; i++)
            {
                REV_ALPHABETA[ALPHABET[i]] = i;
            }
        }

        public static string Encode(long avid)
        {
            if (avid < MIN_AID)
            {
                throw new Exception($"Av {avid} is smaller than {MIN_AID}");
            }
            if (avid >= MAX_AID)
            {
                throw new Exception($"Av {avid} is bigger than {MAX_AID}");
            }

            var bvid = new byte[BV_LEN];
            long tmp = (MAX_AID | avid) ^ XOR_CODE;

            for (byte i = BV_LEN - 1; tmp != 0; i--)
            {
                bvid[i] = ALPHABET[tmp % BASE];
                tmp /= BASE;
            }

            (bvid[0], bvid[6]) = (bvid[6], bvid[0]);
            (bvid[1], bvid[4]) = (bvid[4], bvid[1]);

            return "BV1" + Encoding.ASCII.GetString(bvid);
        }

        public static long Decode(string bvid_str)
        {
            if (bvid_str.Length != BV_LEN)
            {
                throw new Exception($"Bv BV1{bvid_str} must to be 12 char");
            }

            byte[] bvid = Encoding.ASCII.GetBytes(bvid_str);
            (bvid[0], bvid[6]) = (bvid[6], bvid[0]);
            (bvid[1], bvid[4]) = (bvid[4], bvid[1]);

            long avid = 0;
            foreach (byte b in bvid)
            {
                avid = avid * BASE + REV_ALPHABETA[b];
            }

            return (avid & MASK_CODE) ^ XOR_CODE;
        }
    }
}
