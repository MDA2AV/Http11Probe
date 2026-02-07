---
title: "DUPLICATE-HOST"
description: "DUPLICATE-HOST test documentation"
weight: 2
---

| | |
|---|---|
| **Test ID** | `RFC9110-5.4-DUPLICATE-HOST` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2) |
| **Requirement** | MUST |
| **Expected** | `400` |

## What it sends

A request with two `Host` headers containing different values:

```http
GET / HTTP/1.1\r\n
Host: localhost\r\n
Host: evil.com\r\n
\r\n
```

## What the RFC says

> "A server **MUST** respond with a 400 (Bad Request) status code to... any request message that contains more than one Host header field line or a Host header field with an invalid field value."

Same MUST-400 as the missing Host case. The server must send an actual 400 response.

## Why it matters

Duplicate Host headers with different values are a classic host header injection attack. If the application uses the first Host and the CDN uses the second (or vice versa), the attacker can:
- Poison caches with content for the wrong domain
- Bypass host-based access controls
- Trigger SSRF via internal hostnames

## Sources

- [RFC 9112 Section 3.2 — Request Target](https://www.rfc-editor.org/rfc/rfc9112#section-3.2)
- [RFC 9110 Section 7.2 — Host and :authority](https://www.rfc-editor.org/rfc/rfc9110#section-7.2)
