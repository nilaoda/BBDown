# BBDown WPF GUI Design Spec

## Overview

基于 wpfui (lepoco/wpfui) 框架为 BBDown 实现 WPF 图形界面，替代现有的 Python/PyQt5 GUI。新 GUI 直接引用 BBDown.Core 进程内调用，不再通过子进程方式。

## Goals

- 提供简洁的首页用于快速下载
- 提供完整的设置页管理所有高级参数
- 内置下载队列管理，支持多任务进度追踪
- 支持二维码登录 + cookie/token 手动输入
- 配置持久化，记忆用户偏好
- Fluent Design 风格，深色/浅色主题

## Project Structure

```
BBDown.sln
├── BBDown/              # 现有 CLI 项目（不改动）
├── BBDown.Core/         # 现有核心库（小幅解耦，提取进度接口）
└── BBDown.GUI/          # 新增 WPF 项目
    ├── App.xaml
    ├── App.xaml.cs
    ├── BBDown.GUI.csproj
    ├── Views/
    │   ├── MainWindow.xaml          # FluentWindow + NavigationView
    │   ├── Pages/
    │   │   ├── HomePage.xaml         # 首页 - 简易下载
    │   │   ├── DownloadsPage.xaml    # 下载队列
    │   │   ├── SettingsPage.xaml     # 高级设置
    │   │   ├── LoginPage.xaml        # 登录（二维码+cookie/token）
    │   │   └── AboutPage.xaml        # 关于
    │   └── Controls/
    │       └── DownloadTaskCard.xaml # 下载任务卡片
    ├── ViewModels/
    │   ├── MainViewModel.cs
    │   ├── HomeViewModel.cs
    │   ├── DownloadsViewModel.cs
    │   ├── SettingsViewModel.cs
    │   └── LoginViewModel.cs
    ├── Models/
    │   ├── DownloadTask.cs
    │   └── AppConfig.cs
    └── Services/
        ├── DownloadService.cs       # 封装 BBDown.Core 调用
        └── ConfigService.cs         # JSON 配置读写
```

## Navigation

wpfui NavigationView 左侧栏，5个导航项：

| 导航项 | 图标 | 对应 Page |
|--------|------|-----------|
| 首页 | Fluent24 | HomePage |
| 下载队列 | Fluent24 | DownloadsPage |
| 设置 | Fluent24 | SettingsPage |
| 登录 | Fluent24 | LoginPage |
| 关于 | Fluent24 | AboutPage |

## Pages

### HomePage（首页）

只保留每次下载都可能变化的选项，其余移至设置页：

- **视频地址**：TextBox，支持 av/bv/BV/ep/ss 及完整 URL
- **画质选择**：RadioButton 组
  - 优先最高画质（默认）
  - 1080P / 720P / 480P / 360P
  - 更多：ComboBox（8K/杜比/HDR/4K/1080P高帧率/1080P高码率等）
- **分P选择**：RadioButton 组（当前分P/全部分P/最新分P）
- **下载按钮**：Button，主操作
- **设置页快捷入口**：点击跳转到设置页

### DownloadsPage（下载队列）

下载管理器：

- **任务列表**：ItemsControl，每个任务一个 Card
  - 视频标题/URL
  - 进度条（ProgressBar）
  - 状态文本（等待中/下载中/混流中/已完成/失败）
  - 停止按钮
- **日志区域**：只读 TextBox，显示当前选中任务的输出
- **全局控制**：全部开始/全部暂停
- 支持多个任务排队下载

### SettingsPage（设置）

所有参数按分组卡片组织，含从首页移入的选项：

- **常规设置**：下载源（网页/TV/APP/国际）、视频编码、FFmpeg路径、下载目录
- **下载选项**：仅音频/仅视频/仅字幕/下载弹幕/弹幕格式
- **交互选项**：交互选择清晰度/仅解析不下载/隐藏音视频流/调试日志
- **跳过选项**：跳过字幕/跳过封面/跳过混流/跳过AI字幕
- **MP4Box**：启用 MP4Box + 路径
- **Cookies**：access_token + cookie 字符串
- **文件名选项**：单分P模板/多分P模板，带内置变量参考
- **分P选项**：展示所有标题/指定分P/分P间隔
- **aria2c**：启用 + 路径 + 代理 + 附加参数
- **其他**：多线程/强制HTTP/音频语言
- **代理**：代理地址/番剧代理/地区
- **BBDown程序位置**：TextBox + 浏览

所有设置项自动保存到 config.json。

### LoginPage（登录）

- **二维码登录（网页端）**：显示二维码图片 + 刷新按钮
- **二维码登录（TV端）**：显示二维码图片 + 刷新按钮
- **手动输入**：access_token 输入 + cookie 输入（与设置页同步）
- 登录状态提示

### AboutPage（关于）

- 版本信息
- 开源地址链接
- 致谢

## Data Flow

```
用户操作 → ViewModel (ICommand)
  → DownloadService.Enqueue(url, options)
    → new DownloadTask { Status = Queued }
      → DownloadsViewModel.Tasks.Add(task)
        → 排队执行
          → Progress 事件 → 更新进度条
          → Log 事件 → 追加输出
          → Complete/Error → 更新状态
```

## BBDown.Core Refactoring

提取进度回调接口，不改动现有功能：

```csharp
// BBDown.Core 新增
public interface IDownloadProgress
{
    void OnProgress(string stage, double percent, string message);
    void OnLog(string level, string message);
}

// DownloadService 在调用时传入实现
```

下载方法增加 CancellationToken 支持以支持停止下载。

## Dependencies

- **WPF-UI** (`WPF-UI` NuGet) — Fluent Design 控件库
- **BBDown.Core** — 项目引用，核心下载逻辑
- **QRCoder** — 已有依赖，用于二维码生成（BBDown 已引用）

Target: **net9.0-windows**（WPF 项目要求）

## Config Persistence

沿用 Python 版的 config.json 格式，位于工作目录下。包含所有设置项的值，启动时自动加载。

## Theme

- 默认跟随系统主题（wpfui ApplicationThemeManager）
- 支持手动切换深色/浅色
- Mica 背景材质
