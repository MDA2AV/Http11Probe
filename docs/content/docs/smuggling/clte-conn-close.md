---
title: "CL/TE Conn Close — Request Smuggling"
description: "This is a sequence test — it sends multiple requests on the same TCP connection to verify server behavior across the full exchange. Tested against RFC 9112 §6.1."
weight: 10
---

| | |
|---|---|
| **Test ID** | `SMUG-CLTE-CONN-CLOSE` |
| **Category** | Smuggling |
| **Type** | Sequence (2 steps) |
| **Scored** | Yes |
| **RFC** | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **RFC Level** | MUST |
| **Expected** | `400`, or `2xx` + connection close |

## What it does

This is a **sequence test** — it sends multiple requests on the same TCP connection to verify server behavior across the full exchange.

### Step 1: Ambiguous POST (CL+TE)

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 5\r\n
Transfer-Encoding: chunked\r\n
\r\n
0\r\n
\r\n
```

A POST with both `Content-Length: 5` and `Transfer-Encoding: chunked`. The chunked body is the `0` terminator (5 bytes), which happens to match the CL value.

### Step 2: Follow-up GET

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

A normal GET sent on the same connection. This step only executes if the connection is still open after step 1.

## What the RFC says

> "A server MAY reject a request that contains both Content-Length and Transfer-Encoding or process such a request in accordance with the Transfer-Encoding alone. **Regardless, the server MUST close the connection after responding to such a request** to avoid the potential attacks." — RFC 9112 §6.1

The key word is "regardless" — even if the server correctly processes the request via TE, it **must** close the connection afterward.

## Why it matters

The MUST-close requirement exists because keeping the connection open after a dual CL+TE request creates a window for request smuggling. If the connection stays alive, any leftover bytes (or a pipelined request) could be misinterpreted. This sequence test verifies the close actually happens.

## Verdicts

- **Pass** — Server returns `400` (rejected outright), OR returns `2xx` and closes the connection (step 2 never executes)
- **Fail** — Server returns `2xx` and keeps the connection open (step 2 executes and gets a response)

## Sources

- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
- [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
