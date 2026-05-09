using System.Collections.ObjectModel;
using BBDown.Core;
using BBDown.GUI.Models;

namespace BBDown.GUI.Services;

public class DownloadService
{
    public static DownloadService Instance { get; } = new DownloadService();

    public ObservableCollection<DownloadTask> Tasks { get; } = [];

    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private CancellationTokenSource? _cts;

    private DownloadService()
    {
    }

    public DownloadTask Enqueue(string url, Dictionary<string, object> options)
    {
        var task = new DownloadTask { Url = url, Options = options };
        Tasks.Add(task);
        _ = ProcessQueue();
        return task;
    }

    private async Task ProcessQueue()
    {
        await _semaphore.WaitAsync();
        try
        {
            foreach (var task in Tasks)
            {
                if (task.Status == DownloadTaskStatus.Queued)
                {
                    await ExecuteTask(task);
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task ExecuteTask(DownloadTask task)
    {
        _cts = new CancellationTokenSource();
        var progress = new DownloadProgressAdapter(task);

        try
        {
            progress.ReportStart(task.Id, task.Url);

            // Stage 1: Parsing
            task.Status = DownloadTaskStatus.Parsing;
            progress.ReportProgress(task.Id, "parsing", 10, "正在解析视频信息...");
            await Task.Delay(500, _cts.Token);

            // Stage 2: Downloading
            task.Status = DownloadTaskStatus.Downloading;
            progress.ReportProgress(task.Id, "downloading", 30, "正在下载...");
            await Task.Delay(500, _cts.Token);

            // Stage 3: Muxing
            task.Status = DownloadTaskStatus.Muxing;
            progress.ReportProgress(task.Id, "muxing", 80, "正在混流...");
            await Task.Delay(500, _cts.Token);

            // Stage 4: Done
            progress.ReportProgress(task.Id, "done", 100, "下载完成");
            task.Status = DownloadTaskStatus.Completed;

            // TODO: Replace above simulation with actual BBDown.Core call:
            // var option = BuildOption(task);
            // await BBDown.Program.RunAppWithProgress(option, progress, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            task.Status = DownloadTaskStatus.Stopped;
            progress.ReportLog(task.Id, "warn", "下载已停止");
        }
        catch (Exception ex)
        {
            task.Status = DownloadTaskStatus.Failed;
            progress.ReportLog(task.Id, "error", ex.Message);
        }
    }

    public void StopTask(string taskId)
    {
        _cts?.Cancel();
    }

    // TODO: construct MyOption from task.Options and ConfigService.Instance.Config
    // when BBDown.Core integration is added
    private object? BuildOption(DownloadTask task)
    {
        throw new NotImplementedException("BuildOption will be implemented when BBDown.Core download integration is added.");
    }
}

public class DownloadProgressAdapter : IDownloadProgress
{
    private readonly DownloadTask _task;

    public DownloadProgressAdapter(DownloadTask task)
    {
        _task = task;
    }

    public void ReportStart(string taskId, string title)
    {
        _task.Title = title;
    }

    public void ReportProgress(string taskId, string stage, double percent, string message)
    {
        _task.Progress = percent;
        _task.StageText = message;
        _task.LogLines.Add($"[{stage}] {message}");
    }

    public void ReportLog(string taskId, string level, string message)
    {
        _task.LogLines.Add($"[{level}] {message}");
    }
}
