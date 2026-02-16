---
title: "INM-PRECEDENCE"
description: "CAP-INM-PRECEDENCE capability test documentation"
weight: 13
---

| | |
|---|---|
| **Test ID** | `CAP-INM-PRECEDENCE` |
| **Category** | Capabilities |
| **Type** | Sequence (2 steps) |
| **Scored** | No |
| **RFC** | [RFC 9110 §13.1.2](https://www.rfc-editor.org/rfc/rfc9110#section-13.1.2) |
| **RFC Level** | SHOULD |
| **Expected** | `304` |

## What it does

This is a **sequence test** — it sends a conditional GET with both `If-None-Match` (matching ETag) and `If-Modified-Since` (epoch timestamp, guaranteed stale) to verify that the server correctly gives precedence to ETag matching.

### Step 1: Initial GET (capture ETag)

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
Connection: keep-alive\r\n
\r\n
```

Captures the `ETag` header from the response.

### Step 2: Conditional GET (INM + stale IMS)

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
If-None-Match: "abc123"\r\n
If-Modified-Since: Thu, 01 Jan 1970 00:00:00 GMT\r\n
\r\n
```

The `If-None-Match` header matches the current ETag (should produce `304`), but the `If-Modified-Since` is set to epoch (should produce `200` since the resource was certainly modified after 1970). If the server returns `304`, it correctly evaluated `If-None-Match` first.

## What the RFC says

> "A recipient MUST ignore If-Modified-Since if the request contains an If-None-Match header field; the condition in If-None-Match is considered to be a more accurate replacement for the condition in If-Modified-Since." — RFC 9110 §13.1.3

## Why it matters

This is a **MUST**-level requirement in RFC 9110 §13.1.3 for servers that support both mechanisms. If a server evaluates `If-Modified-Since` instead of (or in addition to) `If-None-Match`, clients may get unexpected `200` responses and re-download unchanged resources.

## Verdicts

- **Pass** — Step 2 returns `304` (If-None-Match took precedence)
- **Warn** — Server does not support ETags, or returns `200` (If-Modified-Since took precedence)
- **Fail** — Unexpected error (non-2xx/304 response)

## Sources

- [RFC 9110 §13.1.2](https://www.rfc-editor.org/rfc/rfc9110#section-13.1.2)
- [RFC 9110 §13.1.3](https://www.rfc-editor.org/rfc/rfc9110#section-13.1.3)
