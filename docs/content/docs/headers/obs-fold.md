---
title: "OBS-FOLD"
weight: 2
---

| | |
|---|---|
| **Test ID** | `RFC9112-5.1-OBS-FOLD` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 5.1](https://www.rfc-editor.org/rfc/rfc9112#section-5.1) |
| **Requirement** | MUST |
| **Expected** | `400` |

## What it sends

A request with an obsolete line-folded header value — a continuation line that starts with whitespace:

```http
GET / HTTP/1.1\r\n
Host: localhost\r\n
X-Test: value1\r\n
 continuation\r\n
\r\n
```

The ` continuation` line (starting with a space) is "obs-fold" — it was valid in older HTTP but is deprecated.

## What the RFC says

> "A server that receives an obs-fold in a request message that is not within a message/http container **MUST** either reject the message by sending a 400 (Bad Request), preferably with a representation explaining that obsolete line folding is unacceptable, or replace each received obs-fold with one or more SP octets prior to interpreting the field value or forwarding the message downstream."

This is a MUST with two alternatives: send 400 or silently fix it. Http11Probe scores rejection (400) as a pass because it's the stricter option.

## Why it matters

Obs-fold creates ambiguity: is the continuation line part of the previous header's value, or a new header/request? Different parsers may disagree, creating a smuggling vector.

## Sources

- [RFC 9112 Section 5.1 — Obsolete Line Folding](https://www.rfc-editor.org/rfc/rfc9112#section-5.1)
