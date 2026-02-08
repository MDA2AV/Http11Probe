use ntex::web;

async fn ok() -> &'static str {
    "OK"
}

#[ntex::main]
async fn main() -> std::io::Result<()> {
    let port: u16 = std::env::args()
        .nth(1)
        .and_then(|s| s.parse().ok())
        .unwrap_or(8080);

    web::server(|| {
        web::App::new().default_service(web::to(ok))
    })
    .bind(("127.0.0.1", port))?
    .run()
    .await?;

    Ok(())
}
