[![img](https://img.shields.io/github/stars/nilaoda/BBDown?label=%E7%82%B9%E8%B5%9E)](https://github.com/nilaoda/BBDown)  [![img](https://img.shields.io/github/last-commit/nilaoda/BBDown?label=%E6%9C%80%E8%BF%91%E6%8F%90%E4%BA%A4)](https://github.com/nilaoda/BBDown)  [![img](https://img.shields.io/github/release/nilaoda/BBDown?label=%E6%9C%80%E6%96%B0%E7%89%88%E6%9C%AC)](https://github.com/nilaoda/BBDown/releases)  [![img](https://img.shields.io/github/license/nilaoda/BBDown?label=%E8%AE%B8%E5%8F%AF%E8%AF%81)](https://github.com/nilaoda/BBDown)

# BBDown
一款命令行式哔哩哔哩下载器. Bilibili Downloader.

# 注意
本软件合并时需要使用[ffmpeg](https://www.gyan.dev/ffmpeg/builds/) ，非`win-x64`平台请自行下载配置，并加入环境变量.

也可能需要使用[mp4box](https://gpac.wp.imt.fr/downloads/)，至少用于合并**杜比视界**.

# 快速开始
本软件已经以 [Dotnet Tool](https://www.nuget.org/packages/BBDown/) 形式发布  

如果你本地有dotnet环境，使用如下命令即可安装使用
```
dotnet tool install --global BBDown
```

# 下载
https://github.com/nilaoda/BBDown/releases

# 开始使用
目前命令行参数支持情况
```
BBDown:
  BBDown是一个免费且便捷高效的哔哩哔哩下载/解析软件.

Usage:
  BBDown [options] <url> [command]

Arguments:
  <url>    视频地址 或 av|bv|BV|ep|ss

Options:
  -tv, --use-tv-api                        使用TV端解析模式
  -app, --use-app-api                      使用APP端解析模式
  -intl, --use-intl-api                    使用国际版解析模式
  --use-mp4box                             使用MP4Box来混流
  -hevc, --only-hevc                       只下载hevc编码
  -avc, --only-avc                         只下载avc编码
  -info, --only-show-info                  仅解析而不进行下载
  -hs, --hide-streams                      不要显示所有可用音视频流
  -ia, --interactive                       交互式选择清晰度
  --show-all                               展示所有分P标题
  --use-aria2c                             调用aria2c进行下载(你需要自行准备好二进制可执行文件)
  --aria2c-proxy <aria2c-proxy>            调用aria2c进行下载时的代理地址配置
  -mt, --multi-thread                      使用多线程下载
  -p, --select-page <select-page>          选择指定分p或分p范围：(-p 8 或 -p 1,2 或 -p 3-5 或 -p ALL)
  --audio-only                             仅下载音频
  --video-only                             仅下载视频
  --sub-only                               仅下载字幕
  --no-padding-page-num                    不给分P序号补零
  --debug                                  输出调试日志
  --skip-mux                               跳过混流步骤
  --language <language>                    设置混流的音频语言(代码)，如chi, jpn等
  -c, --cookie <cookie>                    设置字符串cookie用以下载网页接口的会员内容
  -token, --access-token <access-token>    设置access_token用以下载TV/APP接口的会员内容
  --version                                Show version information
  -?, -h, --help                           Show help and usage information

Commands:
  login      通过APP扫描二维码以登录您的WEB账号
  logintv    通过APP扫描二维码以登录您的TV账号
```

# 功能
- [x] 番剧下载(Web|TV|App)
- [x] 课程下载(Web)
- [x] 普通内容下载(Web|TV|App) `(TV接口可以下载部分UP主的无水印内容)`
- [x] 多分P自动下载
- [x] 选择指定分P进行下载
- [x] 选择指定清晰度进行下载
- [x] 下载外挂字幕并转换为srt格式
- [x] 自动合并音频+视频流+字幕流`(使用ffmpeg或mp4box)`
- [x] 单独下载视频或音频轨道
- [x] 二维码登录账号
- [x] **多线程下载**
- [x] 支持调用aria2c下载
- [x] **支持HDR/杜比视界/杜比全景声下载**`(需要使用App接口且输入会员token)`

# TODO
- [ ] 支持更多自定义选项
- [ ] 自动刷新cookie
- [ ] 下载指定收藏夹中的视频
- [ ] 下载某个个人空间页的视频
- [ ] 自定义存储文件名等
- [ ] 弹幕下载&转换

# 使用教程

<details>
<summary>WEB/TV鉴权</summary>  

---
  
扫码登录网页账号：
```
BBDown login
```
然后按照提示操作

扫码登录云视听小电视账号：
```
BBDown logintv
```
然后按照提示操作
 
*PS: 如果登录报错`The type initializer for 'Gdip' threw an exception`，请参考 [#37](https://github.com/nilaoda/BBDown/issues/37) 解决*

手动加载网页cookie：
```
BBDown -c "SESSDATA=******" "https://www.bilibili.com/video/BV1qt4y1X7TW"
```
手动加载云视听小电视token：
```
BBDown -tv -token "******" "https://www.bilibili.com/video/BV1qt4y1X7TW"
```

</details>

<details>
<summary>APP鉴权</summary>  

---
  
目前程序无法自动获取鉴权信息，推荐通过**抓包**来获取.

在请求Header中寻找键为`authorization`的项，其值形为`identify_v1 5227************1`，其中的`5227************1`就是token(access_key)

获取后手动通过`-token`命令加载, 或写入`BBDownApp.data`使程序自动读取
  
```
BBDown -app -token "******" "https://www.bilibili.com/video/BV1qt4y1X7TW"
```

</details>

---

下载普通视频：
```
BBDown "https://www.bilibili.com/video/BV1qt4y1X7TW"
```
使用TV接口下载(粉丝量大的UP主基本上是无水印片源)：
```
BBDown -tv "https://www.bilibili.com/video/BV1qt4y1X7TW"
```
当分P过多时，默认会隐藏展示全部的分P信息，你可以使用如下命令来显示所有每一个分P。
```
BBDown --show-all "https://www.bilibili.com/video/BV1At41167aj"
```
选择下载某些分P的三种情况：
* 单个分P：10
```
BBDown "https://www.bilibili.com/video/BV1At41167aj?p=10"
BBDown -p 10 "https://www.bilibili.com/video/BV1At41167aj"
```
* 多个分P：1,2,10
```
BBDown -p 1,2,10 "https://www.bilibili.com/video/BV1At41167aj"
```
* 范围分P：1-10
```
BBDown -p 1-10 "https://www.bilibili.com/video/BV1At41167aj"
```
下载番剧全集：
```
BBDown -p ALL "https://www.bilibili.com/bangumi/play/ss33073"
```

# 更新日志

请查看 [changelog.txt](https://github.com/nilaoda/BBDown/blob/master/changelog.txt)

# 演示
![1](https://user-images.githubusercontent.com/20772925/88686407-a2001480-d129-11ea-8aac-97a0c71af115.gif)

下载完毕后在当前目录查看MP4文件：

![2](https://user-images.githubusercontent.com/20772925/88478901-5e1cdc00-cf7e-11ea-97c1-154b9226564e.png)

# 致谢

* https://github.com/JamesNK/Newtonsoft.Json
* https://github.com/codebude/QRCoder
* https://github.com/icsharpcode/SharpZipLib
* https://github.com/protobuf-net/protobuf-net
* https://github.com/dotnet/command-line-api
* https://github.com/SocialSisterYi/bilibili-API-collect
* https://github.com/SeeFlowerX/bilibili-grpc-api
* https://github.com/FFmpeg/FFmpeg
* https://github.com/gpac/gpac
* https://github.com/aria2/aria2
