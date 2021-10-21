using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvertTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = "./1.xml";
            string outPath = "./2.ass";
            BBDanmaku.Convert(path, outPath);
            string aa = Console.ReadLine();
        }
    }
}
