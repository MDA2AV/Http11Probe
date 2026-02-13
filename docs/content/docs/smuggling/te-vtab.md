---
title: "TE-VTAB"
description: "TE-VTAB test documentation"
weight: 52
---

| | |
|---|---|
| **Test ID** | `SMUG-TE-VTAB` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 §5.5](https://www.rfc-editor.org/rfc/rfc9110#section-5.5), [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **Requirement** | MUST reject invalid transfer-coding token |
| **Expected** | `400` or close |

## What it sends

`Transfer-Encoding: <VTAB>chunked` with `Content-Length` present.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: \x0bchunked\r\n
Content-Length: 5\r\n
\r\n
hello
```

## Why it matters

Control-character obfuscation is a known TE parsing differential. One hop can reject while another normalizes and parses differently.

## Sources

- [RFC 9110 §5.5](https://www.rfc-editor.org/rfc/rfc9110#section-5.5)
- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
