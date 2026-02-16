---
title: "CLTE-SMUGGLED-HEAD"
description: "CLTE-SMUGGLED-HEAD sequence test documentation"
weight: 18
---

| | |
|---|---|
| **Test ID** | `SMUG-CLTE-SMUGGLED-HEAD` |
| **Category** | Smuggling |
| **Type** | Sequence (single send) |
| **Scored** | Yes |
| **RFC** | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **RFC Level** | MUST |
| **Expected** | `400`, or connection close |

## What it does

This is a `HEAD`-based confirmation variant of `SMUG-CLTE-SMUGGLED-GET`.

It sends an ambiguous `Content-Length` + `Transfer-Encoding: chunked` request whose body contains a complete smuggled `HEAD /` request. If the server parses the body bytes as a second request, it may emit **multiple HTTP responses** after a single client send.

## What it sends

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 46\r\n
Transfer-Encoding: chunked\r\n
\r\n
0\r\n
\r\n
HEAD / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

> "A server MAY reject a request that contains both Content-Length and Transfer-Encoding... **Regardless, the server MUST close the connection after responding to such a request.**" — RFC 9112 §6.1

## Why it matters

The ambiguity is the same as classic CL.TE smuggling. Using `HEAD` as the embedded request helps confirm tunneling/smuggling behavior in stacks where response bodies are suppressed or buffered differently.

## Verdicts

- **Pass**: The server rejects with `400`, or closes the connection.
- **Fail**: Multiple HTTP responses are observed (embedded `HEAD` likely executed).
- **Fail**: The server accepts and keeps the connection open (MUST-close violated), even if no extra response is observed.

## Sources

- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
