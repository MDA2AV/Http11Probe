---
title: "ETAG-IN-304"
description: "CAP-ETAG-IN-304 capability test documentation"
weight: 12
---

| | |
|---|---|
| **Test ID** | `CAP-ETAG-IN-304` |
| **Category** | Capabilities |
| **Type** | Sequence (2 steps) |
| **Scored** | No |
| **RFC** | [RFC 9110 §15.4.5](https://www.rfc-editor.org/rfc/rfc9110#section-15.4.5) |
| **RFC Level** | SHOULD |
| **Expected** | `304` with ETag |

## What it does

This is a **sequence test** — it verifies that a `304 Not Modified` response includes the `ETag` header, allowing clients to update their cached validators.

### Step 1: Initial GET (capture ETag)

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
Connection: keep-alive\r\n
\r\n
```

Captures the `ETag` header from the response.

### Step 2: Conditional GET (If-None-Match)

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
If-None-Match: {ETag from step 1}\r\n
\r\n
```

Replays the `ETag` value captured from step 1. If the server returns `304`, this test checks whether the `ETag` header is present in that response.

## What the RFC says

> "A server generating a 304 response MUST generate any of the following header fields that would have been sent in a 200 (OK) response to the same request: ... ETag" — RFC 9110 §15.4.5

## Why it matters

Including the ETag in a `304` response lets clients confirm which representation they have cached and update their stored validator. Without it, clients may lose track of the ETag and fall back to unconditional requests.

## Verdicts

- **Pass** — Step 2 returns `304` with an ETag header
- **Warn** — Server does not support ETags, or returns `304` without an ETag header
- **Fail** — Unexpected error (non-2xx/304 response)

## Sources

- [RFC 9110 §15.4.5](https://www.rfc-editor.org/rfc/rfc9110#section-15.4.5)
- [RFC 9110 §8.8.3](https://www.rfc-editor.org/rfc/rfc9110#section-8.8.3)
