---
title: "ETAG-304"
description: "CAP-ETAG-304 capability test documentation"
weight: 10
---

| | |
|---|---|
| **Test ID** | `CAP-ETAG-304` |
| **Category** | Capabilities |
| **Type** | Sequence (2 steps) |
| **Scored** | No |
| **RFC** | [RFC 9110 §13.1.2](https://www.rfc-editor.org/rfc/rfc9110#section-13.1.2) |
| **RFC Level** | SHOULD |
| **Expected** | `304` |

## What it does

This is a **sequence test** — it sends two requests on the same TCP connection to test ETag-based conditional request handling.

### Step 1: Initial GET (capture ETag)

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
Connection: keep-alive\r\n
\r\n
```

Captures the `ETag` header from the response for use in step 2.

### Step 2: Conditional GET (If-None-Match)

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
If-None-Match: {ETag from step 1}\r\n
\r\n
```

Replays the `ETag` value captured from step 1 in an `If-None-Match` header. If the resource hasn't changed, the server should return `304 Not Modified`. If the server did not include an `ETag` header in step 1, the test reports Warn immediately.

## What the RFC says

> "An origin server MUST use the strong comparison function when comparing entity-tags for If-None-Match, because the client intends to use the cached representation." — RFC 9110 §13.1.2

> "If the field value is '*', the condition is false if the origin server has a current representation for the target resource." — RFC 9110 §13.1.2

## Why it matters

ETag-based conditional requests are the most reliable caching mechanism in HTTP. They enable efficient revalidation without relying on timestamps, which can be unreliable across servers or after deployments.

## Verdicts

- **Pass** — Step 2 returns `304 Not Modified`
- **Warn** — Server does not include ETag in step 1, or returns `200` in step 2 (no conditional support)
- **Fail** — Unexpected error (non-2xx/304 response)

## Sources

- [RFC 9110 §13.1.2](https://www.rfc-editor.org/rfc/rfc9110#section-13.1.2)
- [RFC 9110 §8.8.3](https://www.rfc-editor.org/rfc/rfc9110#section-8.8.3)
