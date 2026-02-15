---
title: "CLTE-PIPELINE"
description: "CLTE-PIPELINE test documentation"
weight: 8
---

| | |
|---|---|
| **Test ID** | `SMUG-CLTE-PIPELINE` |
| **Category** | Smuggling |
| **Scored** | Yes |
| **RFC** | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **RFC Level** | MAY |
| **Expected** | `400` or close preferred; `2xx` acceptable |

## What it sends

A request with both `Content-Length` and `Transfer-Encoding: chunked` — the classic CL.TE conflict pattern.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 4\r\n
Transfer-Encoding: chunked\r\n
\r\n
0\r\n
\r\n
```

A CL-only parser reads 4 bytes (`0\r\n\r`) as the body. A TE parser sees the `0` chunk as end-of-body. The ambiguity is what makes this a smuggling vector in proxy chains.

## What the RFC says

> "A server MAY reject a request that contains both Content-Length and Transfer-Encoding or process such a request in accordance with the Transfer-Encoding alone. Regardless, the server MUST close the connection after responding to such a request to avoid the potential attacks." — RFC 9112 §6.1

> "If a message is received with both a Transfer-Encoding and a Content-Length header field, the Transfer-Encoding overrides the Content-Length. Such a message might indicate an attempt to perform request smuggling (Section 11.2) or response splitting (Section 11.1) and ought to be handled as an error." — RFC 9112 §6.3

## Why it matters

When both framing headers are present, different parsers in a proxy chain may disagree on where the body ends. A server that rejects the ambiguous request with `400` eliminates the risk entirely. A server that accepts it (processing via TE alone) is RFC-compliant but relies on connection closure to prevent exploitation.

## Verdicts

- **Pass** — Server rejects with `400` or closes the connection (safest behavior)
- **Warn** — Server responds with `2xx` (RFC-compliant if it processes via TE and closes the connection, but the lenient path)
- **Fail** — Any other response

## Scored / Unscored Justification

This test is **scored**. Although the RFC uses MAY language, there is a clear preferred outcome: rejecting the ambiguous request is safer than accepting it. A `2xx` response counts as a warning rather than a pass, reflecting the security trade-off.

## Sources

- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
- [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
- [PortSwigger — HTTP Request Smuggling](https://portswigger.net/web-security/request-smuggling)
