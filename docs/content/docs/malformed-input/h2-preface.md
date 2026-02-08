---
title: "H2-PREFACE"
description: "H2-PREFACE test documentation"
weight: 17
---

| | |
|---|---|
| **Test ID** | `MAL-H2-PREFACE` |
| **Category** | Malformed Input |
| **Expected** | `400`/`505`, close, or timeout |

## What it sends

The HTTP/2 connection preface (`PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n`) to an HTTP/1.1 server.

## Why it matters

An HTTP/1.1-only server receiving the H2 preface should recognize it is not a valid HTTP/1.1 request. Parsing it as HTTP/1.1 could lead to unexpected behavior. The server should reject with 400 or 505, close the connection, or timeout.

## Sources

- RFC 9113 Section 3.4 — HTTP/2 connection preface
