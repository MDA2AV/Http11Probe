---
title: "CLTE-SMUGGLED-GET-TE-CASE-MISMATCH"
description: "CLTE-SMUGGLED-GET-TE-CASE-MISMATCH sequence test documentation"
weight: 21
---

| | |
|---|---|
| **Test ID** | `SMUG-CLTE-SMUGGLED-GET-TE-CASE-MISMATCH` |
| **Category** | Smuggling |
| **Type** | Sequence (single send) |
| **Scored** | Yes |
| **RFC** | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **RFC Level** | MUST |
| **Expected** | `400`, or connection close |

## What it does

This is a variant of `SMUG-CLTE-SMUGGLED-GET` where the `Transfer-Encoding` token is written as `Chunked` (case mismatch).

Some components are case-insensitive as required by the HTTP token rules; others are not. Any case-sensitivity bug in a proxy chain can reintroduce CL.TE framing disagreement.

## What it sends

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 45\r\n
Transfer-Encoding: Chunked\r\n
\r\n
0\r\n
\r\n
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

> "A server MAY reject a request that contains both Content-Length and Transfer-Encoding... **Regardless, the server MUST close the connection after responding to such a request.**" — RFC 9112 §6.1

## Why it matters

Transfer-coding tokens are a classic source of normalization differences. If one hop treats `Chunked` as `chunked` and another treats it as unknown, message framing can diverge and the embedded `GET` can be processed as a separate request.

## Verdicts

- **Pass**: The server rejects with `400`, or closes the connection.
- **Fail**: Multiple HTTP responses are observed (embedded GET likely executed).
- **Fail**: The server accepts and keeps the connection open (MUST-close violated), even if no extra response is observed.

## Sources

- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
