---
title: "ECHO"
description: "COOK-ECHO cookie test documentation"
weight: 1
---

| | |
|---|---|
| **Test ID** | `COOK-ECHO` |
| **Category** | Cookies |
| **Scored** | No |
| **RFC Level** | N/A |
| **Expected** | `2xx with Cookie in body` |

## What it sends

Basic Cookie header echoed back by /echo endpoint.

```http
GET /echo HTTP/1.1\r\n
Host: localhost:8080\r\n
Cookie: foo=bar\r\n
\r\n
```

## Why it matters

Baseline test — verifies the server's echo endpoint reflects Cookie headers, which is required for all other cookie tests to work.

## Verdicts

- **Pass** — 2xx and body contains `Cookie:` header
- **Fail** — No response or missing header

## Sources

- [RFC 6265 §5.4](https://www.rfc-editor.org/rfc/rfc6265#section-5.4) — Cookie header
