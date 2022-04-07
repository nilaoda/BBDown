using System;
using System.Collections.Generic;
using System.Text;

namespace BBDown
{
    class BBDownLogger
    {
        public static bool DEBUG_LOG = false;

        public static void Log(object text, bool enter = true)
        {
            Console.Write(DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss.fff]") + " - " + text);
            if (enter) Console.WriteLine();
        }

        public static void LogWarn(object text, bool enter) => LogWarn(text.ToString(), enter);

        public static void LogWarn(string text, bool enter = true)
        {
            Console.Write(DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss.fff]") + " - ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(text);
            Console.ResetColor();
            if (enter) Console.WriteLine();
        }

        public static void LogError(object text)
        {
            Console.Write(DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss.fff]") + " - ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(text);
            Console.ResetColor();
            Console.WriteLine();
        }

        public static void LogColor(object text, bool time = true)
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

        public static void LogDebug(string toFormat, params object[] args)
        {
            if (DEBUG_LOG)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss.fff]") + " - ");
                if (args.Length > 0)
                    Console.Write(string.Format(toFormat, args).Trim());
                else
                    Console.Write(toFormat);
                Console.ResetColor();
                Console.WriteLine();
            }
        }
    }
}
