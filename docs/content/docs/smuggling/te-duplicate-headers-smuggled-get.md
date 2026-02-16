---
title: "TE-DUPLICATE-HEADERS-SMUGGLED-GET"
description: "TE-DUPLICATE-HEADERS-SMUGGLED-GET sequence test documentation"
weight: 22
---

| | |
|---|---|
| **Test ID** | `SMUG-TE-DUPLICATE-HEADERS-SMUGGLED-GET` |
| **Category** | Smuggling |
| **Type** | Sequence (single send) |
| **Scored** | Yes |
| **RFC** | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **RFC Level** | MUST |
| **Expected** | `400`, or connection close |

## What it does

This is a TE.TE + CL ambiguity variant of `SMUG-CLTE-SMUGGLED-GET`.

It sends two `Transfer-Encoding` header fields with different values (`chunked` and `identity`) plus a `Content-Length`, and embeds a full `GET /` request after the chunked terminator. If the server keeps the connection reusable and the embedded request is executed, the probe will observe multiple HTTP status lines after a single client send.

## What it sends

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
Transfer-Encoding: identity\r\n
Content-Length: 45\r\n
\r\n
0\r\n
\r\n
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

(Actual `Content-Length` is computed to match the body bytes.)

## What the RFC says

> "A server MAY reject a request that contains both Content-Length and Transfer-Encoding... Regardless, the server MUST close the connection after responding to such a request." -- RFC 9112 §6.1

## Why it matters

Request smuggling often relies on parsing disagreements about:

- whether duplicate TE header fields are merged, rejected, or one is ignored
- whether CL is honored in the presence of TE
- whether the connection is kept open after an ambiguous request

This test looks for concrete evidence of request-boundary confusion by detecting multiple HTTP responses produced from a single client send. In a proxy chain, this same behavior is what enables request smuggling.

## Verdicts

- **Pass**: The server rejects with `400`, or closes the connection.
- **Fail**: Multiple HTTP status lines are observed (embedded `GET` likely executed).
- **Fail**: The server accepts and keeps the connection open (MUST-close violated), even if no extra response is observed.

## Sources

- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
- [HTTP Request Smuggling (PortSwigger)](https://portswigger.net/web-security/request-smuggling)
