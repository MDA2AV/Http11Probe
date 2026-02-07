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
        let mut header = ResponseHeader::build(200, None)?;
        header.insert_header("Content-Type", "text/plain")?;
        header.insert_header("Content-Length", "2")?;
        session
            .write_response_header(Box::new(header), false)
            .await?;
        session
            .write_response_body(Some(Bytes::from_static(b"OK")), true)
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
    proxy.add_tcp(&format!("127.0.0.1:{port}"));
    server.add_service(proxy);

    server.run_forever();
}
