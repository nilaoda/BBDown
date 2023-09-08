using System.Diagnostics;
using System.Text;
using static BBDown.BBDownUtil;
using static BBDown.Core.Logger;
using System.IO;
using BBDown.Mux;

namespace BBDown
{
    partial class BBDownMuxer
    {
        public static string? FFMPEG;
        public static string? MP4BOX;

        public static IMuxer CreateMuxer(MyOption option)
        {
            if (option.UseMP4box)
            {
                if (MP4BOX == null)
                    throw new FileNotFoundException("Couldn't find mp4box");
                return new MP4BoxMuxer(MP4BOX);
            }
            if (FFMPEG == null)
                throw new FileNotFoundException("Couldn't find ffmpeg");
            return new FFMPEGMuxer(FFMPEG);
        }

        private static int RunExe(string app, string parms, bool customBin = false)
        {
            int code = 0;
            Process p = new();
            p.StartInfo.FileName = app;
            p.StartInfo.Arguments = parms;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = false;
            p.ErrorDataReceived += delegate (object sendProcess, DataReceivedEventArgs output) {
                if (!string.IsNullOrWhiteSpace(output.Data))
                    Log(output.Data);
            };
            p.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            p.Start();
            p.BeginErrorReadLine();
            p.WaitForExit();
            p.Close();
            p.Dispose();

            return code;
        }

        public static void MergeFLV(string[] files, string outPath)
        {
            if (files.Length == 1)
            {
                File.Move(files[0], outPath);
            }
            else
            {
                foreach (var file in files)
                {
                    var tmpFile = Path.Combine(Path.GetDirectoryName(file)!, Path.GetFileNameWithoutExtension(file) + ".ts");
                    var arguments = $"-loglevel warning -y -i \"{file}\" -map 0 -c copy -f mpegts -bsf:v h264_mp4toannexb \"{tmpFile}\"";
                    LogDebug("ffmpeg命令: {0}", arguments);
                    RunExe("ffmpeg", arguments);
                    File.Delete(file);
                }
                var f = GetFiles(Path.GetDirectoryName(files[0])!, ".ts");
                CombineMultipleFilesIntoSingleFile(f, outPath);
                foreach (var s in f) File.Delete(s);
            }
        }
    }
}
