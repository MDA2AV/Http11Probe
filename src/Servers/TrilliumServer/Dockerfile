FROM rust:1-slim AS build
WORKDIR /src

# Cache dependencies with dummy main
COPY src/Servers/TrilliumServer/Cargo.toml .
COPY src/Servers/TrilliumServer/Cargo.lock .
RUN mkdir src &&\
    echo "fn main() {}" > src/main.rs &&\
    cargo build --release --locked &&\
    rm -rf src target/release/.fingerprint/trillium-server-*

COPY src/Servers/TrilliumServer/src/ src/
RUN cargo build --release --locked

FROM debian:bookworm-slim
COPY --from=build /src/target/release/trillium-server /usr/local/bin/
USER nobody
ENV HOST=0.0.0.0 PORT=8080
ENTRYPOINT ["trillium-server"]
