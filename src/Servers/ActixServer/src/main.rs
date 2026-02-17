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
