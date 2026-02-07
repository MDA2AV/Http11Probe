FROM rust:1-slim AS build
RUN apt-get update && apt-get install -y pkg-config libssl-dev && rm -rf /var/lib/apt/lists/*
WORKDIR /src

# Cache dependencies with dummy main
COPY src/Servers/PingoraServer/Cargo.toml .
RUN mkdir src && echo "fn main() {}" > src/main.rs && cargo build --release && rm -rf src target/release/.fingerprint/pingora-server-*

COPY src/Servers/PingoraServer/src/ src/
RUN cargo build --release

FROM debian:bookworm-slim
RUN apt-get update && apt-get install -y libssl3 && rm -rf /var/lib/apt/lists/*
COPY --from=build /src/target/release/pingora-server /usr/local/bin/
ENTRYPOINT ["pingora-server", "9011"]
