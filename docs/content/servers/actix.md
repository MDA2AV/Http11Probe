---
title: "Actix"
toc: true
breadcrumbs: false
---

**Language:** Rust · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/ActixServer)

## Dockerfile

```dockerfile
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
```

## Source

```rust
use actix_web::{web, App, HttpServer, HttpRequest, HttpResponse, Responder};

async fn echo(req: HttpRequest) -> impl Responder {
    let mut body = String::new();
    for (name, value) in req.headers() {
        body.push_str(&format!("{}: {}\n", name, value.to_str().unwrap_or("")));
    }
    HttpResponse::Ok().content_type("text/plain").body(body)
}

async fn cookie(req: HttpRequest) -> impl Responder {
    let mut body = String::new();
    if let Some(raw) = req.headers().get("cookie").and_then(|v| v.to_str().ok()) {
        for pair in raw.split(';') {
            let trimmed = pair.trim_start();
            if let Some(eq) = trimmed.find('=') {
                body.push_str(&format!("{}={}\n", &trimmed[..eq], &trimmed[eq+1..]));
            }
        }
    }
    HttpResponse::Ok().content_type("text/plain").body(body)
}

async fn handler(req: HttpRequest, body: web::Bytes) -> HttpResponse {
    if req.method() == actix_web::http::Method::POST {
        HttpResponse::Ok()
            .content_type("text/plain")
            .body(body)
    } else {
        HttpResponse::Ok()
            .content_type("text/plain")
            .body("OK")
    }
}

#[actix_web::main]
async fn main() -> std::io::Result<()> {
    let port: u16 = std::env::args()
        .nth(1)
        .and_then(|s| s.parse().ok())
        .unwrap_or(8080);

    HttpServer::new(|| {
        App::new()
            .route("/echo", web::to(echo))
            .route("/cookie", web::to(cookie))
            .default_service(web::to(handler))
    })
    .bind(("0.0.0.0", port))?
    .run()
    .await
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
  ProbeRender.renderServerPage('Actix');
})();
</script>
