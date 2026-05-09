# BBDown WPF GUI 实施计划（三）：Views

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**目标：** 实现所有 XAML View 页面和 MainWindow。

**架构：** wpfui 的 FluentWindow + NavigationView 作为主框架，各 Page 通过 NavigationView 切换。每个 Page 绑定对应 ViewModel。

---

### Task 11: 创建 MainWindow

**Files:**
- Create: `BBDown.GUI/Views/MainWindow.xaml`
- Create: `BBDown.GUI/Views/MainWindow.xaml.cs`

- [ ] **Step 1: 创建 MainWindow.xaml**

```xml
<!-- BBDown.GUI/Views/MainWindow.xaml -->

<!-- 伪代码结构： -->
<!-- <ui:FluentWindow> -->
<!--     <ui:FluentWindow.TitleBar> -->
<!--         <ui:TitleBar Title="BBDown" Icon="favicon.ico" /> -->
<!--     </ui:FluentWindow.TitleBar> -->
<!--     <Grid> -->
<!--         <ui:NavigationView> -->
<!--             <!-- 绑定 MainViewModel.NavigationItems -->
<!--             <!-- 选中项变更 -> Navigate -->
<!--         </ui:NavigationView> -->
<!--         <Frame> -->
<!--             <!-- 内容区，绑定 MainViewModel.CurrentPage -->
<!--         </Frame> -->
<!--     </Grid> -->
<!-- </ui:FluentWindow> -->
```

- [ ] **Step 2: 创建 MainWindow.xaml.cs（代码后置）**

```csharp
// BBDown.GUI/Views/MainWindow.xaml.cs

// 伪代码：
// partial class MainWindow : FluentWindow
//     method MainWindow():
//         InitializeComponent()
//         DataContext = new MainViewModel()
//         Loaded += (s, e) => (DataContext as MainViewModel).LoadConfig()
//         Closing += (s, e) => (DataContext as MainViewModel).SaveConfig()
```

- [ ] **Step 3: 验证构建**

```bash
dotnet build BBDown.GUI/BBDown.GUI.csproj
```

- [ ] **Step 4: 提交**

```bash
git add BBDown.GUI/Views/MainWindow.xaml BBDown.GUI/Views/MainWindow.xaml.cs
git commit -m "feat: add MainWindow with NavigationView and TitleBar"
```

---

### Task 12: 创建 HomePage

**Files:**
- Create: `BBDown.GUI/Views/Pages/HomePage.xaml`
- Create: `BBDown.GUI/Views/Pages/HomePage.xaml.cs`

- [ ] **Step 1: 创建 HomePage.xaml**

```xml
<!-- BBDown.GUI/Views/Pages/HomePage.xaml -->

<!-- 伪代码结构： -->
<!-- <Page DataContext="{Binding HomeViewModel}"> -->

<!-- 居中卡片布局，最大宽度 600：-->
<!-- <ui:Card> -->
<!--     <StackPanel spacing="16"> -->

<!--         [视频地址] -->
<!--         <ui:TextBox Header="视频地址" -->
<!--             Placeholder="输入 av/bv/BV/ep/ss 或完整链接" -->
<!--             Text="{Binding Url, UpdateSourceTrigger=PropertyChanged}" /> -->

<!--         [画质选择] -->
<!--         <TextBlock Text="画质" /> -->
<!--         <RadioButton Content="优先最高画质" IsChecked="{Binding IsQualityAuto}" /> -->
<!--         <RadioButton Content="1080P 高清" /> -->
<!--         <RadioButton Content="720P 高清" /> -->
<!--         <RadioButton Content="480P 清晰" /> -->
<!--         <RadioButton Content="360P 流畅" /> -->
<!--         <ComboBox Items="{Binding MoreQualityOptions}" -->
<!--             SelectedItem="{Binding SelectedMoreQuality}" -->
<!--             Visibility="{Binding IsMoreQualityVisible}" /> -->

<!--         [分P选择] -->
<!--         <TextBlock Text="分P" /> -->
<!--         <RadioButton Content="当前分P" IsChecked="{Binding IsCurrentPart}" /> -->
<!--         <RadioButton Content="全部分P" /> -->
<!--         <RadioButton Content="最新分P" /> -->

<!--         [下载按钮 + 设置入口] -->
<!--         <Grid columns="*, Auto"> -->
<!--             <ui:Button Content="下载" Command="{Binding DownloadCommand}" -->
<!--                 Icon="{ui:SymbolIcon ArrowDownload24}" /> -->
<!--             <ui:Button Content="设置" Command="{Binding NavigateToSettingsCommand}" -->
<!--                 Appearance="Secondary" /> -->
<!--         </Grid> -->

<!--     </StackPanel> -->
<!-- </ui:Card> -->
```

