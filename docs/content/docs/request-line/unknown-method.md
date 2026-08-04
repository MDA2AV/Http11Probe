---
title: "Unknown Method — HTTP/1.1 Compliance"
description: "A request with a completely fabricated method name that no server should recognize. Tested against RFC 9110 §9.1."
weight: 16
---

| | |
|---|---|
| **Test ID** | `COMP-UNKNOWN-METHOD` |
| **Category** | Compliance |
| **RFC** | [RFC 9110 §9.1](https://www.rfc-editor.org/rfc/rfc9110#section-9.1) |
| **Requirement** | SHOULD |
| **Expected** | `501`, `405`, or `400` |

## What it sends

A request with a completely fabricated method name that no server should recognize.

```http
FOOBAR / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

> "An origin server that receives a request method that is unrecognized or not implemented SHOULD respond with the 501 (Not Implemented) status code." -- RFC 9110 Section 9.1

The RFC also states:

> "An origin server that receives a request method that is recognized and implemented, but not allowed for the target resource, SHOULD respond with the 405 (Method Not Allowed) status code." -- RFC 9110 Section 9.1

Since `FOOBAR` is not a recognized method, 501 is the most appropriate response. 405 and 400 are also acceptable alternatives.

## Why it matters

A server that silently accepts unknown methods may execute unintended logic or expose resources that should only be available through specific methods. Proper rejection ensures that only well-defined HTTP semantics are applied to requests.

## Sources

- [RFC 9110 §9.1 -- Overview](https://www.rfc-editor.org/rfc/rfc9110#section-9.1)
