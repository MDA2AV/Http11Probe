---
title: "CLTE-DESYNC"
description: "CLTE-DESYNC sequence test documentation"
weight: 13
---

| | |
|---|---|
| **Test ID** | `SMUG-CLTE-DESYNC` |
| **Category** | Smuggling |
| **Type** | Sequence (2 steps) |
| **Scored** | Yes |
| **RFC** | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **RFC Level** | MUST |
| **Expected** | `400`, or connection close |

## What it does

This is a **sequence test** that detects actual CL.TE request boundary desynchronization — the classic request smuggling attack.

### Step 1: Poison POST (CL=6, TE=chunked, extra byte)

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 6\r\n
Transfer-Encoding: chunked\r\n
\r\n
0\r\n
\r\n
X
```

The chunked body terminates at `0\r\n\r\n` (5 bytes), but `Content-Length` claims 6 bytes. The extra `X` byte sits right after the chunked terminator.

- If the server uses **TE**: reads the chunked terminator (5 bytes), body done. `X` is leftover on the wire.
- If the server uses **CL**: reads 6 bytes (`0\r\n\r\nX`), body done.

Either way, `X` may poison the connection.

### Step 2: Follow-up GET

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

Sent immediately after step 1. If `X` is still on the wire, the server sees `XGET / HTTP/1.1` — a malformed request line that triggers a 400.

## What the RFC says

> "A server MAY reject a request that contains both Content-Length and Transfer-Encoding... **Regardless, the server MUST close the connection after responding to such a request.**" — RFC 9112 §6.1

The only safe outcomes are rejection (400) or closing the connection. Any other behavior risks desynchronization.

## Why it matters

This test detects request **desynchronization on a single server**, not a full proxy-chain exploit. If the poison byte `X` merges with the follow-up GET, the server's request boundary parsing is broken. In a real proxy chain, this class of bug is what enables request smuggling.

## Verdicts

- **Pass** — Server returns `400` (rejected outright), OR closes the connection (step 2 never executes)
- **Fail** — Step 2 executes and returns `400` (desync confirmed — poison byte merged with GET)
- **Fail** — Step 2 executes and returns `2xx` (MUST-close violated, connection stayed open)

## Sources

- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
- [RFC 9112 §11.2](https://www.rfc-editor.org/rfc/rfc9112#section-11.2)
