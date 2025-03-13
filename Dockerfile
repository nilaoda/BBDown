FROM mcr.microsoft.com/dotnet/sdk:9.0 AS builder

WORKDIR /app

COPY . .

RUN dotnet build -c Release

FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app

COPY --from=builder /app/BBDown/bin/Release/net9.0 .

EXPOSE 23333

# install ffmpeg
RUN apt-get update && \
    apt-get install -y ffmpeg && \
    chmod +x /app/BBDown

ENTRYPOINT ["/app/BBDown", "serve", "-l", "http://0.0.0.0:23333"]