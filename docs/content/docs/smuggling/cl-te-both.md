---
title: "CL-TE-BOTH"
description: "CL-TE-BOTH test documentation"
weight: 1
---

| | |
|---|---|
| **Test ID** | `RFC9112-6.1-CL-TE-BOTH` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 Section 6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **Requirement** | "ought to" |
| **Expected** | `400` or close |

## What it sends

A request with both `Content-Length` and `Transfer-Encoding` headers present.

## What the RFC says

> "If a message is received with both a Transfer-Encoding and a Content-Length header field, the Transfer-Encoding overrides the Content-Length. Such a message might indicate an attempt to perform request smuggling or response splitting and **ought to** be handled as an error."

The "ought to" language is between SHOULD and MAY.

## Why it matters

This is **the** classic request smuggling setup. If the front-end uses Content-Length and the back-end uses Transfer-Encoding (or vice versa), they disagree on body boundaries.

## Sources

- [RFC 9112 Section 6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
- [RFC 9110 Section 16.3.1](https://www.rfc-editor.org/rfc/rfc9110#section-16.3.1)
