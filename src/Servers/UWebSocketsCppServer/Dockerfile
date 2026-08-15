FROM debian:trixie-slim AS build
RUN apt-get update && apt-get install -y --no-install-recommends \
        ca-certificates g++ git make zlib1g-dev \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /src
# Deliberately unpinned: upstream asked that results track master so their
# fixes show up here. Rebuilding picks up whatever master is at that moment.
RUN git clone --depth 1 https://github.com/uNetworking/uWebSockets.git . \
    && git submodule update --init --depth 1 uSockets
RUN make -C uSockets
COPY src/Servers/UWebSocketsCppServer/server.cpp .
RUN g++ -std=c++2b -O3 -Isrc -IuSockets/src server.cpp uSockets/*.o -lz -o /uws-server

FROM debian:trixie-slim
RUN apt-get update && apt-get install -y --no-install-recommends zlib1g \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /uws-server /usr/local/bin/uws-server
USER nobody
ENTRYPOINT ["uws-server", "8080"]
