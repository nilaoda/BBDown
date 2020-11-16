using System.Diagnostics;
using System.IO;

namespace BBDown
{
    class BBDownAria2c
    {
        public static int RunCommandCode(string command, string args)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = false;
            p.StartInfo.FileName = command;
            p.StartInfo.Arguments = args;
            p.Start();
            p.WaitForExit();
            return p.ExitCode;
        }

        public static void DownloadFileByAria2c(string url, string path)
        {
            var headerArgs = "";
            if (!url.Contains("platform=android_tv_yst"))
                headerArgs += " --header=\"Referer: https://www.bilibili.com\"";
            headerArgs += " --header=\"User-Agent: Mozilla/5.0\"";
            headerArgs += $" --header=\"Cookie: {Program.COOKIE}\"";
            RunCommandCode("aria2c", $"--auto-file-renaming=false --download-result=hide --allow-overwrite=true --console-log-level=warn -x16 -s16 -k5M {headerArgs} \"{url}\" -d \"{Path.GetDirectoryName(path)}\" -o \"{Path.GetFileName(path)}\"");
        }
    }
}
