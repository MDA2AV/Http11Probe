---
title: "FRAGMENT-IN-TARGET"
description: "FRAGMENT-IN-TARGET test documentation"
weight: 3
---

| | |
|---|---|
| **Test ID** | `RFC9112-3.2-FRAGMENT-IN-TARGET` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2) |
| **Requirement** | SHOULD |
| **Expected** | `400` = Pass; `2xx` = Warn |

## What it sends

A request with a fragment identifier in the URI: `GET /path#frag HTTP/1.1`.

```http
GET /path#frag HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

The origin-form of request-target is defined as:

```
origin-form = absolute-path [ "?" query ]
```

There is no fragment component in this grammar. The `#` character and anything after it are not part of any valid request-target form (origin-form, absolute-form, authority-form, or asterisk-form).

Since the request-line doesn't match any valid form, it is an invalid request-line:

> "Recipients of an invalid request-line **SHOULD** respond with either a 400 (Bad Request) error or a 301 (Moved Permanently) redirect..." — RFC 9112 Section 3

This is a **SHOULD**, not a MUST — servers that strip the fragment and process the path are not violating a mandatory requirement.

## Why it matters

Fragments are a client-side concept used to reference a position within a document. They should never appear on the wire. A server that silently strips fragments may process a different resource than what the client intended, though the practical security risk is low.

**Pass:** Server rejects with `400` (strict parsing).
**Warn:** Server returns `2xx` (likely strips the fragment and processes `/path`).

## Sources

- [RFC 9112 Section 3.2 — origin-form](https://www.rfc-editor.org/rfc/rfc9112#section-3.2)
- [RFC 9112 Section 3 — Request Line](https://www.rfc-editor.org/rfc/rfc9112#section-3)
- [RFC 9110 Section 4.1 — URI References](https://www.rfc-editor.org/rfc/rfc9110#section-4.1)