- [ ] **Step 2: 创建 HomePage.xaml.cs**

```csharp
// BBDown.GUI/Views/Pages/HomePage.xaml.cs

// 伪代码：
// partial class HomePage : Page
//     method HomePage():
//         InitializeComponent()
//         DataContext = new HomeViewModel()
```

- [ ] **Step 3: 验证构建**

```bash
dotnet build BBDown.GUI/BBDown.GUI.csproj
```

- [ ] **Step 4: 提交**

```bash
git add BBDown.GUI/Views/Pages/HomePage.xaml BBDown.GUI/Views/Pages/HomePage.xaml.cs
git commit -m "feat: add HomePage with URL, quality, part selection"
```

---

### Task 13: 创建 DownloadsPage

**Files:**
- Create: `BBDown.GUI/Views/Pages/DownloadsPage.xaml`
- Create: `BBDown.GUI/Views/Pages/DownloadsPage.xaml.cs`

- [ ] **Step 1: 创建 DownloadsPage.xaml**

```xml
<!-- BBDown.GUI/Views/Pages/DownloadsPage.xaml -->

<!-- 伪代码结构： -->
<!-- <Page DataContext="{Binding DownloadsViewModel}"> -->
<!-- <Grid rows="2*" columns="*"> -->

<!--     [左栏：任务列表] -->
<!--     <ItemsControl ItemsSource="{Binding Tasks}"> -->
<!--         <DataTemplate> -->
<!--             <ui:Card> -->
<!--                 <StackPanel> -->
<!--                     <TextBlock Text="{Binding Title}" /> -->
<!--                     <TextBlock Text="{Binding Url, Truncated}" /> -->
<!--                     <ProgressBar Value="{Binding Progress}" /> -->
<!--                     <TextBlock Text="{Binding StageText}" /> -->
<!--                     <ui:Button Content="停止" -->
<!--                         Command="{Binding DataContext.StopTaskCommand}" -->
<!--                         CommandParameter="{Binding}" -->
<!--                         Visibility="{Binding IsRunning}" /> -->
<!--                 </StackPanel> -->
<!--             </ui:Card> -->
<!--         </DataTemplate> -->
<!--     </ItemsControl> -->

<!--     [右栏：输出日志] -->
<!--     <TextBox Text="{Binding OutputLog}" IsReadOnly="True" -->
<!--         FontFamily="Consolas" VerticalScrollBarVisibility="Auto" /> -->

<!-- </Grid> -->
<!-- </Page> -->
```

- [ ] **Step 2: 创建 DownloadsPage.xaml.cs**

```csharp
// BBDown.GUI/Views/Pages/DownloadsPage.xaml.cs
// 同 HomePage 模式：InitializeComponent + DataContext
```

- [ ] **Step 3: 验证构建**

```bash
dotnet build BBDown.GUI/BBDown.GUI.csproj
```

- [ ] **Step 4: 提交**

```bash
git add BBDown.GUI/Views/Pages/DownloadsPage.xaml BBDown.GUI/Views/Pages/DownloadsPage.xaml.cs
git commit -m "feat: add DownloadsPage with task list and log viewer"
```

---

### Task 14: 创建 SettingsPage

**Files:**
- Create: `BBDown.GUI/Views/Pages/SettingsPage.xaml`
- Create: `BBDown.GUI/Views/Pages/SettingsPage.xaml.cs`

- [ ] **Step 1: 创建 SettingsPage.xaml**

