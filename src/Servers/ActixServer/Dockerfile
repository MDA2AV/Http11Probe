FROM rust:1-slim AS build
WORKDIR /src

# Cache dependencies with dummy main
COPY src/Servers/ActixServer/Cargo.toml .
RUN mkdir src && echo "fn main() {}" > src/main.rs && cargo build --release && rm -rf src target/release/.fingerprint/actix-server-*

COPY src/Servers/ActixServer/src/ src/
RUN cargo build --release

FROM debian:bookworm-slim
COPY --from=build /src/target/release/actix-server /usr/local/bin/
ENTRYPOINT ["actix-server", "8080"]
