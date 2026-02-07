---
title: "NON-ASCII-URL"
weight: 10
---

| | |
|---|---|
| **Test ID** | `MAL-NON-ASCII-URL` |
| **Category** | Malformed Input |
| **Expected** | `400` or close |

## What it sends

A request with non-ASCII bytes in the URL.

## What the RFC says

URIs are defined in RFC 3986 as sequences of ASCII characters. Non-ASCII bytes must be percent-encoded.

## Sources

- [RFC 3986 Section 2.1](https://www.rfc-editor.org/rfc/rfc3986#section-2.1)
