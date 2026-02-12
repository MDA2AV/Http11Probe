use ntex::web;
use ntex::util::Bytes;

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
        web::App::new().default_service(web::to(handler))
    })
    .bind(("0.0.0.0", port))?
    .run()
    .await?;

    Ok(())
}
