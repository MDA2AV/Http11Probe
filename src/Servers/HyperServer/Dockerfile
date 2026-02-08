FROM rust:1-slim AS build
WORKDIR /src

# Cache dependencies with dummy main
COPY src/Servers/HyperServer/Cargo.toml .
RUN mkdir src && echo "fn main() {}" > src/main.rs && cargo build --release && rm -rf src target/release/.fingerprint/hyper-server-*

COPY src/Servers/HyperServer/src/ src/
RUN cargo build --release

FROM debian:bookworm-slim
COPY --from=build /src/target/release/hyper-server /usr/local/bin/
ENTRYPOINT ["hyper-server", "8080"]
