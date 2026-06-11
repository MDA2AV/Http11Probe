# Http11Probe target for swerver (https://github.com/justinGrosvenor/swerver),
# a high-performance HTTP/1.1+2+3 server and API gateway in Zig.
#
# Base is Debian trixie (not bookworm like the other targets): swerver's
# HTTP/3 path links OpenSSL's QUIC TLS API (SSL_set_quic_tls_transport_params),
# which is only present in OpenSSL 3.5+ — trixie ships it, bookworm (3.0) does not.
FROM debian:trixie AS build

RUN apt-get update && apt-get install -y --no-install-recommends \
        ca-certificates curl xz-utils git libssl-dev zlib1g-dev dpkg-dev pkg-config \
    && rm -rf /var/lib/apt/lists/*

# Zig 0.16.0 stable
RUN set -eux; \
    ARCH=$(dpkg --print-architecture); \
    case "$ARCH" in amd64) ZA=x86_64 ;; arm64) ZA=aarch64 ;; *) echo "unsupported $ARCH" >&2; exit 1 ;; esac; \
    curl -fsSL --proto '=https' --tlsv1.2 --retry 5 --retry-all-errors --retry-delay 3 --connect-timeout 30 "https://ziglang.org/download/0.16.0/zig-${ZA}-linux-0.16.0.tar.xz" -o /tmp/zig.tar.xz; \
    mkdir -p /opt/zig; tar -xJf /tmp/zig.tar.xz -C /opt/zig --strip-components=1; \
    ln -s /opt/zig/zig /usr/local/bin/zig; zig version

# Clone swerver, then place the probe app at <swerver>/probeapp so its
# build.zig.zon `.path = ".."` dependency resolves to the swerver tree.
WORKDIR /src
RUN git clone --depth 1 --branch main https://github.com/justinGrosvenor/swerver.git .
RUN MULTIARCH=$(dpkg-architecture -qDEB_HOST_MULTIARCH); \
    for lib in libssl.so libssl.a libcrypto.so libcrypto.a; do \
        ln -sf "/usr/lib/${MULTIARCH}/${lib}" "/usr/lib/${lib}"; \
    done
COPY src/Servers/SwerverServer/main.zig src/Servers/SwerverServer/build.zig src/Servers/SwerverServer/build.zig.zon /src/probeapp/
WORKDIR /src/probeapp
RUN zig build --summary all

# ── runtime ──
FROM debian:trixie-slim
RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates libssl3 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /src/probeapp/zig-out/bin/swerver-probe /usr/local/bin/swerver-probe
COPY src/Servers/SwerverServer/config.json /app/config.json
COPY src/Servers/SwerverServer/docroot/ /app/docroot/
USER nobody
EXPOSE 8080
CMD ["/usr/local/bin/swerver-probe", "--config", "/app/config.json"]
