# JSON API文档

## API

如果以服务器模式启动BBDown，BBDown会在本地启动一个http server，该服务器有以下API：

### 获取任务列表
**Endpoint:** `/get-tasks/`

**Method:** GET

**Description:** 获取所有任务的列表，包括正在运行的任务和已完成的任务。

**Response:** JSON格式的`DownloadTaskCollection`。

### 获取正在运行的任务列表
**Endpoint:** `/get-tasks/running`

**Method:** GET

**Description:** 获取所有正在运行的任务的列表。

**Response:** JSON格式的`List<DownloadTask>`, 正在运行的任务列表。

### 获取已完成的任务列表
**Endpoint:** `/get-tasks/finished`

**Method:** GET

**Description:** 获取所有已完成的任务的列表。

**Response:**  JSON格式的`List<DownloadTask>`, 已完成的任务列表。

### 获取特定任务
**Endpoint:** `/get-tasks/{id}`

**Method:** GET

**Description:** 获取特定任务的详细信息，根据视频的AID。

**Parameters:**
- `{id}` (路径参数): 视频的AID

**Response:** 如果找到匹配的任务，将返回JSON格式的`DownloadTask`。如果未找到匹配的任务，将返回404 Not Found。

### 添加任务
**Endpoint:** `/add-task`

**Method:** POST

**Description:** 向任务列表中添加新任务。

**Request Body:** JSON格式的任务信息，需要符合`MyOption`数据结构。并不要求带有MyOption中的每一个字段，只需要有`Url`字段就够了。

**Response:**
- 如果请求有效并成功添加任务，将返回200 OK。
- 如果请求无效，将返回400 Bad Request，并附带错误消息`"输入有误"`。

### 移除已完成的任务
**Endpoint:** `/remove-finished`

**Method:** GET

**Description:** 移除所有已完成的任务

**Response:**
- 返回200 OK。

### 移除已完成的任务
**Endpoint:** `/remove-finished/failed`

**Method:** GET

**Description:** 移除所有已完成但是失败(`IsSuccessful == false`)的任务

**Response:**
- 返回200 OK。

### 移除特定已完成的任务
**Endpoint:** `/remove-finished/{id}`

**Method:** GET

**Description:** 移除特定已完成的任务，根据视频的AID。

**Parameters:**
- `{id}` (路径参数): 视频的AID

**Response:**
- 无论是否能找到对应ID的任务，均返回200 OK。

## 数据结构

### `DownloadTask` 数据结构
`DownloadTask` 数据结构表示一个下载任务的信息。

**属性：**
- `Aid` `<string>`: 视频解析出的Aid，用作正在下载中的任务的唯一标识符，已完成任务中允许重复存在
- `Url` `<string>`: 下载任务请求时的URL，不一定需要完整的URL，命令行支持的`av|bv|BV|ep|ss`都可以在这里使用。
- `TaskCreateTime` `<long>`: 任务创建时间，Unix时间戳，精确到秒，本机时区。
- `Title` `<string?>`: 视频的标题。
- `Pic` `<string?>`: 视频的封面图片链接。
- `VideoPubTime` `<long?>`: 视频发布时间，Unix时间戳，精确到秒。
- `TaskFinishTime` `<long?>`: 任务完成时间，Unix时间戳，精确到秒，本机时区。
- `Progress` `<double>`: 任务的下载进度，为0-1区间范围的小数。
- `DownloadSpeed` `<double>`: 下载速度, 单位为Byte/s。下载中时为最后一次更新的实时速度，下载完成后为平均速度。
- `TotalDownloadedBytes` `<double>`: 总下载字节(Byte)数，完成后的数字比实际文件偏小。
- `IsSuccessful` `<bool>`: 标识任务是否成功完成。

### `DownloadTaskCollection` 数据结构
`DownloadTaskCollection` 数据结构包含两个列表，分别表示正在运行的任务和已完成的任务。

**属性：**
- `Running` `<List<DownloadTask>>`: 包含正在运行的任务的列表，每个元素都是 `DownloadTask` 数据结构。
- `Finished` `<List<DownloadTask>>`: 包含已完成的任务的列表，每个元素都是 `DownloadTask` 数据结构。

### `MyOption` 数据结构

参考[BBDown/MyOption.cs](./BBDown/MyOption.cs)。属性和命令行参数几乎是一一对应的，相应的值填写使用命令行会使用的值即可。这个结构会随着版本变化，请参考对应版本时候的文件。

### 注意事项
- 由于BBDown的下载进度回报频率所限，`TotalDownloadedBytes`会比实际下载的文件略低，大概会少等效于1秒下载速度的文件体积，如果文件本身就非常小那这个数字偏差会较大。
- BBDown目前内部机制没有太好的方法取消单个下载任务，因此目前任务提交以后只能等任务失败或者完成。
- 目前服务器没有对同时执行的下载任务数量做任何限制，如果短时间频繁添加任务就会同时执行相当数量的下载任务，需要小心注意不要耗尽资源。未来考虑添加下载队列。

### 使用例

#### 用BV号添加任务

```shell
curl -X POST -H 'Content-Type: application/json' -d '{ "Url": "BV1qt4y1X7TW" }' http://localhost:58682/add-task
```

#### 下载到指定目录

Windows:
```shell
curl -X POST -H 'Content-Type: application/json' -d { "Url": "BV1qt4y1X7TW", "FilePattern": "C:\\Downloads\\<videoTitle>[<dfn>]" }' http://localhost:58682/add-task
```

Unix-Like:
```shell
curl -X POST -H 'Content-Type: application/json' -d { "Url": "BV1qt4y1X7TW", "FilePattern": "/Downloads/<videoTitle>[<dfn>]" }' http://localhost:58682/add-task
```
