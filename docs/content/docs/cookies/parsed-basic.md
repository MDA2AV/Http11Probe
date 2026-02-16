---
title: "PARSED-BASIC"
description: "COOK-PARSED-BASIC test documentation"
weight: 9
---

| | |
|---|---|
| **Test ID** | `COOK-PARSED-BASIC` |
| **Category** | Cookies |
| **Scored** | No |
| **Expected** | `2xx` with `foo=bar` in body |

## What it sends

```http
GET /cookie HTTP/1.1\r\n
Host: localhost:8080\r\n
Cookie: foo=bar\r\n
\r\n
```

A simple request with a single cookie, targeting the `/cookie` endpoint which returns parsed cookie key=value pairs.

## What the RFC says

> "cookie-pair = cookie-name '=' cookie-value" — RFC 6265 §4.1.1

`foo=bar` is a perfectly valid cookie-pair. The framework parser should extract `foo` with value `bar`.

## Why it matters

This is the baseline for parsed-cookie tests. It confirms that the framework's cookie parser can extract a simple cookie and return it. If this fails, the framework has a fundamental cookie parsing issue.

## Verdicts

- **Pass** — `2xx` with `foo=bar` in the response body
- **Warn** — `404` (endpoint not available on this server)
- **Fail** — `2xx` without `foo=bar`, or `500`

## Sources

- [RFC 6265 §4.1.1](https://www.rfc-editor.org/rfc/rfc6265#section-4.1.1) — cookie-pair syntax
