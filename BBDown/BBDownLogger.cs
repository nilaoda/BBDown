using System;
using System.Collections.Generic;
using System.Text;

namespace BBDown
{
    class BBDownLogger
    {
        public static void Log(string text, bool enter = true)
        {
            Console.Write(DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss.fff]") + " - " + text);
            if (enter) Console.WriteLine();
        }

        public static void LogError(string text)
        {
            Console.Write(DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss.fff]") + " - ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(text);
            Console.ResetColor();
            Console.WriteLine();
        }

        public static void LogColor(string text, bool time = true)
        {
            if (time)
                Console.Write(DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss.fff]") + " - ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            if (time)
                Console.Write(text);
            else
                Console.Write("                            " + text);
            Console.ResetColor();
            Console.WriteLine();
        }
    }
}
