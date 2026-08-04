---
title: "Date Header — HTTP/1.1 Compliance"
description: "A standard GET request. The test validates that the server includes a Date header in its response. Tested against RFC 9110 §6.6.1."
weight: 10
---

| | |
|---|---|
| **Test ID** | `COMP-DATE-HEADER` |
| **Category** | Compliance |
| **RFC** | [RFC 9110 §6.6.1](https://www.rfc-editor.org/rfc/rfc9110#section-6.6.1) |
| **Requirement** | MUST |
| **Expected** | `2xx` with `Date` header |

## What it sends

A standard GET request. The test validates that the server includes a `Date` header in its response.

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

> "An origin server with a clock MUST generate a Date header field in all 2xx (Successful), 3xx (Redirection), and 4xx (Client Error) responses, and MAY generate a Date header field in 1xx (Informational) and 5xx (Server Error) responses." -- RFC 9110 Section 6.6.1

## Why it matters

The Date header is essential for HTTP caching. Caches use it to calculate age, determine freshness, and resolve clock skew between origin servers and intermediaries. Without it, caches cannot properly compute expiration times, leading to either stale content being served or unnecessary revalidation.

## Sources

- [RFC 9110 §6.6.1 -- Date](https://www.rfc-editor.org/rfc/rfc9110#section-6.6.1)
