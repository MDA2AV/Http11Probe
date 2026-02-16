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
