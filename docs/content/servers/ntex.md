---
title: "Ntex"
toc: true
breadcrumbs: false
---

**Language:** Rust · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/NtexServer)

## Dockerfile

```dockerfile
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
```

## Source

```rust
use ntex::web;
use ntex::util::Bytes;

async fn echo(req: web::HttpRequest) -> impl web::Responder {
    let mut body = String::new();
    for (name, value) in req.headers() {
        body.push_str(&format!("{}: {}\n", name, value.to_str().unwrap_or("")));
    }
    web::HttpResponse::Ok().content_type("text/plain").body(body)
}

async fn cookie(req: web::HttpRequest) -> impl web::Responder {
    let mut body = String::new();
    if let Some(raw) = req.headers().get("cookie").and_then(|v| v.to_str().ok()) {
        for pair in raw.split(';') {
            let trimmed = pair.trim_start();
            if let Some(eq) = trimmed.find('=') {
                body.push_str(&format!("{}={}\n", &trimmed[..eq], &trimmed[eq+1..]));
            }
        }
    }
    web::HttpResponse::Ok().content_type("text/plain").body(body)
}

async fn handler(req: web::HttpRequest, body: Bytes) -> web::HttpResponse {
    if req.method() == ntex::http::Method::POST {
        web::HttpResponse::Ok()
            .content_type("text/plain")
            .body(body)
    } else {
        web::HttpResponse::Ok()
            .content_type("text/plain")
            .body("OK")
    }
}

#[ntex::main]
async fn main() -> std::io::Result<()> {
    let port: u16 = std::env::args()
        .nth(1)
        .and_then(|s| s.parse().ok())
        .unwrap_or(8080);

    web::server(|| {
        web::App::new()
            .route("/echo", web::to(echo))
            .route("/cookie", web::to(cookie))
            .default_service(web::to(handler))
    })
    .bind(("0.0.0.0", port))?
    .run()
    .await?;

    Ok(())
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

<script src="/Http11Probe/probe/data.js"></script>
<script src="/Http11Probe/probe/render.js"></script>
<script>
(function() {
  if (!window.PROBE_DATA) {
    document.getElementById('server-summary').innerHTML = '<p><em>No probe data available yet. Run the Probe workflow on <code>main</code> to generate results.</em></p>';
    return;
  }
  ProbeRender.renderServerPage('Ntex');
})();
</script>
