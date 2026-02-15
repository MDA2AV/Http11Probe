---
title: "RANGE-INVALID"
description: "RANGE-INVALID test documentation"
weight: 14
---

| | |
|---|---|
| **Test ID** | `COMP-RANGE-INVALID` |
| **Category** | Compliance |
| **Scored** | No |
| **RFC** | [RFC 9110 §14.2](https://www.rfc-editor.org/rfc/rfc9110#section-14.2) |
| **RFC Level** | MAY |
| **Expected** | `2xx` (ignore) or `416` |

## What it sends

A GET request with a syntactically invalid `Range` header.

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
Range: bytes=abc-xyz\r\n
\r\n
```

The range value `abc-xyz` does not match the required integer format.

## What the RFC says

> "A server MAY ignore the Range header field." — RFC 9110 §14.2

> "An origin server MUST ignore a Range header field that contains a range unit it does not understand. A proxy MAY discard a Range header field that contains a range unit it does not understand." — RFC 9110 §14.2

> "A server that supports range requests MAY ignore or reject a Range header field that consists of more than two overlapping ranges, or a set of many small ranges that are not listed in ascending order, since both are indications of either a broken client or a deliberate denial-of-service attack." — RFC 9110 §14.2

## Why it matters

A server that receives an unparseable Range value should either ignore it (serve the full resource with `200`) or reject it with `416 Range Not Satisfiable`. Returning `206 Partial Content` with bogus range values could expose unexpected data or cause client-side parsing errors.

## Verdicts

- **Pass** — Server returns `2xx` (ignoring the invalid range) or `416`
- **Warn** — Server returns an unexpected status

## Sources

- [RFC 9110 §14.2](https://www.rfc-editor.org/rfc/rfc9110#section-14.2)
