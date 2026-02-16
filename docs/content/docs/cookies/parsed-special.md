---
title: "PARSED-SPECIAL"
description: "COOK-PARSED-SPECIAL test documentation"
weight: 12
---

| | |
|---|---|
| **Test ID** | `COOK-PARSED-SPECIAL` |
| **Category** | Cookies |
| **Scored** | No |
| **Expected** | `2xx` (no crash) |

## What it sends

```http
GET /cookie HTTP/1.1\r\n
Host: localhost:8080\r\n
Cookie: a=hello world; b=x=y\r\n
\r\n
```

Two cookies with edge-case values:
- `a=hello world` — contains a space (technically invalid per RFC 6265 cookie-octet, but common in practice)
- `b=x=y` — contains an `=` sign in the value

## What the RFC says

> "cookie-octet = %x21 / %x23-2B / %x2D-3A / %x3C-5B / %x5D-7E" — RFC 6265 §4.1.1

A space (`0x20`) is not in the `cookie-octet` set, making `hello world` technically invalid. However, many frameworks accept spaces in cookie values for compatibility. The `=` sign (`0x3D`) is in the `cookie-octet` range (`%x3C-5B`), so `x=y` is valid.

## Why it matters

Real-world cookies often contain characters that are technically outside the RFC 6265 grammar. Frameworks must decide whether to be strict (reject) or lenient (accept). Either approach is acceptable — crashing is not.

The `=` in `b=x=y` is a common parser edge case: the parser must split on the *first* `=` only, yielding key `b` and value `x=y`.

## Verdicts

- **Pass** — `2xx` (survived, with or without correct parsing)
- **Warn** — `404` (endpoint not available)
- **Fail** — `500` or connection crash

## Sources

- [RFC 6265 §4.1.1](https://www.rfc-editor.org/rfc/rfc6265#section-4.1.1) — cookie-octet syntax
