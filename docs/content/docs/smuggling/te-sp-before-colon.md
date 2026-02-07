---
title: "TE-SP-BEFORE-COLON"
description: "TE-SP-BEFORE-COLON test documentation"
weight: 7
---

| | |
|---|---|
| **Test ID** | `SMUG-TE-SP-BEFORE-COLON` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 Section 5](https://www.rfc-editor.org/rfc/rfc9112#section-5) |
| **Requirement** | MUST |
| **Expected** | `400` |

## What it sends

`Transfer-Encoding : chunked` — space before the colon.

## What the RFC says

Same MUST-reject-with-400 rule as SP-BEFORE-COLON. No whitespace allowed between field name and colon.

## Why it matters

This is the Transfer-Encoding variant of the SP-BEFORE-COLON smuggling technique. If one parser ignores the space and processes chunked encoding while another rejects or ignores the header, they'll frame the body differently.

## Sources

- [RFC 9112 Section 5](https://www.rfc-editor.org/rfc/rfc9112#section-5)
