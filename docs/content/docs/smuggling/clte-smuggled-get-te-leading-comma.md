---
title: "CLTE-SMUGGLED-GET-TE-LEADING-COMMA"
description: "CLTE-SMUGGLED-GET-TE-LEADING-COMMA sequence test documentation"
weight: 20
---

| | |
|---|---|
| **Test ID** | `SMUG-CLTE-SMUGGLED-GET-TE-LEADING-COMMA` |
| **Category** | Smuggling |
| **Type** | Sequence (single send) |
| **Scored** | Yes |
| **RFC** | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **RFC Level** | MUST |
| **Expected** | `400`, or connection close |

## What it does

This is a variant of `SMUG-CLTE-SMUGGLED-GET` where the `Transfer-Encoding` field value contains a **leading comma** (`", chunked"`).

Some parsers ignore empty list elements and treat this as equivalent to `chunked`; others reject it or ignore the header. That discrepancy can enable request smuggling in multi-hop deployments.

## What it sends

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 45\r\n
Transfer-Encoding: , chunked\r\n
\r\n
0\r\n
\r\n
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

> "A server MAY reject a request that contains both Content-Length and Transfer-Encoding... **Regardless, the server MUST close the connection after responding to such a request.**" — RFC 9112 §6.1

## Why it matters

Comma-list parsing differences are a recurring source of TE normalization bugs. If one hop sees TE as valid and another does not, the embedded `GET` can be interpreted as a separate request by one side.

## Verdicts

- **Pass**: The server rejects with `400`, or closes the connection.
- **Fail**: Multiple HTTP responses are observed (embedded GET likely executed).
- **Fail**: The server accepts and keeps the connection open (MUST-close violated), even if no extra response is observed.

## Sources

- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
