---
title: "CLTE-SMUGGLED-GET-TE-OBS-FOLD"
description: "CLTE-SMUGGLED-GET-TE-OBS-FOLD sequence test documentation"
weight: 17
---

| | |
|---|---|
| **Test ID** | `SMUG-CLTE-SMUGGLED-GET-TE-OBS-FOLD` |
| **Category** | Smuggling |
| **Type** | Sequence (single send) |
| **Scored** | Yes |
| **RFC** | [RFC 9112 §5.2](https://www.rfc-editor.org/rfc/rfc9112#section-5.2) · [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **RFC Level** | MUST |
| **Expected** | `400`, or connection close |

## What it does

This is a variant of `SMUG-CLTE-SMUGGLED-GET` that uses **obs-fold** (obsolete line folding) on the `Transfer-Encoding` header while also sending `Content-Length`.

If a server unfolds obs-fold into `Transfer-Encoding: chunked` and then fails to close the connection, the embedded `GET /` can be interpreted as a second request and the server may emit multiple HTTP responses.

## What it sends

Transfer-Encoding is split across two lines using obs-fold:

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding:\r\n
 chunked\r\n
Content-Length: 45\r\n
\r\n
0\r\n
\r\n
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

> "A server that receives an obs-fold in a request message... **MUST** either reject the message by sending a 400 (Bad Request)... or replace each received obs-fold with one or more SP octets prior to interpreting the field value..." — RFC 9112 §5.2

If unfolded, the message still contains both `Transfer-Encoding` and `Content-Length`, triggering connection safety requirements:

> "**Regardless, the server MUST close the connection after responding** to such a request." — RFC 9112 §6.1

## Why it matters

Obs-fold is a well-known parsing differential: some components unfold it, others treat it as malformed. When it is applied to `Transfer-Encoding` with `Content-Length` present, that disagreement is directly usable as a CL.TE smuggling vector.

This test checks for concrete evidence of request-boundary confusion by looking for **multiple HTTP status lines** (multiple responses) after a single client send. In a proxy chain, this same behavior is what enables request smuggling.

## Verdicts

- **Pass**: The server rejects with `400`, or closes the connection.
- **Fail**: Multiple HTTP responses are observed (embedded GET likely executed).
- **Fail**: The server accepts and keeps the connection open (MUST-close violated), even if no extra response is observed.

## Sources

- [RFC 9112 §5.2](https://www.rfc-editor.org/rfc/rfc9112#section-5.2)
- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
