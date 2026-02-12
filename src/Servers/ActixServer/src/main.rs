use actix_web::{web, App, HttpServer, HttpRequest, HttpResponse};

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
        App::new().default_service(web::to(handler))
    })
    .bind(("0.0.0.0", port))?
    .run()
    .await
}
