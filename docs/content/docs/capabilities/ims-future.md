---
title: "IMS-FUTURE"
description: "CAP-IMS-FUTURE capability test documentation"
weight: 15
---

| | |
|---|---|
| **Test ID** | `CAP-IMS-FUTURE` |
| **Category** | Capabilities |
| **Type** | Sequence (2 steps) |
| **Scored** | No |
| **RFC** | [RFC 9110 §13.1.3](https://www.rfc-editor.org/rfc/rfc9110#section-13.1.3) |
| **RFC Level** | SHOULD |
| **Expected** | `200` |

## What it does

This is a **sequence test** — it sends an `If-Modified-Since` header with a date far in the future to check whether the server correctly ignores it.

### Step 1: Initial GET (confirm 2xx)

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
Connection: keep-alive\r\n
\r\n
```

Verifies the resource exists and returns a success response.

### Step 2: Conditional GET (If-Modified-Since: future date)

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
If-Modified-Since: Thu, 01 Jan 2099 00:00:00 GMT\r\n
\r\n
```

Sends a future date. A compliant server must ignore an `If-Modified-Since` value that is later than the server's current time and return the resource normally.

## What the RFC says

> "A recipient MUST ignore If-Modified-Since if the field value is not a valid HTTP-date, or if the field value is a date in the future (compared to the server's current time)." — RFC 9110 §13.1.3

## Why it matters

A server that blindly compares dates without checking whether the date is in the future could incorrectly return `304 Not Modified` for every request with a future timestamp, allowing cache-poisoning or stale-content attacks.

## Verdicts

- **Pass** — Step 2 returns `200` (correctly ignores future date)
- **Warn** — Server returns `304` (didn't validate the date against current time)
- **Fail** — Unexpected error (non-2xx/304 response)

## Sources

- [RFC 9110 §13.1.3](https://www.rfc-editor.org/rfc/rfc9110#section-13.1.3)
