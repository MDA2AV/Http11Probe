---
title: "IMS-INVALID"
description: "CAP-IMS-INVALID capability test documentation"
weight: 16
---

| | |
|---|---|
| **Test ID** | `CAP-IMS-INVALID` |
| **Category** | Capabilities |
| **Type** | Sequence (2 steps) |
| **Scored** | No |
| **RFC** | [RFC 9110 §13.1.3](https://www.rfc-editor.org/rfc/rfc9110#section-13.1.3) |
| **RFC Level** | SHOULD |
| **Expected** | `200` |

## What it does

This is a **sequence test** — it sends an `If-Modified-Since` header with an unparseable garbage value to check whether the server correctly ignores it.

### Step 1: Initial GET (confirm 2xx)

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
Connection: keep-alive\r\n
\r\n
```

Verifies the resource exists and returns a success response.

### Step 2: Conditional GET (If-Modified-Since: garbage)

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
If-Modified-Since: not-a-date\r\n
\r\n
```

Sends a value that is not a valid HTTP-date. A compliant server must ignore the header and return the resource normally.

## What the RFC says

> "A recipient MUST ignore If-Modified-Since if the field value is not a valid HTTP-date." — RFC 9110 §13.1.3

## Why it matters

If a server treats an unparseable date as "very old" and returns `304`, it could cause clients to serve stale cached content. Correct behavior is to ignore the invalid header entirely.

## Verdicts

- **Pass** — Step 2 returns `200` (correctly ignores invalid date)
- **Warn** — Server returns `304` (treated garbage as a valid date)
- **Fail** — Unexpected error (non-2xx/304 response)

## Sources

- [RFC 9110 §13.1.3](https://www.rfc-editor.org/rfc/rfc9110#section-13.1.3)
