---
title: "TRACE-SENSITIVE"
description: "TRACE-SENSITIVE test documentation"
weight: 33
---

| | |
|---|---|
| **Test ID** | `COMP-TRACE-SENSITIVE` |
| **Category** | Compliance |
| **Scored** | No |
| **RFC** | [RFC 9110 §9.3.8](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.8) |
| **RFC Level** | SHOULD |
| **Expected** | Sensitive headers excluded from echo |

## What it sends

A TRACE request that includes an `Authorization` header with a bearer token.

```http
TRACE / HTTP/1.1\r\n
Host: localhost:8080\r\n
Authorization: Bearer secret-token-123\r\n
\r\n
```

The test checks whether the echoed response body contains the sensitive token value.

## What the RFC says

> "A client MUST NOT generate header fields in a TRACE request containing sensitive data that might be disclosed by the response. For example, it would be foolish for a user agent to send stored credentials [RFC2617] in a TRACE request." — RFC 9110 §9.3.8

> "A server SHOULD exclude any request header fields that are likely to contain sensitive data when that server generates the response to a TRACE request." — RFC 9110 §9.3.8

## Why it matters

TRACE echoes the received request back in the response body. If the server includes sensitive headers like `Authorization`, `Cookie`, or `Proxy-Authorization` in the echo, an attacker who can trigger a TRACE request (via XSS or other means) can steal authentication credentials. This is the basis of the Cross-Site Tracing (XST) attack.

## Verdicts

- **Pass** — TRACE disabled (`405`/`501`), or TRACE response excludes the Authorization header
- **Warn** — TRACE echoes the `Authorization` header including the secret token

## Sources

- [RFC 9110 §9.3.8](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.8)
