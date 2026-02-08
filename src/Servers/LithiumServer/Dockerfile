FROM ubuntu:24.04 AS build
RUN apt-get update && apt-get install -y --no-install-recommends \
    g++ wget libboost-context-dev libssl-dev ca-certificates && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /src
RUN wget -q https://raw.githubusercontent.com/matt-42/lithium/master/single_headers/lithium_http_server.hh
COPY src/Servers/LithiumServer/main.cc .
RUN g++ -O2 -o lithium-server main.cc -lpthread -lboost_context -lssl -lcrypto

FROM ubuntu:24.04
RUN apt-get update && apt-get install -y --no-install-recommends \
    libboost-context1.83.0 libssl3t64 && \
    rm -rf /var/lib/apt/lists/*
COPY --from=build /src/lithium-server /usr/local/bin/
ENTRYPOINT ["lithium-server", "8080"]
