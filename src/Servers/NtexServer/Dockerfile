FROM rust:1-slim AS build
WORKDIR /src

# Cache dependencies with dummy main
COPY src/Servers/NtexServer/Cargo.toml .
RUN mkdir src && echo "fn main() {}" > src/main.rs && cargo build --release && rm -rf src target/release/.fingerprint/ntex-server-*

COPY src/Servers/NtexServer/src/ src/
RUN cargo build --release

FROM debian:bookworm-slim
COPY --from=build /src/target/release/ntex-server /usr/local/bin/
ENTRYPOINT ["ntex-server", "8080"]
