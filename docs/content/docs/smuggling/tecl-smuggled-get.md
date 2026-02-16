---
title: "TECL-SMUGGLED-GET"
description: "TECL-SMUGGLED-GET sequence test documentation"
weight: 23
---

| | |
|---|---|
| **Test ID** | `SMUG-TECL-SMUGGLED-GET` |
| **Category** | Smuggling |
| **Type** | Sequence (single send) |
| **Scored** | Yes |
| **RFC** | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **RFC Level** | MUST |
| **Expected** | `400`, or connection close |

## What it does

This test is a TE.CL smuggling confirmation technique inspired by common request smuggling labs.

The body begins with a valid chunk-size line (for the chunked framing), but the `Content-Length` is set to only cover the chunk-size prefix (the `{hex}\r\n` line). If a server incorrectly uses `Content-Length` framing in the presence of `Transfer-Encoding: chunked`, it can leave the chunk-data bytes on the wire and interpret them as the next request.

To make the signal cleaner, the smuggled `GET` includes `Content-Length: 7` so the remaining chunked framing bytes (`\r\n0\r\n\r\n`) get consumed as the smuggled request body if it is parsed as a second request.

## What it sends

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
Content-Length: 4\r\n
\r\n
2b\r\n
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 7\r\n
\r\n
\r\n
0\r\n
\r\n
```

(`2b` and `Content-Length: 4` are examples; the probe computes the exact chunk size and the corresponding prefix length.)

## What the RFC says

> "A server MAY reject a request that contains both Content-Length and Transfer-Encoding... Regardless, the server MUST close the connection after responding to such a request." -- RFC 9112 §6.1

## Why it matters

In real deployments, TE.CL smuggling happens when one hop uses chunked framing and another hop uses Content-Length framing. If the connection is left open, leftover bytes can be interpreted as a new request and smuggled past security controls.

This test looks for concrete evidence of request-boundary confusion by detecting multiple HTTP status lines in the response to a single client send. In a proxy chain, this same behavior is what enables request smuggling.

## Verdicts

- **Pass**: The server rejects with `400`, or closes the connection.
- **Fail**: Multiple HTTP status lines are observed (smuggled `GET` likely executed).
- **Fail**: The server accepts and keeps the connection open (MUST-close violated), even if no extra response is observed.

## Sources

- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
- [HTTP Request Smuggling (PortSwigger)](https://portswigger.net/web-security/request-smuggling)
- [Request smuggling (PortSwigger labs)](https://portswigger.net/web-security/request-smuggling/lab-basic-te-cl)
