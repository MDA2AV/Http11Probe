---
title: "HEAD No Body — HTTP/1.1 Compliance"
description: "A standard HEAD request. The server must respond with headers only — no message body. Tested against RFC 9110 §9.3.2."
weight: 15
---

| | |
|---|---|
| **Test ID** | `COMP-HEAD-NO-BODY` |
| **Category** | Compliance |
| **RFC** | [RFC 9110 §9.3.2](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.2) |
| **Requirement** | MUST |
| **Expected** | `2xx` with no body |

## What it sends

A standard HEAD request. The server must respond with headers only — no message body.

```http
HEAD / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

> "The HEAD method is identical to GET except that the server MUST NOT send content in the response." -- RFC 9110 Section 9.3.2

The server may include `Content-Length` or `Transfer-Encoding` headers to indicate what the body *would have been* for a GET request, but the actual response must contain zero body bytes.

## Why it matters

If a server sends body content in response to HEAD, it corrupts connection state on persistent connections. A client or proxy reading the connection will interpret those extra bytes as the start of the next response, leading to response desync. This is a particularly dangerous defect in proxy environments where multiple clients share connections.

## Sources

- [RFC 9110 §9.3.2 -- HEAD](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.2)
