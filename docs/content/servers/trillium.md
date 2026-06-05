---
title: "Trillium"
toc: true
breadcrumbs: false
---

**Language:** Rust · [View source on GitHub](https://github.com/MDA2AV/tree/main/src/Servers/TrilliumServer)

## Dockerfile

```dockerfile
FROM rust:1-slim AS build
WORKDIR /src

# Cache dependencies with dummy main
COPY src/Servers/TrilliumServer/Cargo.toml .
RUN mkdir src && echo "fn main() {}" > src/main.rs && cargo build --release && rm -rf src target/release/.fingerprint/trillium-server-*

COPY src/Servers/TrilliumServer/src/ src/
RUN cargo build --release

FROM debian:bookworm-slim
COPY --from=build /src/target/release/trillium-server /usr/local/bin/
ENV HOST=0.0.0.0 PORT=8080
ENTRYPOINT ["trillium-server"]
```

## Source

```rust
use std::fmt::Write;
use trillium::{Conn, Handler, Method};
use trillium_caching_headers::caching_headers;
use trillium_cookies::{CookiesConnExt, cookies};
use trillium_head::head;
use trillium_router::router;

async fn echo_body(mut conn: Conn) -> Conn {
    match conn.request_body().read_bytes().await {
        Ok(bytes) => conn.ok(bytes),
        Err(_) => conn.with_status(400),
    }
}

async fn echo_headers(conn: Conn) -> Conn {
    let body = conn.request_headers().to_string();
    conn.ok(body)
}

async fn echo_cookies(conn: Conn) -> Conn {
    let mut body = String::new();
    for cookie in conn.cookies().iter() {
        let _ = writeln!(body, "{}={}", cookie.name(), cookie.value());
    }
    conn.ok(body)
}

fn app() -> impl Handler {
    (
        head(),
        caching_headers(),
        cookies(),
        router()
            .get("/", "OK")
            .post("/", echo_body)
            .any(&[Method::Get, Method::Post], "/echo", echo_headers)
            .any(&[Method::Get, Method::Post], "/cookie", echo_cookies)
            .with_method_not_allowed(),
    )
}

fn main() {
    trillium_smol::run(app());
}
```

## Test Results

<div id="server-summary"><p><em>Loading results...</em></p></div>

### Compliance

<div id="results-compliance"></div>

### Smuggling

<div id="results-smuggling"></div>

### Malformed Input

<div id="results-malformedinput"></div>

### Caching

<div id="results-capabilities"></div>

### Cookies

<div id="results-cookies"></div>

<script src="/probe/data.js"></script>
<script src="/probe/render.js"></script>
<script>
(function() {
  if (!window.PROBE_DATA) {
    document.getElementById('server-summary').innerHTML = '<p><em>No probe data available yet. Run the Probe workflow on <code>main</code> to generate results.</em></p>';
    return;
  }
  ProbeRender.renderServerPage('Trillium');
})();
</script>
