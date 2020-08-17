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
  -tv, --use-tv-api                    使用TV端API解析
  -hevc, --only-hevc                   选择HEVC编码
  -info, --only-show-info              仅解析流信息
  -hs, --hide-streams                  不显示可用音视频流
  -ia, --interactive                   交互选择流
  -mt, --multi-thread                  多线程下载
  -p, --select-page <select-page>      指定分p或分p范围
  -c, --cookie <cookie>                设置cookie以访问会员内容
  -a, --access-token <access-token>    设置access_token以访问TV端会员内容
  --version                            Show version information
  -?, -h, --help                       Show help and usage information

Commands:
  login      扫描二维码登录WEB账号
  logintv    扫描二维码登录TV账号
```

# 功能
- [x] 番剧下载(Web|TV)
- [x] 普通内容下载(Web|TV) `(TV接口可以下载部分UP主的无水印内容)`
- [x] 多分P自动下载
- [x] 选择指定分P进行下载
- [x] 选择指定清晰度进行下载
- [x] 下载外挂字幕并转换为srt格式
- [x] 自动合并音频+视频流+字幕流
- [x] 二维码登录账号
- [x] **多线程下载**

# TODO
- [ ] 支持更多自定义选项
- [ ] 其他的懒得写了

# 更新日志
已移至 [CHANGELOG.md](CHANGELOG.md)

# 演示
![1](https://user-images.githubusercontent.com/20772925/88686407-a2001480-d129-11ea-8aac-97a0c71af115.gif)

下载完毕后在当前目录查看MP4文件：

![2](https://user-images.githubusercontent.com/20772925/88478901-5e1cdc00-cf7e-11ea-97c1-154b9226564e.png)
