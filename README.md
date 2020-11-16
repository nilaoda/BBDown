[![img](https://img.shields.io/github/stars/nilaoda/BBDown?label=%E7%82%B9%E8%B5%9E)](https://github.com/nilaoda/BBDown)  [![img](https://img.shields.io/github/last-commit/nilaoda/BBDown?label=%E6%9C%80%E8%BF%91%E6%8F%90%E4%BA%A4)](https://github.com/nilaoda/BBDown)  [![img](https://img.shields.io/github/release/nilaoda/BBDown?label=%E6%9C%80%E6%96%B0%E7%89%88%E6%9C%AC)](https://github.com/nilaoda/BBDown/releases)  [![img](https://img.shields.io/github/license/nilaoda/BBDown?label=%E8%AE%B8%E5%8F%AF%E8%AF%81)](https://github.com/nilaoda/BBDown)

# BBDown
一款命令行式哔哩哔哩下载器. Bilibili Downloader.

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
  -tv, --use-tv-api                    使用TV端解析模式
  -hevc, --only-hevc                   只下载hevc编码
  -info, --only-show-info              仅解析而不进行下载
  -hs, --hide-streams                  不要显示所有可用音视频流
  -ia, --interactive                   交互式选择清晰度
  --show-all                           展示所有分P标题
  --use-aria2c                         调用aria2c进行下载(你需要自行准备好二进制可执行文件)
  -mt, --multi-thread                  使用多线程下载
  -p, --select-page <select-page>      选择指定分p或分p范围：(-p 8 或 -p 1,2 或 -p 3-5 或 -p ALL)
  --audio-only                         仅下载音频
  --video-only                         仅下载视频
  --debug                              输出调试日志
  --skip-mux                           跳过混流步骤
  -c, --cookie <cookie>                设置字符串cookie用以下载网页接口的会员内容
  -a, --access-token <access-token>    设置access_token用以下载TV接口的会员内容
  --version                            Show version information
  -?, -h, --help                       Show help and usage information

Commands:
  login      通过APP扫描二维码以登录您的WEB账号
  logintv    通过APP扫描二维码以登录您的TV账号
```

# 功能
- [x] 番剧下载(Web|TV)
- [x] 课程下载(Web)
- [x] 普通内容下载(Web|TV) `(TV接口可以下载部分UP主的无水印内容)`
- [x] 多分P自动下载
- [x] 选择指定分P进行下载
- [x] 选择指定清晰度进行下载
- [x] 下载外挂字幕并转换为srt格式
- [x] 自动合并音频+视频流+字幕流
- [x] 二维码登录账号
- [x] **多线程下载**
- [x] 支持调用aria2c下载
- [x] 支持至高4K HDR清晰度下载

# TODO
- [ ] 支持更多自定义选项
- [ ] 自动刷新cookie

# 使用示例

扫码登录网页账号：
```
BBDown login
```
扫码登录云视听小电视账号：
```
BBDown logintv
```
 
*PS: 如果登录报错`The type initializer for 'Gdip' threw an exception`，请参考 [#37](https://github.com/nilaoda/BBDown/issues/37) 解决*

手动加载网页cookie：
```
BBDown -c "SESSDATA=******" "https://www.bilibili.com/video/BV1qt4y1X7TW"
```
手动加载云视听小电视token：
```
BBDown -a "access_token=******" "https://www.bilibili.com/video/BV1qt4y1X7TW"
```
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
```
```
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
