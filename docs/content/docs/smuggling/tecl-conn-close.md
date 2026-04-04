---
title: "TECL-CONN-CLOSE"
description: "TECL-CONN-CLOSE sequence test documentation"
weight: 11
---

| | |
|---|---|
| **Test ID** | `SMUG-TECL-CONN-CLOSE` |
| **Category** | Smuggling |
| **Type** | Sequence (2 steps) |
| **Scored** | Yes |
| **RFC** | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **RFC Level** | MUST |
| **Expected** | `400`, or `2xx` + connection close |

## What it does

This is a **sequence test** — it sends multiple requests on the same TCP connection to verify server behavior across the full exchange. It is a mirror of [CLTE-CONN-CLOSE](/docs/smuggling/clte-conn-close/) with the header order reversed.

### Step 1: Ambiguous POST (TE+CL)

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
Content-Length: 5\r\n
\r\n
0\r\n
\r\n
```

A POST with `Transfer-Encoding: chunked` listed **before** `Content-Length: 5`. Some parsers treat headers differently depending on order. The chunked body is the `0` terminator (5 bytes), matching the CL value.

### Step 2: Follow-up GET

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

A normal GET sent on the same connection. This step only executes if the connection is still open after step 1.

## What the RFC says

> "A server MAY reject a request that contains both Content-Length and Transfer-Encoding or process such a request in accordance with the Transfer-Encoding alone. **Regardless, the server MUST close the connection after responding to such a request** to avoid the potential attacks." — RFC 9112 §6.1

The MUST-close requirement applies regardless of header order. This test verifies servers don't accidentally rely on header ordering when deciding whether to close.

## Why it matters

Some servers process headers in order and may handle `TE, CL` differently from `CL, TE`. If a server only triggers its MUST-close logic when `Content-Length` appears first, the reversed order could bypass the protection, leaving the connection open for smuggling.

## Verdicts

- **Pass** — Server returns `400` (rejected outright), OR returns `2xx` and closes the connection (step 2 never executes)
- **Fail** — Server returns `2xx` and keeps the connection open (step 2 executes and gets a response)

## Sources

- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
