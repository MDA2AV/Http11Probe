use std::fmt::Write;
use trillium::{Conn, Handler, Method};
use trillium_caching_headers::caching_headers;
use trillium_cookies::{CookiesConnExt, cookies};
use trillium_head::head;
use trillium_router::router;

async fn echo_body(mut conn: Conn) -> Conn {
    match conn.request_body().read_bytes().await {
        Ok(bytes) => conn.ok(bytes),
        Err(_) => conn.with_status(400),
    }
}

async fn echo_headers(conn: Conn) -> Conn {
    let body = conn.request_headers().to_string();
    conn.ok(body)
}

async fn echo_cookies(conn: Conn) -> Conn {
    let mut body = String::new();
    for cookie in conn.cookies().iter() {
        let _ = writeln!(body, "{}={}", cookie.name(), cookie.value());
    }
    conn.ok(body)
}

fn app() -> impl Handler {
    (
        head(),
        caching_headers(),
        cookies(),
        router()
            .get("/", "OK")
            .post("/", echo_body)
            .any(&[Method::Get, Method::Post], "/echo", echo_headers)
            .any(&[Method::Get, Method::Post], "/cookie", echo_cookies)
            .with_method_not_allowed(),
    )
}

fn main() {
    trillium_smol::run(app());
}
