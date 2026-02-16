---
title: "CLTE-SMUGGLED-GET"
description: "CLTE-SMUGGLED-GET sequence test documentation"
weight: 14
---

| | |
|---|---|
| **Test ID** | `SMUG-CLTE-SMUGGLED-GET` |
| **Category** | Smuggling |
| **Type** | Sequence (single send) |
| **Scored** | Yes |
| **RFC** | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **RFC Level** | MUST |
| **Expected** | `400`, or connection close |

## What it does

This test is the "real" version of `SMUG-CLTE-DESYNC`: instead of smuggling a single poison byte (`X`), it smuggles a complete `GET /` request into the ambiguous body.

If a server accepts an ambiguous `Content-Length` + `Transfer-Encoding: chunked` request and keeps the connection open, it risks parsing the embedded `GET /` as a second request and sending **two HTTP responses** on one connection.

## What it sends

The request body begins with a valid chunked terminator (`0\r\n\r\n`) and then immediately contains an entire `GET /` request.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 45\r\n
Transfer-Encoding: chunked\r\n
\r\n
0\r\n
\r\n
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

> "A server MAY reject a request that contains both Content-Length and Transfer-Encoding... **Regardless, the server MUST close the connection after responding to such a request.**" — RFC 9112 §6.1

This rule exists specifically to prevent request smuggling and desynchronization when different HTTP processors disagree about message boundaries.

## Why it matters

In a real proxy chain, if a front-end uses `Content-Length` while a back-end uses `Transfer-Encoding: chunked`, the embedded `GET /` can be treated as a separate request by the back-end and "smuggled" past the front-end's routing and security checks.

This test looks for concrete evidence of request-boundary confusion by checking whether the server emits **multiple HTTP status lines** (multiple responses) after a single client send. In a proxy chain, this same behavior is what enables request smuggling.

## Verdicts

- **Pass**: The server rejects with `400`, or closes the connection.
- **Fail**: Multiple HTTP responses are observed (embedded GET likely executed).
- **Fail**: The server accepts and keeps the connection open (MUST-close violated), even if no extra response is observed.

## Sources

- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
- [RFC 9112 §11.2](https://www.rfc-editor.org/rfc/rfc9112#section-11.2)
