namespace BBDown.Core;

public interface IDownloadProgress
{
    void ReportStart(string taskId, string title);
    void ReportProgress(string taskId, string stage, double percent, string message);
    void ReportLog(string taskId, string level, string message);
}
