---
title: "Pipeline Safe — Request Smuggling"
description: "This is a baseline sequence test — it sends two clean, unambiguous requests on the same keep-alive connection to verify the server supports normal HTTP/1.1 pipelining."
weight: 15
---

| | |
|---|---|
| **Test ID** | `SMUG-PIPELINE-SAFE` |
| **Category** | Smuggling |
| **Type** | Sequence (2 steps) |
| **Scored** | No |
| **RFC** | [RFC 9112 §9.3](https://www.rfc-editor.org/rfc/rfc9112#section-9.3) |
| **RFC Level** | SHOULD |
| **Expected** | `2xx` + `2xx` |

## What it does

This is a **baseline sequence test** — it sends two clean, unambiguous requests on the same keep-alive connection to verify the server supports normal HTTP/1.1 pipelining.

### Step 1: First GET

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

### Step 2: Second GET

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

Both requests are identical, clean, and unambiguous. No smuggling payload.

## What the RFC says

> "A client that supports persistent connections MAY 'pipeline' its requests (i.e., send multiple requests without waiting for each response). A server MAY process a sequence of pipelined requests in parallel if they all have safe methods." — RFC 9112 §9.3

## Why it matters

This test serves as a **control** for the other sequence tests. If a server can't handle two clean GETs on one connection, the results of desync and MUST-close tests are unreliable — failures could be caused by missing pipelining support rather than smuggling vulnerabilities.

## Verdicts

- **Pass** — Both steps return `2xx` (pipelining works correctly)
- **Warn** — Step 1 returns `2xx` but server closes connection before step 2 (no pipelining support)
- **Fail** — Step 1 does not return `2xx`

## Sources

- [RFC 9112 §9.3](https://www.rfc-editor.org/rfc/rfc9112#section-9.3)
