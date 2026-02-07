# Usage:
# docker build -t ftr-server:latest .
# docker run --rm -p 7777:7777/udp -p 7777:7777/tcp ftr-server:latest

# PRE-REQUISITES: this CANNOT build the game server, it should be already built in ./Build/Server/
# After building this image the game binary and dlls will be encapsulated in it

# **Dependencies**
FROM debian:bookworm-slim AS deps

RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    libglib2.0-0 \
    libstdc++6 \
    libgcc-s1 \
    && rm -rf /var/lib/apt/lists/*

# **Runtime**
FROM gcr.io/distroless/base-debian12

WORKDIR /app

COPY --from=deps /lib/x86_64-linux-gnu /lib/x86_64-linux-gnu
COPY --from=deps /usr/lib/x86_64-linux-gnu /usr/lib/x86_64-linux-gnu
COPY Build/Server/ /app/

EXPOSE 7777/tcp
EXPOSE 7777/udp

ENTRYPOINT ["./server.x86_64", "-batchmode", "-nographics"]