```xml
<!-- BBDown.GUI/Views/Pages/SettingsPage.xaml -->

<!-- 伪代码结构： -->
<!-- <Page DataContext="{Binding SettingsViewModel}"> -->
<!-- <ScrollViewer VerticalScrollBarVisibility="Auto"> -->
<!-- <StackPanel spacing="16"> -->

<!--     [常规设置 卡片] -->
<!--     <ui:Card Header="常规设置"> -->
<!--         <StackPanel> -->
<!--             <ComboBox Header="下载源" Items="网页端/TV端/APP端/国际端" -->
<!--                 SelectedIndex="{Binding DownloadSource}" /> -->
<!--             <ComboBox Header="视频编码" Items="优先可用/AVC/AV1/HEVC" -->
<!--                 SelectedIndex="{Binding EncodingPriorityIndex}" /> -->
<!--             <Grid columns="*, Auto"> -->
<!--                 <TextBox Header="FFmpeg路径" Text="{Binding FfmpegPath}" /> -->
<!--                 <ui:Button Content="浏览" Command="{Binding BrowseFfmpegCommand}" /> -->
<!--             </Grid> -->
<!--             <Grid columns="*, Auto"> -->
<!--                 <TextBox Header="下载目录" Text="{Binding WorkDir}" /> -->
<!--                 <ui:Button Content="浏览" Command="{Binding BrowseWorkDirCommand}" /> -->
<!--             </Grid> -->
<!--         </StackPanel> -->
<!--     </ui:Card> -->

<!--     [下载选项 卡片] -->
<!--     <ui:Card Header="下载选项"> -->
<!--         <StackPanel> -->
<!--             <ToggleSwitch IsChecked="{Binding AudioOnly}">仅音频</ToggleSwitch> -->
<!--             <ToggleSwitch IsChecked="{Binding VideoOnly}">仅视频</ToggleSwitch> -->
<!--             <ToggleSwitch IsChecked="{Binding SubOnly}">仅字幕</ToggleSwitch> -->
<!--             <ToggleSwitch IsChecked="{Binding DownloadDanmaku}">下载弹幕</ToggleSwitch> -->
<!--             <TextBox Header="弹幕格式" Text="{Binding DanmakuFormats}" -->
<!--                 Visibility="{Binding DownloadDanmaku}" /> -->
<!--         </StackPanel> -->
<!--     </ui:Card> -->

<!--     [交互选项 卡片] -->
<!--     <ui:Card Header="交互选项"> -->
<!--         <ToggleSwitch IsChecked="{Binding Interactive}">交互式选择清晰度</ToggleSwitch> -->
<!--         <ToggleSwitch IsChecked="{Binding OnlyShowInfo}">仅解析不下载</ToggleSwitch> -->
<!--         <ToggleSwitch IsChecked="{Binding HideStreams}">隐藏音视频流列表</ToggleSwitch> -->
<!--         <ToggleSwitch IsChecked="{Binding Debug}">输出调试日志</ToggleSwitch> -->
<!--     </ui:Card> -->

<!--     [跳过选项 卡片] -->
<!--     <ui:Card Header="跳过选项"> -->
<!--         <ToggleSwitch IsChecked="{Binding SkipSubtitle}">跳过字幕下载</ToggleSwitch> -->
<!--         <ToggleSwitch IsChecked="{Binding SkipCover}">跳过封面下载</ToggleSwitch> -->
<!--         <ToggleSwitch IsChecked="{Binding SkipMux}">跳过混流</ToggleSwitch> -->
<!--         <ToggleSwitch IsChecked="{Binding SkipAi}">跳过AI字幕</ToggleSwitch> -->
<!--     </ui:Card> -->

<!--     [MP4Box 卡片] -->
<!--     <ui:Card Header="MP4Box"> -->
<!--         <ToggleSwitch IsChecked="{Binding UseMp4box}">使用MP4Box混流</ToggleSwitch> -->
<!--         <TextBox Header="MP4Box路径" Text="{Binding Mp4boxPath}"
<!--             Visibility="{Binding UseMp4box}" /> -->
<!--     </ui:Card> -->

<!--     [Cookies 卡片] -->
<!--     <ui:Card Header="登录凭证"> -->
<!--         <TextBox Header="access_token" Text="{Binding AccessToken}"
<!--             Placeholder="TV/APP端会员内容" /> -->
<!--         <TextBox Header="cookie" Text="{Binding Cookie}"
<!--             Placeholder="网页端会员内容" /> -->
<!--     </ui:Card> -->

<!--     [文件名选项 卡片] -->
<!--     <ui:Card Header="文件名"> -->
<!--         <TextBox Header="单分P模板" Text="{Binding FilePattern}"
<!--             Placeholder="<videoTitle>" /> -->
<!--         <TextBox Header="多分P模板" Text="{Binding MultiFilePattern}"
<!--             Placeholder="<videoTitle>/[P<pageNumberWithZero>]<pageTitle>" /> -->
<!--         <TextBox IsReadOnly="True" Text="{Binding FilePatternReference}"
<!--             Height="200" /> -->
<!--     </ui:Card> -->

<!--     [分P选项 卡片] -->
<!--     <ui:Card Header="分P"> -->
<!--         <ToggleSwitch IsChecked="{Binding ShowAll}">展示所有分P标题</ToggleSwitch> -->
<!--         <TextBox Header="指定分P" Text="{Binding SelectPage}"
<!--             Placeholder="1,2或3-5或ALL" /> -->
<!--         <TextBox Header="分P间隔(秒)" Text="{Binding DelayPerPage}" /> -->
<!--     </ui:Card> -->

<!--     [aria2c 卡片] -->
<!--     <ui:Card Header="aria2c"> -->
<!--         <ToggleSwitch IsChecked="{Binding UseAria2c}">启用aria2c</ToggleSwitch> -->
<!--         <TextBox Header="路径" Text="{Binding Aria2cPath}"
<!--             Visibility="{Binding UseAria2c}" /> -->
<!--         <TextBox Header="代理" Text="{Binding Aria2cProxy}"
<!--             Visibility="{Binding UseAria2c}" /> -->
<!--         <TextBox Header="附加参数" Text="{Binding Aria2cArgs}"
<!--             Visibility="{Binding UseAria2c}" /> -->
<!--     </ui:Card> -->

<!--     [其他 卡片] -->
<!--     <ui:Card Header="其他"> -->
<!--         <ToggleSwitch IsChecked="{Binding MultiThread}">多线程下载</ToggleSwitch> -->
<!--         <ToggleSwitch IsChecked="{Binding ForceHttp}">HTTP替换HTTPS</ToggleSwitch> -->
<!--         <TextBox Header="音频语言" Text="{Binding Language}" Placeholder="chi,jpn" /> -->
<!--         <TextBox Header="User-Agent" Text="{Binding UserAgent}" /> -->
<!--     </ui:Card> -->

<!--     [代理 卡片] -->
<!--     <ui:Card Header="代理"> -->
<!--         <ToggleSwitch IsChecked="{Binding EnableProxy}">启用代理</ToggleSwitch> -->
<!--         <TextBox Header="代理地址" Text="{Binding Host}" /> -->
<!--         <TextBox Header="番剧代理" Text="{Binding EpHost}" /> -->
<!--         <TextBox Header="地区" Text="{Binding Area}" Placeholder="hk/tw/th" /> -->
<!--     </ui:Card> -->

<!--     [保存按钮] -->
<!--     <ui:Button Content="保存设置" Command="{Binding SaveCommand}"
<!--         Icon="{ui:SymbolIcon Save24}" HorizontalAlignment="Right" /> -->

<!-- </StackPanel> -->
<!-- </ScrollViewer> -->
<!-- </Page> -->
```

