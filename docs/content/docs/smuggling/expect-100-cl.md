---
title: "EXPECT-100-CL"
description: "EXPECT-100-CL test documentation"
weight: 33
---

| | |
|---|---|
| **Test ID** | `SMUG-EXPECT-100-CL` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 Section 10.1.1](https://www.rfc-editor.org/rfc/rfc9110#section-10.1.1) |
| **Requirement** | Unscored |
| **Expected** | `400` or `2xx` |

## What it sends

POST with `Content-Length: 5` and `Expect: 100-continue`, body included immediately.

## What the RFC says

> "A server that receives a 100-continue expectation in an HTTP/1.1 request MUST send either a 100 (Continue) interim response... or a final status code." — RFC 9110 §10.1.1

## Why it matters

This is unscored. It tests whether the server handles Expect: 100-continue correctly when the body is sent immediately. Both accepting and rejecting are valid depending on implementation.

## Sources

- [RFC 9110 Section 10.1.1](https://www.rfc-editor.org/rfc/rfc9110#section-10.1.1)
