---
title: "TE-CASE-MISMATCH"
description: "TE-CASE-MISMATCH test documentation"
weight: 14
---

| | |
|---|---|
| **Test ID** | `SMUG-TE-CASE-MISMATCH` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **Requirement** | SHOULD |
| **Expected** | `400` or `2xx` |

## What it sends

`Transfer-Encoding: Chunked` — capital `C` instead of lowercase.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: Chunked\r\n
Content-Length: 5\r\n
\r\n
hello
```

Note `Chunked` with a capital C instead of `chunked`.


## What the RFC says

> "All transfer-coding names are case-insensitive and ought to be registered within the HTTP Transfer Coding registry." — RFC 9112 Section 7

> "A recipient MUST be able to parse the chunked transfer coding (Section 7.1) because it plays a crucial role in framing messages when the content size is not known in advance." — RFC 9112 Section 6.1

Recognizing `Chunked` as `chunked` is correct, RFC-compliant behavior. A server that treats transfer coding names as case-sensitive may reject or misinterpret the header, creating a potential CL/TE desync when Content-Length is also present.

**Pass:** Server rejects with `400` (strict, safe).
**Warn:** Server accepts and responds `2xx` (RFC-valid case-insensitive matching).

## Why it matters

Because transfer coding names are explicitly case-insensitive, both `400` and `2xx` are defensible responses. A server that rejects the request is being overly strict but safe. A server that accepts it is following the RFC. However, rejecting is preferred because case-insensitive matching combined with Content-Length creates a smuggling risk.

## Deep Analysis

### ABNF

The Transfer-Encoding header and token grammar are defined as follows:

```
Transfer-Encoding = #transfer-coding       ; RFC 9112 §6.1
transfer-coding   = token                  ; RFC 9110 §10.1.4
token             = 1*tchar                ; RFC 9110 §5.6.2
tchar             = "!" / "#" / "$" / "%" / "&" / "'" / "*"
                    / "+" / "-" / "." / "^" / "_" / "`" / "|" / "~"
                    / DIGIT / ALPHA
```

### RFC Evidence

> "All transfer-coding names are case-insensitive and ought to be registered within the HTTP Transfer Coding registry." -- RFC 9112 §7

> "A recipient MUST be able to parse the chunked transfer coding (Section 7.1) because it plays a crucial role in framing messages when the content size is not known in advance." -- RFC 9112 §6.1

> "A server MAY reject a request that contains both Content-Length and Transfer-Encoding or process such a request in accordance with the Transfer-Encoding alone. Regardless, the server MUST close the connection after responding to such a request to avoid the potential attacks." -- RFC 9112 §6.1

### Chain of Reasoning

1. The test sends `Transfer-Encoding: Chunked` with a capital `C` instead of the lowercase `chunked`.
2. RFC 9112 section 7 explicitly states that all transfer-coding names are **case-insensitive**. This means `Chunked`, `CHUNKED`, and `chunked` are all equivalent per the specification.
3. RFC 9112 section 6.1 requires that a recipient MUST be able to parse the chunked transfer coding. A case-insensitive comparison satisfies this requirement regardless of capitalization.
4. The request also includes `Content-Length: 5`, creating a CL/TE dual-header scenario. Under RFC 9112 section 6.1, the server MAY reject such a request or process it using Transfer-Encoding alone, but MUST close the connection afterward.
5. The smuggling risk arises when a front-end parser performs case-sensitive matching and does not recognize `Chunked` as a valid transfer coding, falling back to Content-Length framing. If a back-end parser correctly performs case-insensitive matching and uses chunked framing, the two parsers disagree on where the message body ends -- enabling request smuggling.

### Scored / SHOULD Justification

This test is scored as **SHOULD** reject because, while the RFC explicitly declares transfer-coding names case-insensitive, the combination with Content-Length creates a smuggling risk. A server that rejects `Chunked` with `400` is being strict but safe. A server that accepts it and processes chunked encoding is following the RFC correctly. Neither behavior violates a MUST-level requirement, but rejection is preferred.

- **Pass (400):** Strict rejection prevents any parser disagreement with intermediaries.
- **Warn (2xx):** RFC-compliant case-insensitive matching; the server correctly recognized the encoding.

### Smuggling Attack Scenarios

- **CL/TE Desync via Case Sensitivity:** An attacker sends `Transfer-Encoding: Chunked` with `Content-Length: 5`. A case-sensitive front-end proxy does not recognize `Chunked` and routes based on Content-Length. The back-end server performs case-insensitive matching, switches to chunked framing, and interprets the body differently. The attacker can embed a second request inside the chunked body that the front-end never sees.
- **Reverse Proxy Bypass:** Some WAFs or reverse proxies check for `Transfer-Encoding: chunked` using exact string matching. Sending `Chunked` or `CHUNKED` bypasses the check while the origin server still processes chunked encoding, allowing smuggled payloads to reach the back-end unfiltered.

## Sources

- [RFC 9112 §7](https://www.rfc-editor.org/rfc/rfc9112#section-7)
- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
