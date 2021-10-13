using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace BBDown
{
    class BBDownAria2c
    {
        public static async Task<int> RunCommandCodeAsync(string command, string args)
        {
            if (File.Exists(Path.Combine(Program.APP_DIR, $"{command}")))
                command = Path.Combine(Program.APP_DIR, $"{command}");
            if (File.Exists(Path.Combine(Program.APP_DIR, $"{command}.exe")))
                command = Path.Combine(Program.APP_DIR, $"{command}.exe");
            using Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = false;
            p.StartInfo.FileName = command;
            p.StartInfo.Arguments = args;
            p.Start();
            await p.WaitForExitAsync();
            return p.ExitCode;
        }

        public static async Task DownloadFileByAria2cAsync(string url, string path, string proxy)
        {
            var headerArgs = "";
            if (!url.Contains("platform=android_tv_yst") && !url.Contains("platform=android"))
                headerArgs += " --header=\"Referer: https://www.bilibili.com\"";
            headerArgs += " --header=\"User-Agent: Mozilla/5.0\"";
            headerArgs += $" --header=\"Cookie: {Program.COOKIE}\"";
            await RunCommandCodeAsync("aria2c", $"{(proxy == "" ? "" : "--all-proxy=" + proxy)} --auto-file-renaming=false --download-result=hide --allow-overwrite=true --console-log-level=warn -x16 -s16 -k5M {headerArgs} \"{url}\" -d \"{Path.GetDirectoryName(path)}\" -o \"{Path.GetFileName(path)}\"");
        }
    }
}
