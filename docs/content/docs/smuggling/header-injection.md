---
title: "HEADER-INJECTION"
weight: 12
---

| | |
|---|---|
| **Test ID** | `SMUG-HEADER-INJECTION` |
| **Category** | Smuggling (Unscored) |
| **Expected** | `400` or close |

## What it sends

A header value containing CRLF followed by an injected header line — attempting to inject additional headers via a field value.

## Why it matters

If the server doesn't validate field values for CRLF sequences, an attacker can inject arbitrary headers. This can lead to response splitting, cache poisoning, or session hijacking.

## Sources

- [RFC 9110 Section 5.5](https://www.rfc-editor.org/rfc/rfc9110#section-5.5)
