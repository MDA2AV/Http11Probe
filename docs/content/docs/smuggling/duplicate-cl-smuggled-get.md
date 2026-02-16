---
title: "DUPLICATE-CL-SMUGGLED-GET"
description: "DUPLICATE-CL-SMUGGLED-GET sequence test documentation"
weight: 24
---

| | |
|---|---|
| **Test ID** | `SMUG-DUPLICATE-CL-SMUGGLED-GET` |
| **Category** | Smuggling |
| **Type** | Sequence (single send) |
| **Scored** | Yes |
| **RFC** | [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) |
| **RFC Level** | MUST |
| **Expected** | `400`, or connection close |

## What it does

This is a CL.CL smuggling confirmation variant of `SMUG-DUPLICATE-CL`.

It sends two different `Content-Length` header fields and includes an embedded `GET /` request immediately after the shorter body's boundary. If a server selects the shorter Content-Length value, the embedded `GET` can be interpreted as the next request on the connection and cause multiple responses to be emitted.

## What it sends

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 4\r\n
Content-Length: 49\r\n
\r\n
PINGGET / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

(Actual `Content-Length: 49` is computed to match the body bytes.)

## What the RFC says

RFC 9110 defines `Content-Length` as a single decimal length. Conflicting Content-Length header fields make framing invalid:

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error." -- RFC 9110 §8.6

## Why it matters

CL.CL disagreement is a classic request smuggling vector when two HTTP processors choose different Content-Length values (for example, first vs last header). If the connection stays open, the leftover bytes can become a smuggled request.

This test looks for concrete evidence of request-boundary confusion by detecting multiple HTTP status lines in the response to a single client send. In a proxy chain, this same behavior is what enables request smuggling.

## Verdicts

- **Pass**: The server rejects with `400`, or closes the connection.
- **Fail**: Multiple HTTP status lines are observed (embedded `GET` likely executed).
- **Fail**: The server accepts and keeps the connection open (duplicate CL not rejected).

## Sources

- [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
- [HTTP Request Smuggling (PortSwigger)](https://portswigger.net/web-security/request-smuggling)
