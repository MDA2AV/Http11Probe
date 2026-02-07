---
title: "SP-BEFORE-COLON"
weight: 1
---

| | |
|---|---|
| **Test ID** | `RFC9110-5.6.2-SP-BEFORE-COLON` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 5](https://www.rfc-editor.org/rfc/rfc9112#section-5) |
| **Requirement** | MUST |
| **Expected** | `400` |

## What it sends

A request with a space between the header field name and the colon: `Host : localhost`.

## What the RFC says

> "No whitespace is allowed between the field name and colon. In the past, differences in the handling of such whitespace have led to security vulnerabilities in request routing and response handling. A server **MUST** reject, with a response status code of 400 (Bad Request), any received request message that contains whitespace between a header field name and colon."

This is one of the strongest requirements in the HTTP spec — **MUST reject with 400 specifically**. Not close, not 500 — exactly 400.

## Why it matters

This requirement was added specifically because of real-world security vulnerabilities. When different parsers handle `Header : value` vs `Header: value` differently, attackers can craft requests that are interpreted as having different headers by different components.

The `Transfer-Encoding` smuggling variant (`Transfer-Encoding : chunked`) exploits exactly this.

## Sources

- [RFC 9112 Section 5 — Field Syntax](https://www.rfc-editor.org/rfc/rfc9112#section-5)
- [RFC 9110 Section 16.3.1 — Request Smuggling](https://www.rfc-editor.org/rfc/rfc9110#section-16.3.1)
