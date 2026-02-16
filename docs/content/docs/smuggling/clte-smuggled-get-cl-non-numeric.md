---
title: "CLTE-SMUGGLED-GET-CL-NON-NUMERIC"
description: "CLTE-SMUGGLED-GET-CL-NON-NUMERIC sequence test documentation"
weight: 16
---

| | |
|---|---|
| **Test ID** | `SMUG-CLTE-SMUGGLED-GET-CL-NON-NUMERIC` |
| **Category** | Smuggling |
| **Type** | Sequence (single send) |
| **Scored** | Yes |
| **RFC** | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **RFC Level** | MUST |
| **Expected** | `400`, or connection close |

## What it does

This is a variant of `SMUG-CLTE-SMUGGLED-GET` that uses a non-numeric `Content-Length` value (`N<alpha>`) while also sending `Transfer-Encoding: chunked`.

Some HTTP stacks reject non-numeric Content-Length outright; others parse a numeric prefix and ignore the trailing junk. In a proxy chain, this can create framing disagreements that enable request smuggling.

## What it sends

The request body begins with a valid chunked terminator (`0\r\n\r\n`) and then immediately contains an entire `GET /` request.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 45x\r\n
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

Closing the connection after responding prevents any leftover bytes (including an embedded request) from being interpreted as a second request on the same persistent connection.

## Why it matters

Numeric-prefix parsing (`45x` parsed as `45`) is a frequent hardening gap. If one hop reads 45 bytes while another treats the value as invalid, their views of the byte stream diverge and the embedded `GET /` can be executed out of band.

This test checks for smuggling by looking for **multiple HTTP status lines** (multiple responses) after a single client send.

## Verdicts

- **Pass**: The server rejects with `400`, or closes the connection.
- **Fail**: Multiple HTTP responses are observed (embedded GET likely executed).
- **Fail**: The server accepts and keeps the connection open (MUST-close violated), even if no extra response is observed.

## Sources

- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
