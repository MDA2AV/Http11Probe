---
title: "TE/CL Desync — Request Smuggling"
description: "This is a sequence test that detects TE.CL request boundary desynchronization — the reverse of the classic CL.TE smuggling attack. Tested against RFC 9112 §6.1."
weight: 14
---

| | |
|---|---|
| **Test ID** | `SMUG-TECL-DESYNC` |
| **Category** | Smuggling |
| **Type** | Sequence (2 steps) |
| **Scored** | Yes |
| **RFC** | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **RFC Level** | MUST |
| **Expected** | `400`, or connection close |

## What it does

This is a **sequence test** that detects TE.CL request boundary desynchronization — the reverse of the classic CL.TE smuggling attack.

### Step 1: Poison POST (TE terminates early, CL=30)

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
Content-Length: 30\r\n
\r\n
0\r\n
\r\n
X
```

The `Transfer-Encoding: chunked` body terminates at `0\r\n\r\n` (5 bytes), but `Content-Length` claims 30 bytes. The extra `X` sits after the chunked terminator.

- If the server uses **TE**: reads the chunked terminator (5 bytes), body done. Still expects 25 more bytes per CL — `X` and any subsequent data become part of the expected body or a new request.
- If the server uses **CL**: waits for 30 bytes total, which never arrive (timeout).

### Step 2: Follow-up GET

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

Sent immediately after step 1. If the server used TE and left `X` on the wire, it sees `XGET / HTTP/1.1` — a malformed request that triggers a 400.

## What the RFC says

> "**Regardless, the server MUST close the connection after responding to such a request** to avoid the potential attacks." — RFC 9112 §6.1

## Why it matters

In a proxy chain where the front-end uses CL and the back-end uses TE, this pattern allows an attacker to smuggle a request by placing it after the chunked terminator but within the CL-declared body. This test verifies the server doesn't leave the connection in an ambiguous state.

## Verdicts

- **Pass** — Server returns `400` (rejected outright), OR closes the connection (step 2 never executes)
- **Fail** — Step 2 executes and returns `400` (desync confirmed — poison byte merged with GET)
- **Fail** — Step 2 executes and returns `2xx` (MUST-close violated, connection stayed open)

## Sources

- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
- [RFC 9112 §11.2](https://www.rfc-editor.org/rfc/rfc9112#section-11.2)
