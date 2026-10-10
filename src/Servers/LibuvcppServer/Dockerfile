# libuvcpp entry for Http11Probe.
#
# The build downloads the official v1.6.0 linux-x64 release package and compiles
# server.cpp against it -- the same artifact a user gets from the GitHub release.
# That package's shared object already carries libuv, OpenSSL, nghttp2, ngtcp2
# and zlib, so the runtime image needs no HTTP library and no toolchain.
#
# The download is pinned by sha256 rather than by tag: a release tag can be moved
# and its assets re-uploaded, so the hash is what fixes which bytes get built.
#
# Both stages are ubuntu:24.04 (glibc 2.39; the release package's floor is
# 2.34), so the compiler that links the binary and the libc that runs it are the
# same ones. Alpine is not an option: the package is glibc-linked.

FROM ubuntu:24.04 AS build

ARG LIBUVCPP_VERSION=1.6.0
ARG LIBUVCPP_SHA256=b64ae68d121b49543e7a37126bbf38adf95a00ac51bff463b11be2dc499a138b

RUN apt-get update \
 && apt-get install -y --no-install-recommends \
        ca-certificates curl unzip g++ \
 && rm -rf /var/lib/apt/lists/*

WORKDIR /build
RUN curl -fsSL --retry 3 --retry-connrefused -o libuvcpp.zip \
        "https://github.com/Antruly/libuvcpp/releases/download/v${LIBUVCPP_VERSION}/libuvcpp-${LIBUVCPP_VERSION}-linux-x64.zip" \
 && echo "${LIBUVCPP_SHA256}  libuvcpp.zip" | sha256sum -c - \
 && unzip -q libuvcpp.zip -d /opt \
 && rm -f libuvcpp.zip

COPY src/Servers/LibuvcppServer/server.cpp .
RUN g++ -std=c++11 -O2 -DNDEBUG -o /probe-server server.cpp \
        -I/opt/libuvcpp-1.6.0-linux-x64/include \
        -L/opt/libuvcpp-1.6.0-linux-x64/lib \
        -luvcpp -Wl,-rpath,/opt/libuvcpp-1.6.0-linux-x64/lib -pthread

FROM ubuntu:24.04

# g++ links the C++ runtime dynamically, so the runtime layer needs it.
RUN apt-get update \
 && apt-get install -y --no-install-recommends libstdc++6 \
 && rm -rf /var/lib/apt/lists/*

COPY --from=build /opt/libuvcpp-1.6.0-linux-x64/lib/libuvcpp.so \
                  /opt/libuvcpp-1.6.0-linux-x64/lib/libuvcpp.so
COPY --from=build /probe-server /usr/local/bin/probe-server

USER nobody
ENTRYPOINT ["probe-server", "8080"]
