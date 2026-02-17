---
title: "LAST-MODIFIED-304"
description: "CAP-LAST-MODIFIED-304 capability test documentation"
weight: 11
---

| | |
|---|---|
| **Test ID** | `CAP-LAST-MODIFIED-304` |
| **Category** | Capabilities |
| **Type** | Sequence (2 steps) |
| **Scored** | No |
| **RFC** | [RFC 9110 §13.1.3](https://www.rfc-editor.org/rfc/rfc9110#section-13.1.3) |
| **RFC Level** | SHOULD |
| **Expected** | `304` |

## What it does

This is a **sequence test** — it sends two requests on the same TCP connection to test Last-Modified-based conditional request handling.

### Step 1: Initial GET (capture Last-Modified)

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
Connection: keep-alive\r\n
\r\n
```

Captures the `Last-Modified` header from the response for use in step 2.

### Step 2: Conditional GET (If-Modified-Since)

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
If-Modified-Since: {Last-Modified from step 1}\r\n
\r\n
```

Replays the `Last-Modified` value captured from step 1 in an `If-Modified-Since` header. If the resource hasn't changed since that date, the server should return `304 Not Modified`. If the server did not include a `Last-Modified` header in step 1, the test reports Warn immediately.

## What the RFC says

> "A recipient MUST ignore If-Modified-Since if the request contains an If-None-Match header field... The condition in If-Modified-Since is only evaluated if the request is for a safe method." — RFC 9110 §13.1.3

## Why it matters

Last-Modified is the oldest conditional request mechanism in HTTP and remains widely deployed. Unlike ETags, it relies on timestamps, which makes it less precise but simpler to implement for static file servers.

## Verdicts

- **Pass** — Step 2 returns `304 Not Modified`
- **Warn** — Server does not include Last-Modified in step 1, or returns `200` in step 2 (no conditional support)
- **Fail** — Unexpected error (non-2xx/304 response)

## Sources

- [RFC 9110 §13.1.3](https://www.rfc-editor.org/rfc/rfc9110#section-13.1.3)
- [RFC 9110 §8.8.2](https://www.rfc-editor.org/rfc/rfc9110#section-8.8.2)
