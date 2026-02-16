use std::convert::Infallible;
use std::net::SocketAddr;

use http_body_util::Full;
use hyper::body::Bytes;
use hyper::server::conn::http1;
use hyper::service::service_fn;
use hyper::{Request, Response};
use hyper_util::rt::TokioIo;
use tokio::net::TcpListener;

async fn handle(req: Request<hyper::body::Incoming>) -> Result<Response<Full<Bytes>>, Infallible> {
    if req.uri().path() == "/echo" {
        let mut body = String::new();
        for (name, value) in req.headers() {
            body.push_str(&format!("{}: {}\n", name, value.to_str().unwrap_or("")));
        }
        return Ok(Response::builder()
            .status(200)
            .header("Content-Type", "text/plain")
            .body(Full::new(Bytes::from(body)))
            .unwrap());
    }
    if req.uri().path() == "/cookie" {
        let mut body = String::new();
        if let Some(raw) = req.headers().get("cookie").and_then(|v| v.to_str().ok()) {
            for pair in raw.split(';') {
                let trimmed = pair.trim_start();
                if let Some(eq) = trimmed.find('=') {
                    body.push_str(&format!("{}={}\n", &trimmed[..eq], &trimmed[eq+1..]));
                }
            }
        }
        return Ok(Response::builder()
            .status(200)
            .header("Content-Type", "text/plain")
            .body(Full::new(Bytes::from(body)))
            .unwrap());
    }
    if req.method() == hyper::Method::POST {
        let body = match http_body_util::BodyExt::collect(req.into_body()).await {
            Ok(collected) => collected.to_bytes(),
            Err(_) => Bytes::new(),
        };
        return Ok(Response::new(Full::new(body)));
    }
    Ok(Response::new(Full::new(Bytes::from("OK"))))
}

#[tokio::main]
async fn main() {
    let port: u16 = std::env::args()
        .nth(1)
        .and_then(|s| s.parse().ok())
        .unwrap_or(8080);

    let addr = SocketAddr::from(([0, 0, 0, 0], port));
    let listener = TcpListener::bind(addr).await.unwrap();

    loop {
        let (stream, _) = listener.accept().await.unwrap();
        let io = TokioIo::new(stream);

        tokio::task::spawn(async move {
            if let Err(err) = http1::Builder::new()
                .serve_connection(io, service_fn(handle))
                .await
            {
                eprintln!("Error serving connection: {err:?}");
            }
        });
    }
}
