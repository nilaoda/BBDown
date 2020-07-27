# BBDown
一款命令行式哔哩哔哩下载器. Bilibili Downloader.

# 下载
https://github.com/nilaoda/BBDown/releases

# 开始使用
目前测试版的命令行参数支持情况
```
BBDown <-i url> [-tv] [-hevc] [-info] [-cookie <string>]

        -i url          输入视频地址
        -tv             使用TV端解析模式(可以免费下载4K等网页会员清晰度,但不支持番剧)
        -hevc           下载hevc编码
        -info           仅解析不下载
        -cookie         设置cookie以下载网页接口的会员内容
                        (例如-cookie "SESSDATA=abcdefg", cookie在F12-Application-Cookie中可以找到)
```

# 功能
- [x] 番剧下载(Web)
- [x] 普通内容下载(Web|TV) `(TV接口可以下载部分UP主的无水印内容)`
- [x] 多分P自动下载
- [x] 下载外挂字幕并转换为srt格式
- [x] 自动合并音频+视频流+字幕流

# TODO
- [ ] 支持更多自定义选项
- [ ] 其他的懒得写了

# 更新日志
* 2020年7月27日 20:29  
  更改解析接口，修复有时候获取不到分辨率编码等问题  
  修复分P下载异常问题  
  开始显示所有流信息
  
* 2020年7月27日 0:15  
  修复部分番剧无法下载问题
  
* 2020年7月26日 23:33  
  支持字幕自动封装
  
* 2020年7月26日 19:50  
  发布公测
  
# 演示
![1](https://user-images.githubusercontent.com/20772925/88478847-fe263580-cf7d-11ea-8ad3-b37ceb99fb92.gif)

下载完毕后在当前目录查看MP4文件：

![2](https://user-images.githubusercontent.com/20772925/88478901-5e1cdc00-cf7e-11ea-97c1-154b9226564e.png)
