# BBDown WPF GUI 实施计划（二）：ViewModels

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**目标：** 实现所有 ViewModel 层，包括导航主页、首页、下载队列、设置和登录。

**架构：** MVVM 模式，使用 CommunityToolkit.Mvvm（可选）或手动实现 INotifyPropertyChanged。ViewModel 通过 Services 访问数据，View 通过 Binding 响应变化。

---

### Task 6: 实现 MainViewModel

**Files:**
- Create: `BBDown.GUI/ViewModels/MainViewModel.cs`

- [ ] **Step 1: 实现 MainViewModel**

```csharp
// BBDown.GUI/ViewModels/MainViewModel.cs

// 伪代码：
// class MainViewModel : INotifyPropertyChanged
//     property CurrentPage: object  // 当前显示的 Page ViewModel
//     property NavigationItems: List<NavItem>
//     property Theme: string ("System" | "Light" | "Dark")
//
//     class NavItem:
//         property Name: string   // "首页"
//         property Icon: SymbolRegular
//         property PageType: Type // 对应 Page 类型
//
//     method MainViewModel():
//         NavigationItems = [
//             new("首页", SymbolRegular.Home24, typeof(HomePage)),
//             new("下载队列", SymbolRegular.ArrowDownload24, typeof(DownloadsPage)),
//             new("设置", SymbolRegular.Settings24, typeof(SettingsPage)),
//             new("登录", SymbolRegular.Person24, typeof(LoginPage)),
//             new("关于", SymbolRegular.Info24, typeof(AboutPage)),
//         ]
//         CurrentPage = NavigationItems[0]  // 默认首页
//
//     method Navigate(NavItem item):
//         CurrentPage = item.PageType 实例
//
//     method SetTheme(string theme):
//         Theme = theme
//         ApplicationThemeManager.Apply(theme)
//
//     method LoadConfig():
//         ConfigService.Instance.Load()
//         SetTheme(ConfigService.Config.Theme)
//
//     method SaveConfig():
//         ConfigService.Config.Theme = Theme
//         ConfigService.Save()
```

- [ ] **Step 2: 验证构建**

```bash
dotnet build BBDown.GUI/BBDown.GUI.csproj
```

- [ ] **Step 3: 提交**

```bash
git add BBDown.GUI/ViewModels/MainViewModel.cs
git commit -m "feat: add MainViewModel with navigation and theme support"
```

---

### Task 7: 实现 HomeViewModel

**Files:**
- Create: `BBDown.GUI/ViewModels/HomeViewModel.cs`

- [ ] **Step 1: 实现 HomeViewModel**

```csharp
// BBDown.GUI/ViewModels/HomeViewModel.cs

// 伪代码：
// class HomeViewModel : INotifyPropertyChanged
//     // 输入属性（双向绑定）
//     property Url: string
//     property SelectedQuality: string = "" // 空=最高画质
//     property SelectedPart: string = ""    // ""=当前, "ALL"=全部, "NEW"=最新
//
//     // 画质选项列表
//     property QualityOptions: List<string> = [
//         "优先最高画质", "1080P 高清", "720P 高清",
//         "480P 清晰", "360P 流畅", "更多..."
//     ]
//     property MoreQualityOptions: List<string> = [
//         "优先最高画质", "8K 超高清", "杜比视界", "HDR 真彩",
//         "4K 超清", "1080P 高帧率", "1080P 高码率", "720P 高帧率"
//     ]
//
//     // 分P选项
//     property PartOptions: Dictionary<string, string> = {
//         "当前分P": "", "全部分P": "ALL", "最新分P": "NEW"
//     }
//
//     // 命令
//     command Download():
//         if string.IsNullOrWhiteSpace(Url): 提示"请输入视频地址"; return
//
//         options = {}
//         if SelectedQuality != "": options["dfnPriority"] = SelectedQuality
//         if SelectedPart != "": options["selectPage"] = SelectedPart
//
//         // 从 ConfigService 读取设置页的选项合并
//         cfg = ConfigService.Instance.Config
//         options["useTvApi"] = cfg.DownloadSource == "tv"
//         options["useAppApi"] = cfg.DownloadSource == "app"
//         options["useIntlApi"] = cfg.DownloadSource == "intl"
//         options["encodingPriority"] = cfg.EncodingPriority
//         options["workDir"] = cfg.WorkDir
//         // ... 合并所有 cfg 中的高级选项
//
//         task = DownloadService.Instance.Enqueue(Url, options)
//         MainViewModel.Navigate("下载队列")
//
//     command NavigateToSettings():
//         MainViewModel.Navigate("设置")
```

- [ ] **Step 2: 验证构建**

```bash
dotnet build BBDown.GUI/BBDown.GUI.csproj
```

- [ ] **Step 3: 提交**

```bash
git add BBDown.GUI/ViewModels/HomeViewModel.cs
git commit -m "feat: add HomeViewModel with URL, quality and part selection"
```

---

### Task 8: 实现 DownloadsViewModel

**Files:**
- Create: `BBDown.GUI/ViewModels/DownloadsViewModel.cs`

