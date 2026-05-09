using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BBDown.GUI.Models;
using BBDown.GUI.Services;

namespace BBDown.GUI.ViewModels;

public class DownloadsViewModel : INotifyPropertyChanged
{
    private DownloadTask? _selectedTask;
    private string _outputLog = "";

    public ObservableCollection<DownloadTask> Tasks { get; }

    public DownloadTask? SelectedTask
    {
        get => _selectedTask;
        set
        {
            if (_selectedTask != value)
            {
                // Unsubscribe from the old task's log collection
                if (_selectedTask != null)
                {
                    _selectedTask.LogLines.CollectionChanged -= OnSelectedTaskLogLinesChanged;
                }

                _selectedTask = value;

                // Subscribe to the new task's log collection
                if (_selectedTask != null)
                {
                    _selectedTask.LogLines.CollectionChanged += OnSelectedTaskLogLinesChanged;
                }

                OnPropertyChanged();
                RefreshOutputLog();
            }
        }
    }

    public string OutputLog
    {
        get => _outputLog;
        private set
        {
            if (_outputLog != value)
            {
                _outputLog = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand StopCommand { get; }
    public ICommand ClearCompletedCommand { get; }

    public DownloadsViewModel()
    {
        Tasks = DownloadService.Instance.Tasks;
        StopCommand = new RelayCommand(param =>
        {
            if (param is DownloadTask task)
                DownloadService.Instance.StopTask(task.Id);
        });
        ClearCompletedCommand = new RelayCommand(_ => ClearCompleted());
    }

    private void ClearCompleted()
    {
        var completed = Tasks
            .Where(t => t.Status is DownloadTaskStatus.Completed or DownloadTaskStatus.Failed or DownloadTaskStatus.Stopped)
            .ToList();

        foreach (var t in completed)
        {
            Tasks.Remove(t);
        }
    }

    private void OnSelectedTaskLogLinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshOutputLog();
    }

    private void RefreshOutputLog()
    {
        OutputLog = SelectedTask != null
            ? string.Join(Environment.NewLine, SelectedTask.LogLines)
            : "";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
