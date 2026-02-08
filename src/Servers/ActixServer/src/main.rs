use actix_web::{web, App, HttpServer, HttpResponse};

async fn ok() -> HttpResponse {
    HttpResponse::Ok()
        .content_type("text/plain")
        .body("OK")
}

#[actix_web::main]
async fn main() -> std::io::Result<()> {
    let port: u16 = std::env::args()
        .nth(1)
        .and_then(|s| s.parse().ok())
        .unwrap_or(8080);

    HttpServer::new(|| {
        App::new().default_service(web::to(ok))
    })
    .bind(("127.0.0.1", port))?
    .run()
    .await
}
