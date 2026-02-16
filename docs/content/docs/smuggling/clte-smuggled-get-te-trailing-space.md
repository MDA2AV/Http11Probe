---
title: "CLTE-SMUGGLED-GET-TE-TRAILING-SPACE"
description: "CLTE-SMUGGLED-GET-TE-TRAILING-SPACE sequence test documentation"
weight: 19
---

| | |
|---|---|
| **Test ID** | `SMUG-CLTE-SMUGGLED-GET-TE-TRAILING-SPACE` |
| **Category** | Smuggling |
| **Type** | Sequence (single send) |
| **Scored** | Yes |
| **RFC** | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **RFC Level** | MUST |
| **Expected** | `400`, or connection close |

## What it does

This is a variant of `SMUG-CLTE-SMUGGLED-GET` where the `Transfer-Encoding` value includes a **trailing space** (`chunked␠`).

Some components treat this as invalid and fall back to `Content-Length`; others trim and treat it as `chunked`. In a multi-hop chain, that parsing differential can enable CL.TE request smuggling.

## What it sends

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 45\r\n
Transfer-Encoding: chunked \r\n
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

TE value normalization is a common hardening gap. If one hop trims and another does not, the chain can disagree about message framing and interpret the embedded `GET` as a second request.

## Verdicts

- **Pass**: The server rejects with `400`, or closes the connection.
- **Fail**: Multiple HTTP responses are observed (embedded GET likely executed).
- **Fail**: The server accepts and keeps the connection open (MUST-close violated), even if no extra response is observed.

## Sources

- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
