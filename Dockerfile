FROM alpine:latest AS builder
COPY artifact/ /opt/

RUN apk update \
    && apk add unzip file \
    && if [ $(arch) == "aarch64" ]; then \
         unzip /opt/BBDown_*_linux-arm64.zip -d /; \
       elif [ $(arch) == "x86_64" ]; then \
         unzip /opt/BBDown_*_linux-x64.zip -d /; \
       fi \
    && file /BBDown


FROM ubuntu:25.04

COPY --from=builder /BBDown /BBDown

RUN apt-get -y update \
    && apt-get -y upgrade \
    && apt-get install -y --no-install-recommends ffmpeg \
    && apt-get clean

EXPOSE 23333

ENTRYPOINT ["/BBDown", "serve", "-l", "http://0.0.0.0:23333"]