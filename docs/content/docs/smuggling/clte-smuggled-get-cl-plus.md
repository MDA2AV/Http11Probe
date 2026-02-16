---
title: "CLTE-SMUGGLED-GET-CL-PLUS"
description: "CLTE-SMUGGLED-GET-CL-PLUS sequence test documentation"
weight: 15
---

| | |
|---|---|
| **Test ID** | `SMUG-CLTE-SMUGGLED-GET-CL-PLUS` |
| **Category** | Smuggling |
| **Type** | Sequence (single send) |
| **Scored** | Yes |
| **RFC** | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **RFC Level** | MUST |
| **Expected** | `400`, or connection close |

## What it does

This is a variant of `SMUG-CLTE-SMUGGLED-GET` that uses a malformed `Content-Length` header (`+N`) while also sending `Transfer-Encoding: chunked`.

Some HTTP stacks reject `Content-Length: +N` as invalid; others parse it leniently. In a proxy chain, these disagreements can reintroduce classic CL.TE smuggling.

## What it sends

The request body begins with a valid chunked terminator (`0\r\n\r\n`) and then immediately contains an entire `GET /` request.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: +45\r\n
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

Even if a server chooses to accept and process the message, it must close the connection afterward to prevent request boundary confusion and smuggling.

## Why it matters

Malformed framing headers are a common source of front-end/back-end parsing disagreements. If one hop accepts `Content-Length: +N` while another rejects it (or ignores it in favor of chunked framing), the embedded `GET /` can be interpreted as a separate request.

This test looks for concrete evidence of request-boundary confusion by checking whether the server emits **multiple HTTP status lines** (multiple responses) after a single client send. In a proxy chain, this same behavior is what enables request smuggling.

## Verdicts

- **Pass**: The server rejects with `400`, or closes the connection.
- **Fail**: Multiple HTTP responses are observed (embedded GET likely executed).
- **Fail**: The server accepts and keeps the connection open (MUST-close violated), even if no extra response is observed.

## Sources

- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
