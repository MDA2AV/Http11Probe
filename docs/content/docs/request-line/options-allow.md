---
title: "OPTIONS Allow — HTTP/1.1 Compliance"
description: "An OPTIONS request to the root path, asking the server to describe its capabilities for that resource. Tested against RFC 9110 §9.3.7."
weight: 18
---

| | |
|---|---|
| **Test ID** | `COMP-OPTIONS-ALLOW` |
| **Category** | Compliance |
| **RFC** | [RFC 9110 §9.3.7](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.7) |
| **Requirement** | SHOULD |
| **Expected** | `2xx` with `Allow` header |

## What it sends

An OPTIONS request to the root path, asking the server to describe its capabilities for that resource.

```http
OPTIONS / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

> "A server generating a successful response to OPTIONS SHOULD send any header that might indicate optional features implemented by the server and applicable to the target resource (e.g., Allow)." -- RFC 9110 Section 9.3.7

## Why it matters

OPTIONS is the standard mechanism for clients to discover which methods a resource supports. The Allow header is the primary vehicle for this information. Without it, the OPTIONS response provides no actionable data. API clients and CORS preflight logic depend on this header to function correctly.

## Sources

- [RFC 9110 §9.3.7 -- OPTIONS](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.7)
