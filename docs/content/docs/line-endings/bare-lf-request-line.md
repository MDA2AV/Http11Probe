---
title: "BARE-LF-REQUEST-LINE"
weight: 1
---

| | |
|---|---|
| **Test ID** | `RFC9112-2.2-BARE-LF-REQUEST-LINE` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) |
| **Requirement** | MAY |
| **Expected** | `400` or close |

## What it sends

A `GET / HTTP/1.1` request where the request-line is terminated with `\n` (bare LF) instead of `\r\n` (CRLF).

## What the RFC says

> "Although the line terminator for the start-line and fields is the sequence CRLF, a recipient **MAY** recognize a single LF as a line terminator and ignore any preceding CR."

The sender MUST NOT generate bare LF, but the recipient is explicitly given permission to accept it. This is a MAY — not a MUST.

## Why it matters

Bare LF acceptance is a common source of parser disagreements. If a front-end proxy accepts bare LF as a line terminator but a back-end server does not (or vice versa), the two may disagree on request boundaries — a prerequisite for request smuggling.

Strict rejection is the safer choice, which is why Http11Probe scores it as a pass when the server rejects.

## Sources

- [RFC 9112 Section 2.2 — Message Parsing](https://www.rfc-editor.org/rfc/rfc9112#section-2.2)
- [RFC 9110 Section 16.3 — Intermediary Encapsulation Attacks](https://www.rfc-editor.org/rfc/rfc9110#section-16.3)
