---
title: "PARSED-BASIC"
description: "COOK-PARSED-BASIC cookie test documentation"
weight: 9
---

| | |
|---|---|
| **Test ID** | `COOK-PARSED-BASIC` |
| **Category** | Cookies |
| **Scored** | No |
| **RFC Level** | N/A |
| **Expected** | `2xx with foo=bar in body` |

## What it sends

Basic cookie parsed correctly by framework.

```http
GET /cookie HTTP/1.1\r\n
Host: localhost:8080\r\n
Cookie: foo=bar\r\n
\r\n
```

## Why it matters

Tests that the framework's cookie parser correctly extracts a simple name=value pair — the most basic cookie parsing operation.

## Verdicts

- **Pass** — 2xx and body contains `foo=bar`
- **Warn** — 404 (endpoint not available)
- **Fail** — 500 or mangled output

## Sources

- [RFC 6265 §5.4](https://www.rfc-editor.org/rfc/rfc6265#section-5.4) — Cookie header
