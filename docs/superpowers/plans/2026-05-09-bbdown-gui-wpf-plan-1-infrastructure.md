# BBDown WPF GUI 实施计划（一）：基础设施

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**目标：** 搭建 BBDown.GUI 项目骨架，重构 BBDown.Core 进度接口，实现 Models 和 Services 层。

**架构：** 新建 WPF 项目引用 wpfui + BBDown.Core，通过 IDownloadProgress 接口实现进度回调，DownloadService 封装下载逻辑，ConfigService 处理 JSON 配置持久化。

**技术栈：** .NET 9 + WPF + wpfui (lepoco) + BBDown.Core

**运行时标识符：** `win-x64`, `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`

---

### Task 1: 创建 Git 分支并初始化 BBDown.GUI 项目

**Files:**
- Create: `BBDown.GUI/BBDown.GUI.csproj`
- Create: `BBDown.GUI/App.xaml`
- Create: `BBDown.GUI/App.xaml.cs`
- Modify: `BBDown.sln`

- [ ] **Step 1: 创建分支**

```bash
git checkout -b feature/gui-wpf
```

- [ ] **Step 2: 创建项目文件 BBDown.GUI.csproj**

```xml
<!-- Target: net9.0-windows, 启用 WPF -->
<!-- PackageReference: WPF-UI 最新版 -->
<!-- ProjectReference: ../BBDown.Core/BBDown.Core.csproj -->
```

- [ ] **Step 3: 创建 App.xaml**

```xml
<!-- 合并 wpfui 资源字典：ThemesDictionary + ControlsDictionary -->
<!-- 默认主题跟随系统 -->
```

- [ ] **Step 4: 创建 App.xaml.cs**

```csharp
// 继承 Application，OnStartup 中配置 ApplicationThemeManager
// 设置 Mica 背景
// 创建并显示 MainWindow
```

- [ ] **Step 5: 将项目加入 solution**

```bash
dotnet sln BBDown.sln add BBDown.GUI/BBDown.GUI.csproj
```

- [ ] **Step 6: 验证构建**

```bash
dotnet build BBDown.GUI/BBDown.GUI.csproj
```

- [ ] **Step 7: 提交**

```bash
git add BBDown.GUI/ BBDown.sln
git commit -m "feat: add BBDown.GUI WPF project with wpfui"
```

---

### Task 2: 提取 BBDown.Core 进度回调接口

**Files:**
- Create: `BBDown.Core/IDownloadProgress.cs`
- Modify: `BBDown.Core/BBDown.Core.csproj` — 无需改动（已有 net9.0）

- [ ] **Step 1: 创建 IDownloadProgress 接口**

```csharp
// BBDown.Core/IDownloadProgress.cs

// 接口定义三个方法（伪代码）：
// interface IDownloadProgress:
//     void ReportStart(string taskId, string title)
//     void ReportProgress(string taskId, string stage, double percent, string message)
//         // stage: "parsing" | "downloading" | "muxing" | "done"
//     void ReportLog(string taskId, string level, string message)
//         // level: "info" | "warn" | "error"
```

- [ ] **Step 2: 验证 BBDown.Core 构建无影响**

```bash
dotnet build BBDown.Core/BBDown.Core.csproj
```

- [ ] **Step 3: 提交**

```bash
git add BBDown.Core/IDownloadProgress.cs
git commit -m "feat: add IDownloadProgress interface for download callbacks"
```

---

### Task 3: 创建 Models

**Files:**
- Create: `BBDown.GUI/Models/DownloadTask.cs`
- Create: `BBDown.GUI/Models/AppConfig.cs`

- [ ] **Step 1: 创建 DownloadTask 模型**

```csharp
// BBDown.GUI/Models/DownloadTask.cs

// 伪代码：
// class DownloadTask:
//     property Id: string = Guid
//     property Url: string
//     property Title: string
//     property Status: enum { Queued, Parsing, Downloading, Muxing, Completed, Failed, Stopped }
//     property Progress: double (0-100)
//     property StageText: string
//     property LogLines: ObservableCollection<string>
//     property CreatedAt: DateTime
//     property Options: Dictionary<string, object>  // 下载参数
```

- [ ] **Step 2: 创建 AppConfig 模型**

