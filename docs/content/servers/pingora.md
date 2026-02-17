---
title: "Pingora"
toc: true
breadcrumbs: false
---

**Language:** Rust · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/PingoraServer)

## Dockerfile

```dockerfile
FROM rust:1-slim AS build
RUN apt-get update && apt-get install -y pkg-config libssl-dev cmake g++ && rm -rf /var/lib/apt/lists/*
WORKDIR /src

# Cache dependencies with dummy main
COPY src/Servers/PingoraServer/Cargo.toml .
RUN mkdir src && echo "fn main() {}" > src/main.rs && cargo build --release && rm -rf src target/release/.fingerprint/pingora-server-*

COPY src/Servers/PingoraServer/src/ src/
RUN cargo build --release

FROM debian:bookworm-slim
RUN apt-get update && apt-get install -y libssl3 && rm -rf /var/lib/apt/lists/*
COPY --from=build /src/target/release/pingora-server /usr/local/bin/
ENTRYPOINT ["pingora-server", "8080"]
```

## Source

```rust
use async_trait::async_trait;
use bytes::Bytes;
use pingora::http::ResponseHeader;
use pingora::prelude::*;
use pingora::proxy::{http_proxy_service, ProxyHttp, Session};

struct OkProxy;

#[async_trait]
impl ProxyHttp for OkProxy {
    type CTX = ();

    fn new_ctx(&self) -> Self::CTX {}

    async fn request_filter(
        &self,
        session: &mut Session,
        _ctx: &mut Self::CTX,
    ) -> Result<bool> {
        let is_cookie = session.req_header().uri.path() == "/cookie";
        if is_cookie {
            let mut body_str = String::new();
            if let Some(raw) = session.req_header().headers.get("cookie").and_then(|v| v.to_str().ok()) {
                for pair in raw.split(';') {
                    let trimmed = pair.trim_start();
                    if let Some(eq) = trimmed.find('=') {
                        body_str.push_str(&format!("{}={}\n", &trimmed[..eq], &trimmed[eq+1..]));
                    }
                }
            }
            let body = Bytes::from(body_str);
            let mut header = ResponseHeader::build(200, None)?;
            header.insert_header("Content-Type", "text/plain")?;
            header.insert_header("Content-Length", &body.len().to_string())?;
            session
                .write_response_header(Box::new(header), false)
                .await?;
            session
                .write_response_body(Some(body), true)
                .await?;
            return Ok(true);
        }

        let is_echo = session.req_header().uri.path() == "/echo";
        if is_echo {
            let mut body_str = String::new();
            for (name, value) in session.req_header().headers.iter() {
                body_str.push_str(&format!("{}: {}\n", name, value.to_str().unwrap_or("")));
            }
            let body = Bytes::from(body_str);
            let mut header = ResponseHeader::build(200, None)?;
            header.insert_header("Content-Type", "text/plain")?;
            header.insert_header("Content-Length", &body.len().to_string())?;
            session
                .write_response_header(Box::new(header), false)
                .await?;
            session
                .write_response_body(Some(body), true)
                .await?;
            return Ok(true);
        }

        let is_post = session.req_header().method == pingora::http::Method::POST;
        let body = if is_post {
            let mut buf = Vec::new();
            while let Some(chunk) = session.read_request_body().await? {
                buf.extend_from_slice(&chunk);
            }
            Bytes::from(buf)
        } else {
            Bytes::from_static(b"OK")
        };
        let mut header = ResponseHeader::build(200, None)?;
        header.insert_header("Content-Type", "text/plain")?;
        header.insert_header("Content-Length", &body.len().to_string())?;
        session
            .write_response_header(Box::new(header), false)
            .await?;
        session
            .write_response_body(Some(body), true)
            .await?;
        Ok(true)
    }

    async fn upstream_peer(
        &self,
        _session: &mut Session,
        _ctx: &mut Self::CTX,
    ) -> Result<Box<HttpPeer>> {
        // Never reached — request_filter always handles the request
        unreachable!()
    }
}

fn main() {
    let port: u16 = std::env::args()
        .nth(1)
        .and_then(|s| s.parse().ok())
        .unwrap_or(9011);

    let mut server = Server::new(None).unwrap();
    server.bootstrap();

    let mut proxy = http_proxy_service(&server.configuration, OkProxy);
    proxy.add_tcp(&format!("0.0.0.0:{port}"));
    server.add_service(proxy);

    server.run_forever();
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
  ProbeRender.renderServerPage('Pingora');
})();
</script>
