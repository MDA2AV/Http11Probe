FROM drogonframework/drogon AS build
WORKDIR /src
COPY src/Servers/DrogonServer/CMakeLists.txt .
COPY src/Servers/DrogonServer/main.cc .
RUN mkdir build && cd build && cmake .. -DCMAKE_BUILD_TYPE=Release && make -j$(nproc)

FROM ubuntu:24.04
RUN apt-get update && apt-get install -y --no-install-recommends \
    libjsoncpp25 libssl3t64 libuuid1 zlib1g libbrotli1 && \
    rm -rf /var/lib/apt/lists/*
COPY --from=build /src/build/drogon-server /usr/local/bin/
ENTRYPOINT ["drogon-server", "8080"]
