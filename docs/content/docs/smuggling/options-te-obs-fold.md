---
title: "OPTIONS-TE-OBS-FOLD"
description: "SMUG-OPTIONS-TE-OBS-FOLD test documentation"
weight: 1
---

| | |
|---|---|
| **Test ID** | `SMUG-OPTIONS-TE-OBS-FOLD` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §5.2](https://www.rfc-editor.org/rfc/rfc9112#section-5.2) |
| **Requirement** | MUST |
| **Expected** | `400` or `2xx` + close |

## What it sends

A two-step sequence: `OPTIONS` with folded `Transfer-Encoding` plus `Content-Length`, then a follow-up `GET`.

```http
OPTIONS / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding:\r\n
 chunked\r\n
Content-Length: 5\r\n
\r\n
hello

GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

> "A server that receives an obs-fold in a request message ... MUST either reject the message by sending a 400 (Bad Request) ... or replace each received obs-fold with one or more SP octets." -- RFC 9112 Section 5.2

If unfolded to `Transfer-Encoding: chunked` while `Content-Length` is also present, connection safety rules still apply.

## Partial Coverage Note

Existing test `SMUG-TE-OBS-FOLD` already covers this grammar issue in a single request. This variant exercises the `OPTIONS` method path and verifies follow-up connection handling.

## Why it matters

Method-specific parser branches can bypass generic TE validation. This is the class highlighted by recent OPTIONS+obs-fold smuggling disclosures.

## Sources

- [RFC 9112 §5.2](https://www.rfc-editor.org/rfc/rfc9112#section-5.2)
