---
title: "TE-LEADING-COMMA"
description: "TE-LEADING-COMMA test documentation"
weight: 23
---

| | |
|---|---|
| **Test ID** | `SMUG-TE-LEADING-COMMA` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 §5.6.1](https://www.rfc-editor.org/rfc/rfc9110#section-5.6.1) |
| **Requirement** | SHOULD |
| **Expected** | `400` or `2xx` |

## What it sends

`Transfer-Encoding: , chunked` — leading comma before `chunked`.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: , chunked\r\n
Content-Length: 5\r\n
\r\n
hello
```

The Transfer-Encoding value starts with a leading comma before `chunked`.


## What the RFC says

> "A recipient MUST parse and ignore a reasonable number of empty list elements: enough to handle common mistakes by senders that merge values, but not so much that they could be used as a denial-of-service mechanism." -- RFC 9110 Section 5.6.1

The leading comma produces an empty list element before `chunked`. Since RFC 9110 Section 5.6.1 requires recipients to parse and ignore empty list elements, a server that strips the empty element and processes `chunked` normally is RFC-compliant.

## Pass / Warn

The RFC requires recipients to ignore empty list elements, so stripping the leading comma and processing `chunked` is RFC-compliant. However, rejecting the value is the safer choice since the leading comma creates ambiguity. Both behaviors are valid, but rejection is preferred.

**Pass:** Server rejects with `400` (strict, safe).
**Warn:** Server accepts and responds `2xx` (RFC-valid per Section 5.6.1 empty-element handling).

## Why it matters

Some parsers strip leading commas and see "chunked", while others reject the value entirely. This discrepancy enables smuggling when front-end and back-end parsers disagree on whether Transfer-Encoding is valid.

## Deep Analysis

### ABNF

```
Transfer-Encoding = #transfer-coding       ; RFC 9112 §6.1
transfer-coding   = token                  ; RFC 9110 §10.1.4
token             = 1*tchar                ; RFC 9110 §5.6.2
OWS               = *( SP / HTAB )         ; RFC 9110 §5.6.3
```

The `#` rule (list syntax) is defined in RFC 9110 section 5.6.1. A `#` list allows comma-separated elements with optional whitespace. The value `, chunked` consists of an empty element (before the comma), a comma delimiter, and the element `chunked`. The empty element is what the RFC requires recipients to parse and ignore.

### RFC Evidence

> "A recipient MUST parse and ignore a reasonable number of empty list elements: enough to handle common mistakes by senders that merge values, but not so much that they could be used as a denial-of-service mechanism." -- RFC 9110 §5.6.1

> "A recipient MUST be able to parse the chunked transfer coding (Section 7.1) because it plays a crucial role in framing messages when the content size is not known in advance." -- RFC 9112 §6.1

> "A server MAY reject a request that contains both Content-Length and Transfer-Encoding or process such a request in accordance with the Transfer-Encoding alone. Regardless, the server MUST close the connection after responding to such a request to avoid the potential attacks." -- RFC 9112 §6.1

### Chain of Reasoning

1. The test sends `Transfer-Encoding: , chunked` alongside `Content-Length: 5`.
2. Per the `#` list syntax in RFC 9110 section 5.6.1, the leading comma produces an empty list element before the actual `chunked` element.
3. RFC 9110 section 5.6.1 explicitly requires recipients to "parse and ignore a reasonable number of empty list elements." This means a fully compliant server should strip the empty element and see `Transfer-Encoding: chunked`.
4. However, some parsers do not implement empty-element stripping for Transfer-Encoding specifically. They may see `, chunked` as a single unrecognized token (including the comma and space) and reject it or fall back to Content-Length.
5. A server that strips the empty element and processes `chunked` is following RFC 9110 section 5.6.1. A server that rejects with `400` is being strict but safe.
6. The dual presence of Transfer-Encoding and Content-Length triggers the smuggling warning in RFC 9112 section 6.3, regardless of how the leading comma is handled.

### Scored / SHOULD Justification

This test is scored as **SHOULD** reject because, while RFC 9110 section 5.6.1 explicitly requires recipients to handle empty list elements, the leading comma creates smuggling risk. A server that ignores the empty element and processes `chunked` is RFC-compliant. A server that rejects the request is being defensive. Neither response violates a MUST-level requirement, but rejection is the safer behavior.

- **Pass (400):** Strict rejection -- the server refuses to process the syntactically unusual value.
- **Warn (2xx):** RFC-compliant per section 5.6.1 -- the server correctly stripped the empty list element and processed `chunked`.

### Smuggling Attack Scenarios

- **Empty-Element Stripping Disagreement:** A front-end proxy does not strip empty list elements and sees `, chunked` as an unrecognized transfer coding, falling back to Content-Length framing. A back-end correctly strips the empty element, sees `chunked`, and uses chunked framing. The desync between Content-Length and chunked framing lets the attacker embed a second request inside the chunked body.
- **Comma Prefix as WAF Bypass:** Web Application Firewalls that check for `Transfer-Encoding: chunked` using exact string matching will not match `, chunked`. The request passes through the WAF unexamined, but the origin server strips the leading comma and processes chunked encoding normally. This allows the attacker to smuggle requests past the WAF.
- **Normalization Cascade:** In a multi-hop proxy chain, the first proxy may strip the leading comma and forward `Transfer-Encoding: chunked`. The second proxy now sees a clean header and processes normally. But if the first proxy used Content-Length for its own framing decision before normalizing, the body it forwarded may not match what the second proxy expects from chunked framing.

## Sources

- [RFC 9110 §5.6.1 -- Lists](https://www.rfc-editor.org/rfc/rfc9110#section-5.6.1)
- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
