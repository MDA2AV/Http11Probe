---
title: "No 1xx HTTP/1.0 — HTTP/1.1 Compliance"
description: "An HTTP/1.0 POST with Expect: 100-continue and a body, designed to test whether the server incorrectly sends a 100 Continue interim response to an HTTP/1.0 client. Tested against RFC 9110 §15.2."
weight: 11
---

| | |
|---|---|
| **Test ID** | `COMP-NO-1XX-HTTP10` |
| **Category** | Compliance |
| **RFC** | [RFC 9110 §15.2](https://www.rfc-editor.org/rfc/rfc9110#section-15.2) |
| **Requirement** | MUST NOT |
| **Expected** | Non-1xx response |

## What it sends

An HTTP/1.0 POST with `Expect: 100-continue` and a body, designed to test whether the server incorrectly sends a `100 Continue` interim response to an HTTP/1.0 client.

```http
POST / HTTP/1.0\r\n
Host: localhost:8080\r\n
Expect: 100-continue\r\n
Content-Length: 5\r\n
\r\n
hello
```

## What the RFC says

> "Since HTTP/1.0 did not define any 1xx status codes, a server MUST NOT send a 1xx response to an HTTP/1.0 client." -- RFC 9110 Section 15.2

## Why it matters

HTTP/1.0 clients do not understand interim responses. If a server sends `100 Continue` to an HTTP/1.0 client, the client may interpret the `100` status line as a malformed final response, discard it as garbage, or enter an undefined state. This is especially dangerous in proxy chains where an HTTP/1.0 hop cannot forward 1xx responses correctly.

## Sources

- [RFC 9110 §15.2 -- Informational 1xx](https://www.rfc-editor.org/rfc/rfc9110#section-15.2)
