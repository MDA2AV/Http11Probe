---
title: "LEADING-CRLF"
description: "LEADING-CRLF test documentation"
weight: 4
---

| | |
|---|---|
| **Test ID** | `COMP-LEADING-CRLF` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) |
| **Requirement** | MAY ignore (unscored) |
| **Expected** | `400` or `2xx` |

## What it sends

Two leading CRLF sequences before the request-line.

## What the RFC says

> "In the interest of robustness, a server that is expecting to receive and parse a request-line SHOULD ignore at least one empty line (CRLF) received prior to the request-line." — RFC 9112 Section 2.2

## Why it matters

This is an unscored test. The RFC encourages servers to tolerate leading CRLFs. Either `400` (strict) or `2xx` (tolerant) is acceptable.

## Sources

- [RFC 9112 Section 2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2)