- [ ] **Step 2: 创建 SettingsPage.xaml.cs**

```csharp
// BBDown.GUI/Views/Pages/SettingsPage.xaml.cs
// 同 HomePage 模式
```

- [ ] **Step 3: 验证构建**

```bash
dotnet build BBDown.GUI/BBDown.GUI.csproj
```

- [ ] **Step 4: 提交**

```bash
git add BBDown.GUI/Views/Pages/SettingsPage.xaml BBDown.GUI/Views/Pages/SettingsPage.xaml.cs
git commit -m "feat: add SettingsPage with all advanced option groups"
```

---

### Task 15: 创建 LoginPage

**Files:**
- Create: `BBDown.GUI/Views/Pages/LoginPage.xaml`
- Create: `BBDown.GUI/Views/Pages/LoginPage.xaml.cs`

- [ ] **Step 1: 创建 LoginPage.xaml**

```xml
<!-- BBDown.GUI/Views/Pages/LoginPage.xaml -->

<!-- 伪代码结构： -->
<!-- <Page DataContext="{Binding LoginViewModel}"> -->
<!-- <Grid rows="Auto,Auto,Auto"> -->

<!--     [二维码登录] -->
<!--     <ui:Card Header="扫码登录"> -->
<!--         <Grid columns="Auto,Auto"> -->
<!--             <StackPanel> -->
<!--                 <TextBlock Text="网页端" /> -->
<!--                 <Image Source="{Binding QrCodeImage}" Width="200" Height="200" /> -->
<!--                 <ui:Button Content="刷新" Command="{Binding RefreshQrCommand}" -->
<!--                     CommandParameter="web" /> -->
<!--             </StackPanel> -->
<!--             <StackPanel> -->
<!--                 <TextBlock Text="TV端" /> -->
<!--                 <Image Source="{Binding QrCodeTvImage}" Width="200" Height="200" /> -->
<!--                 <ui:Button Content="刷新" Command="{Binding RefreshQrCommand}" -->
<!--                     CommandParameter="tv" /> -->
<!--             </StackPanel> -->
<!--         </Grid> -->
<!--     </ui:Card> -->

<!--     [手动输入] -->
<!--     <ui:Card Header="手动输入"> -->
<!--         <StackPanel> -->
<!--             <TextBox Header="access_token" Text="{Binding AccessToken}"
<!--                 Placeholder="用于TV/APP端会员内容" /> -->
<!--             <TextBox Header="cookie" Text="{Binding Cookie}"
<!--                 Placeholder="用于网页端会员内容" /> -->
<!--             <ui:Button Content="保存" Command="{Binding SaveTokensCommand}" /> -->
<!--         </StackPanel> -->
<!--     </ui:Card> -->

<!--     [状态] -->
<!--     <ui:Card> -->
<!--         <TextBlock Text="{Binding LoginStatus}" /> -->
<!--     </ui:Card> -->

<!-- </Grid> -->
<!-- </Page> -->
```

