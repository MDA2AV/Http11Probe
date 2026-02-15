---
title: "TECL-PIPELINE"
description: "TECL-PIPELINE test documentation"
weight: 9
---

| | |
|---|---|
| **Test ID** | `SMUG-TECL-PIPELINE` |
| **Category** | Smuggling |
| **Scored** | Yes |
| **RFC** | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **RFC Level** | MAY |
| **Expected** | `400` or close preferred; `2xx` acceptable |

## What it sends

The reverse of CL.TE — a request with `Transfer-Encoding: chunked` listed first, plus a conflicting `Content-Length`.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
Content-Length: 30\r\n
\r\n
0\r\n
\r\n
```

A TE parser sees the `0` chunk as end-of-body (5 bytes consumed). A CL parser tries to read 30 bytes, consuming far more than the chunked body. The disagreement is what enables the TE.CL smuggling variant.

## What the RFC says

> "A server MAY reject a request that contains both Content-Length and Transfer-Encoding or process such a request in accordance with the Transfer-Encoding alone. Regardless, the server MUST close the connection after responding to such a request to avoid the potential attacks." — RFC 9112 §6.1

> "If a message is received with both a Transfer-Encoding and a Content-Length header field, the Transfer-Encoding overrides the Content-Length. Such a message might indicate an attempt to perform request smuggling (Section 11.2) or response splitting (Section 11.1) and ought to be handled as an error." — RFC 9112 §6.3

## Why it matters

Together with CL.TE, this covers both orderings of the dual-header conflict. A proxy chain where one hop prefers TE and the other prefers CL is vulnerable to this variant. Rejecting the request outright is the safest defense.

## Verdicts

- **Pass** — Server rejects with `400` or closes the connection (safest behavior)
- **Warn** — Server responds with `2xx` (RFC-compliant if it processes via TE and closes the connection, but the lenient path)
- **Fail** — Any other response

## Scored / Unscored Justification

This test is **scored**. Although the RFC uses MAY language, there is a clear preferred outcome: rejecting the ambiguous request is safer than accepting it. The reasoning mirrors CLTE-PIPELINE exactly.

## Sources

- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
- [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
- [PortSwigger — HTTP Request Smuggling](https://portswigger.net/web-security/request-smuggling)
