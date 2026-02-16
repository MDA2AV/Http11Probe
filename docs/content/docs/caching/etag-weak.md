---
title: "ETAG-WEAK"
description: "CAP-ETAG-WEAK capability test documentation"
weight: 18
---

| | |
|---|---|
| **Test ID** | `CAP-ETAG-WEAK` |
| **Category** | Capabilities |
| **Type** | Sequence (2 steps) |
| **Scored** | No |
| **RFC** | [RFC 9110 §13.1.2](https://www.rfc-editor.org/rfc/rfc9110#section-13.1.2) |
| **RFC Level** | SHOULD |
| **Expected** | `304` |

## What it does

This is a **sequence test** — it captures the server's ETag and resends it with a `W/` weak prefix in `If-None-Match` to test whether the server uses the weak comparison function for GET requests.

### Step 1: Initial GET (capture ETag)

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
Connection: keep-alive\r\n
\r\n
```

Captures the `ETag` header from the response. If the ETag is strong (e.g., `"abc123"`), step 2 will prepend `W/` to make it weak (`W/"abc123"`). If already weak, it is sent as-is.

### Step 2: Conditional GET (If-None-Match: W/etag)

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
If-None-Match: W/"abc123"\r\n
\r\n
```

The weak ETag should still match via the weak comparison function, which only compares the opaque-tag portion.

## What the RFC says

> "A recipient MUST use the weak comparison function when comparing entity-tags for If-None-Match." — RFC 9110 §13.1.2

The weak comparison function is defined as: "two entity-tags are equivalent if their opaque-tags match character-by-character, regardless of either or both being tagged as 'weak'."

## Why it matters

GET conditional requests must use weak comparison. A server that only does byte-for-byte matching of the full ETag string (including `W/` prefix) will fail to match weak ETags, causing unnecessary full responses for cacheable content.

## Verdicts

- **Pass** — Step 2 returns `304` (weak comparison matched)
- **Warn** — No ETag in step 1, or step 2 returns `200` (server didn't use weak comparison)
- **Fail** — Unexpected error (non-2xx/304 response)

## Sources

- [RFC 9110 §13.1.2](https://www.rfc-editor.org/rfc/rfc9110#section-13.1.2)
- [RFC 9110 §8.8.3.2](https://www.rfc-editor.org/rfc/rfc9110#section-8.8.3.2)
