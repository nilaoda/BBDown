# 使用 .NET SDK 作为基础镜像
FROM mcr.microsoft.com/dotnet/sdk:8.0

# 设置工作目录
WORKDIR /app

# 安装 BBDown
RUN dotnet tool install --global BBDown

# 安装 ffmpeg
RUN apt-get update && apt-get install -y ffmpeg && rm -rf /var/lib/apt/lists/*

# 将 dotnet tools 添加到 PATH
ENV PATH="${PATH}:/root/.dotnet/tools"

# 创建下载目录
RUN mkdir -p /downloads
WORKDIR /downloads

# 设置容器启动时的默认命令
ENTRYPOINT ["BBDown"]

# 说明书:
# docker build -t bbdown .
# docker run --rm -v $(pwd)/downloads:/downloads bbdown BV1LSSHYXEtv --use-app-api --work-dir /downloads
