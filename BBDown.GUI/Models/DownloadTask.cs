using System.Collections.ObjectModel;

namespace BBDown.GUI.Models;

public enum DownloadTaskStatus
{
    Queued,
    Parsing,
    Downloading,
    Muxing,
    Completed,
    Failed,
    Stopped
}

public class DownloadTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public DownloadTaskStatus Status { get; set; } = DownloadTaskStatus.Queued;
    public double Progress { get; set; }
    public string StageText { get; set; } = "";
    public ObservableCollection<string> LogLines { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public Dictionary<string, object> Options { get; set; } = [];
}