- [ ] **Step 1: 实现 DownloadsViewModel**

```csharp
// BBDown.GUI/ViewModels/DownloadsViewModel.cs

// 伪代码：
// class DownloadsViewModel : INotifyPropertyChanged
//     property Tasks: ObservableCollection<DownloadTask>
//         // = DownloadService.Instance.Tasks 直接引用
//
//     property SelectedTask: DownloadTask
//
//     property OutputLog: string  // 选中任务的日志拼接
//
//     command StopTask(DownloadTask task):
//         DownloadService.Instance.StopTask(task.Id)
//
//     command ClearCompleted():
//         foreach task in Tasks where task.Status in [Completed, Failed, Stopped]:
//             Tasks.Remove(task)
//
//     // 监听 SelectedTask 变化时更新 OutputLog
//     // 监听 Tasks 集合变化以更新 UI
```

- [ ] **Step 2: 验证构建**

```bash
dotnet build BBDown.GUI/BBDown.GUI.csproj
```

- [ ] **Step 3: 提交**

```bash
git add BBDown.GUI/ViewModels/DownloadsViewModel.cs
git commit -m "feat: add DownloadsViewModel with task queue management"
```

---

### Task 9: 实现 SettingsViewModel

**Files:**
- Create: `BBDown.GUI/ViewModels/SettingsViewModel.cs`

- [ ] **Step 1: 实现 SettingsViewModel**

```csharp
// BBDown.GUI/ViewModels/SettingsViewModel.cs

// 伪代码：
// class SettingsViewModel : INotifyPropertyChanged
//     // 所有属性与 AppConfig 一一对应，双向绑定
//     property DownloadSource: int (0=网页, 1=TV, 2=APP, 3=国际)
//     property EncodingPriorityIndex: int (0=优先, 1=AVC, 2=AV1, 3=HEVC)
//     property FfmpegPath: string
//     property WorkDir: string
//     // ... 所有 AppConfig 中定义的属性
//
//     // 编码选项
//     property EncodingOptions: List<string> = ["优先可用", "AVC", "AV1", "HEVC"]
//
//     // 文件名变量参考文本
//     property FilePatternReference: string = "<videoTitle>: 主标题\n<pageNumber>: 分P序号\n..."
//
//     method SettingsViewModel():
//         LoadFromConfig()
//
//     method LoadFromConfig():
//         cfg = ConfigService.Instance.Config
//         DownloadSource = ConvertSource(cfg.DownloadSource)
//         // ... 逐个赋值
//
//     command Save():
//         cfg = ConfigService.Instance.Config
//         // ... 逐个写入
//         ConfigService.Save()
//         Snackbar.Show("设置已保存")
//
//     command BrowseFfmpeg():
//         dialog = OpenFileDialog(filter: "ffmpeg*")
//         if dialog result OK: FfmpegPath = dialog.FileName
//
//     command BrowseWorkDir():
//         dialog = OpenFolderDialog()
//         if dialog result OK: WorkDir = dialog.Folder
```

- [ ] **Step 2: 验证构建**

```bash
dotnet build BBDown.GUI/BBDown.GUI.csproj
```

- [ ] **Step 3: 提交**

```bash
git add BBDown.GUI/ViewModels/SettingsViewModel.cs
git commit -m "feat: add SettingsViewModel with all advanced options"
```

---

### Task 10: 实现 LoginViewModel

**Files:**
- Create: `BBDown.GUI/ViewModels/LoginViewModel.cs`

- [ ] **Step 1: 实现 LoginViewModel**

```csharp
// BBDown.GUI/ViewModels/LoginViewModel.cs

// 伪代码：
// class LoginViewModel : INotifyPropertyChanged
//     property QrCodeImage: BitmapSource  // Web 端二维码
//     property QrCodeTvImage: BitmapSource  // TV 端二维码
//     property AccessToken: string
//     property Cookie: string
//     property LoginStatus: string
//
//     // 初始化时从 ConfigService 加载已有的 token/cookie
//
//     method GenerateQrCode(string type = "web"):
//         // 调用 BBDown 的二维码生成逻辑（QRCoder）
//         // 将生成的二维码转为 BitmapSource 绑定到 UI
//         // type="web": 调用 BBDown.Program 中 Web 端登录流程
//         // type="tv": 调用 BBDown.Program 中 TV 端登录流程
//         QrCodeImage = generatedBitmap
//
//     method RefreshQrCode():
//         GenerateQrCode()
//
//     command SaveTokens():
//         ConfigService.Instance.Config.AccessToken = AccessToken
//         ConfigService.Instance.Config.Cookie = Cookie
//         ConfigService.Save()
//         LoginStatus = "已保存"
```

- [ ] **Step 2: 验证构建**

```bash
dotnet build BBDown.GUI/BBDown.GUI.csproj
```

- [ ] **Step 3: 提交**

```bash
git add BBDown.GUI/ViewModels/LoginViewModel.cs
git commit -m "feat: add LoginViewModel with QR code and token support"
```
