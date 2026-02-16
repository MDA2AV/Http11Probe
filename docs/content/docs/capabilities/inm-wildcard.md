---
title: "INM-WILDCARD"
description: "CAP-INM-WILDCARD capability test documentation"
weight: 14
---

| | |
|---|---|
| **Test ID** | `CAP-INM-WILDCARD` |
| **Category** | Capabilities |
| **Type** | Sequence (2 steps) |
| **Scored** | No |
| **RFC** | [RFC 9110 §13.1.2](https://www.rfc-editor.org/rfc/rfc9110#section-13.1.2) |
| **RFC Level** | SHOULD |
| **Expected** | `304` |

## What it does

This is a **sequence test** — it uses the wildcard `*` value in `If-None-Match` to test whether the server recognizes that any current representation matches.

### Step 1: Initial GET (confirm 2xx)

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
Connection: keep-alive\r\n
\r\n
```

Verifies the resource exists and returns a success response.

### Step 2: Conditional GET (If-None-Match: *)

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
If-None-Match: *\r\n
\r\n
```

The wildcard `*` means "match any entity-tag". Since step 1 confirmed the resource exists, the server should return `304 Not Modified`.

## What the RFC says

> "If the field value is '*', the condition is false if the origin server has a current representation for the target resource." — RFC 9110 §13.1.2

In other words, `If-None-Match: *` means "give me the resource only if it doesn't exist". Since it does exist, the condition is false, and the server should return `304`.

## Why it matters

The wildcard `If-None-Match` is primarily used for preventing the "lost update" problem in PUT requests (only create if absent). For GET, it's a useful test of whether the server has a standards-compliant conditional request implementation beyond simple ETag string matching.

## Verdicts

- **Pass** — Step 2 returns `304 Not Modified`
- **Warn** — Server returns `200` (ignores `If-None-Match: *`)
- **Fail** — Unexpected error (non-2xx/304 response)

## Sources

- [RFC 9110 §13.1.2](https://www.rfc-editor.org/rfc/rfc9110#section-13.1.2)
