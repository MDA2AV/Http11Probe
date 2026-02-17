---
title: "INM-UNQUOTED"
description: "CAP-INM-UNQUOTED capability test documentation"
weight: 17
---

| | |
|---|---|
| **Test ID** | `CAP-INM-UNQUOTED` |
| **Category** | Capabilities |
| **Type** | Sequence (2 steps) |
| **Scored** | No |
| **RFC** | [RFC 9110 §8.8.3](https://www.rfc-editor.org/rfc/rfc9110#section-8.8.3) |
| **RFC Level** | SHOULD |
| **Expected** | `200` |

## What it does

This is a **sequence test** — it captures the server's ETag, strips the surrounding quotes, and sends it back unquoted in `If-None-Match` to test whether the server enforces ETag syntax.

### Step 1: Initial GET (capture ETag)

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
Connection: keep-alive\r\n
\r\n
```

Captures the `ETag` header from the response for use in step 2.

### Step 2: Conditional GET (If-None-Match: unquoted)

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
If-None-Match: {ETag from step 1, unquoted}\r\n
\r\n
```

Sends the ETag value captured from step 1, stripped of the required surrounding double quotes. According to the RFC grammar, `entity-tag = [ weak ] opaque-tag` and `opaque-tag = DQUOTE *etagc DQUOTE` — the quotes are mandatory.

## What the RFC says

> `entity-tag = [ weak ] opaque-tag`
> `opaque-tag = DQUOTE *etagc DQUOTE` — RFC 9110 §8.8.3

An unquoted value violates the entity-tag syntax. A strict server should not match it.

## Why it matters

Accepting unquoted ETags means the server is doing lenient parsing of conditional headers. While not a security vulnerability, it indicates relaxed syntax validation that could mask other parsing issues.

## Verdicts

- **Pass** — Step 2 returns `200` (correctly rejects malformed ETag syntax)
- **Warn** — No ETag in step 1 (no ETag support), or step 2 returns `304` (accepted unquoted ETag)
- **Fail** — Unexpected error (non-2xx/304 response)

## Sources

- [RFC 9110 §8.8.3](https://www.rfc-editor.org/rfc/rfc9110#section-8.8.3)
- [RFC 9110 §13.1.2](https://www.rfc-editor.org/rfc/rfc9110#section-13.1.2)