```csharp
// BBDown.GUI/Models/AppConfig.cs

// 伪代码：
// class AppConfig:
//     // 常规设置
//     property DownloadSource: string = ""  // 空=网页端, "tv"/"app"/"intl"
//     property EncodingPriority: string = ""
//     property FfmpegPath: string = ""
//     property WorkDir: string = ""
//
//     // 下载选项
//     property AudioOnly: bool = false
//     property VideoOnly: bool = false
//     property SubOnly: bool = false
//     property DownloadDanmaku: bool = false
//     property DanmakuFormats: string = ""
//
//     // 交互选项
//     property Interactive: bool = false
//     property OnlyShowInfo: bool = false
//     property HideStreams: bool = false
//     property Debug: bool = false
//
//     // 跳过选项
//     property SkipSubtitle: bool = false
//     property SkipCover: bool = false
//     property SkipMux: bool = false
//     property SkipAi: bool = true
//
//     // MP4Box
//     property UseMp4box: bool = false
//     property Mp4boxPath: string = ""
//
//     // Cookies
//     property AccessToken: string = ""
//     property Cookie: string = ""
//
//     // 文件名
//     property FilePattern: string = ""
//     property MultiFilePattern: string = ""
//
//     // 分P
//     property ShowAll: bool = false
//     property SelectPage: string = ""
//     property DelayPerPage: string = "0"
//
//     // aria2c
//     property UseAria2c: bool = false
//     property Aria2cPath: string = ""
//     property Aria2cProxy: string = ""
//     property Aria2cArgs: string = ""
//
//     // 其他
//     property MultiThread: bool = true
//     property ForceHttp: bool = true
//     property Language: string = ""
//     property UserAgent: string = ""
//
//     // 代理
//     property EnableProxy: bool = false
//     property Host: string = ""
//     property EpHost: string = ""
//     property Area: string = ""
//
//     // BBDown 程序位置
//     property BBDownPath: string = ""
```

- [ ] **Step 3: 验证构建**

```bash
dotnet build BBDown.GUI/BBDown.GUI.csproj
```

- [ ] **Step 4: 提交**

```bash
git add BBDown.GUI/Models/
git commit -m "feat: add DownloadTask and AppConfig models"
```

---

### Task 4: 实现 ConfigService

**Files:**
- Create: `BBDown.GUI/Services/ConfigService.cs`

- [ ] **Step 1: 实现 ConfigService**

```csharp
// BBDown.GUI/Services/ConfigService.cs

// 伪代码：
// class ConfigService:
//     static property Instance: ConfigService (单例)
//     property Config: AppConfig
//     private string _configPath = Path.Combine(AppContext.BaseDirectory, "config.json")
//
//     method Load():
//         if File.Exists(_configPath):
//             Config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(_configPath))
//         else:
//             Config = new AppConfig()  // 默认值
//
//     method Save():
//         json = JsonSerializer.Serialize(Config, indent: true)
//         File.WriteAllText(_configPath, json)
```

- [ ] **Step 2: 验证构建**

```bash
dotnet build BBDown.GUI/BBDown.GUI.csproj
```

- [ ] **Step 3: 提交**

```bash
git add BBDown.GUI/Services/ConfigService.cs
git commit -m "feat: add ConfigService for JSON config persistence"
```

---

### Task 5: 实现 DownloadService

**Files:**
- Create: `BBDown.GUI/Services/DownloadService.cs`

- [ ] **Step 1: 实现 DownloadService**

```csharp
// BBDown.GUI/Services/DownloadService.cs

// 伪代码：
// class DownloadService:
//     static property Instance: DownloadService (单例)
//     property Tasks: ObservableCollection<DownloadTask>
//     private SemaphoreSlim _semaphore(1)  // 当前单任务，后续可扩展并发
//     private CancellationTokenSource _cts
//
//     method Enqueue(string url, Dictionary<string, object> options) -> DownloadTask:
//         task = new DownloadTask { Id=Guid, Url=url, Status=Queued }
//         task.Options = options
//         Tasks.Add(task)
//         _ = ProcessQueue()  // fire and forget
//         return task
//
//     method ProcessQueue():
//         await _semaphore.WaitAsync()
//         try:
//             foreach task in Tasks where task.Status == Queued:
//                 await ExecuteTask(task)
//         finally:
//             _semaphore.Release()
//
//     method ExecuteTask(DownloadTask task):
//         _cts = new CancellationTokenSource()
//         progress = new DownloadProgressAdapter(task)
//         // 构造 MyOption 从 task.Options
//         option = BuildOption(task)
//         try:
//             task.Status = Parsing
//             // 调用 BBDown.Program 的解析+下载逻辑
//             // 传入 progress 和 _cts.Token
//             await DownloadWithProgress(option, progress, _cts.Token)
//             task.Status = Completed
//         catch OperationCanceledException:
//             task.Status = Stopped
//         catch Exception ex:
//             task.Status = Failed
//             progress.ReportLog(task.Id, "error", ex.Message)
//
//     method StopTask(string taskId):
//         _cts?.Cancel()
//
//     // DownloadProgressAdapter: IDownloadProgress
//     // 实现三个方法，更新对应 DownloadTask 的属性
//     // ReportProgress -> task.Progress = percent, task.StageText = stage
//     // ReportLog -> task.LogLines.Add(message)
```

- [ ] **Step 2: 验证构建**

```bash
dotnet build BBDown.GUI/BBDown.GUI.csproj
```

- [ ] **Step 3: 提交**

```bash
git add BBDown.GUI/Services/DownloadService.cs
git commit -m "feat: add DownloadService with task queue and progress adapter"
```