- [ ] **Step 2: 创建 LoginPage.xaml.cs**

```csharp
// 同上模式
```

- [ ] **Step 3: 验证构建**

```bash
dotnet build BBDown.GUI/BBDown.GUI.csproj
```

- [ ] **Step 4: 提交**

```bash
git add BBDown.GUI/Views/Pages/LoginPage.xaml BBDown.GUI/Views/Pages/LoginPage.xaml.cs
git commit -m "feat: add LoginPage with QR code and manual token input"
```

---

### Task 16: 创建 AboutPage

**Files:**
- Create: `BBDown.GUI/Views/Pages/AboutPage.xaml`
- Create: `BBDown.GUI/Views/Pages/AboutPage.xaml.cs`

- [ ] **Step 1: 创建 AboutPage.xaml**

```xml
<!-- BBDown.GUI/Views/Pages/AboutPage.xaml -->

<!-- 伪代码结构： -->
<!-- <Page> -->
<!-- <ui:Card centered> -->
<!--     <StackPanel centered spacing="16"> -->
<!--         <Image Source="favicon.ico" Width="64" Height="64" /> -->
<!--         <TextBlock Text="BBDown GUI" font-size="24" font-weight="bold" /> -->
<!--         <TextBlock Text="版本 x.x.x" /> -->
<!--         <TextBlock Text="基于 BBDown 的 WPF 图形界面" /> -->
<!--         <HyperlinkButton Content="github.com/nilaoda/BBDown" /> -->
<!--         <HyperlinkButton Content="github.com/lepoco/wpfui" /> -->
<!--         <TextBlock Text="License: MIT" /> -->
<!--     </StackPanel> -->
<!-- </ui:Card> -->
<!-- </Page> -->
```

- [ ] **Step 2: 验证构建**

```bash
dotnet build BBDown.GUI/BBDown.GUI.csproj
```

- [ ] **Step 3: 提交**

```bash
git add BBDown.GUI/Views/Pages/AboutPage.xaml BBDown.GUI/Views/Pages/AboutPage.xaml.cs
git commit -m "feat: add AboutPage"
```
