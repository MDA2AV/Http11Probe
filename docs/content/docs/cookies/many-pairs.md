---
title: "MANY-PAIRS"
description: "COOK-MANY-PAIRS test documentation"
weight: 6
---

| | |
|---|---|
| **Test ID** | `COOK-MANY-PAIRS` |
| **Category** | Cookies |
| **Scored** | No |
| **Expected** | `2xx` or `400`/`431` |

## What it sends

```http
GET /echo HTTP/1.1\r\n
Host: localhost:8080\r\n
Cookie: k0=v0; k1=v1; k2=v2; ... ; k999=v999\r\n
\r\n
```

A `Cookie` header containing 1000 semicolon-separated key=value pairs.

## What the RFC says

> "At least 50 cookies per domain." — RFC 6265 §6.1

The practical limit of 50 cookies per domain is a user-agent guideline. Servers have no mandated limit, but 1000 pairs in a single header tests parser performance boundaries.

## Why it matters

Each cookie pair must be parsed, allocated, and stored in internal data structures. 1000 pairs can trigger:
- **O(n) or O(n^2) parsing** in naive cookie parsers
- **Memory exhaustion** from 1000 individual allocations
- **Hash table collisions** in cookie lookup structures

A well-behaved server should either parse all 1000 pairs or reject the oversized header.

## Verdicts

- **Pass** — `2xx` (survived) or `400`/`431` (rejected gracefully)
- **Fail** — `500` or connection crash

## Sources

- [RFC 6265 §6.1](https://www.rfc-editor.org/rfc/rfc6265#section-6.1) — cookie limits
- [RFC 6585 §5](https://www.rfc-editor.org/rfc/rfc6585#section-5) — 431 status code
