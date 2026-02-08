---
title: "TE-TRAILING-COMMA"
description: "TE-TRAILING-COMMA test documentation"
weight: 51
---

| | |
|---|---|
| **Test ID** | `SMUG-TE-TRAILING-COMMA` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 §5.6.1](https://www.rfc-editor.org/rfc/rfc9110#section-5.6.1) |
| **Requirement** | SHOULD |
| **Expected** | `400` or `2xx` |

## What it sends

Transfer-Encoding with a trailing comma after `chunked`, alongside Content-Length.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked,\r\n
Content-Length: 5\r\n
\r\n
hello
```

The `Transfer-Encoding` value is `chunked,` — a trailing comma produces an empty list element after `chunked`.


## What the RFC says

> "A recipient MUST parse and ignore a reasonable number of empty list elements: enough to handle common mistakes by senders that merge values, but not so much that they could be used as a denial-of-service mechanism." -- RFC 9110 Section 5.6.1

The trailing comma creates an empty list element. Per §5.6.1, the server should strip the empty element and see just `chunked`. However, some parsers reject the value because the trailing comma makes it syntactically unusual, while others strip it and process normally.

## Pass / Warn

The RFC requires recipients to ignore empty list elements, so stripping the trailing comma and processing `chunked` is RFC-compliant. However, rejecting the value is the safer choice since the trailing comma creates ambiguity. Both behaviors are valid, but rejection is preferred.

**Pass:** Server rejects with `400` (strict, safe).
**Warn:** Server accepts and responds `2xx` (RFC-valid per Section 5.6.1 empty-element handling).

## Why it matters

When Content-Length is also present, parser disagreement on whether `chunked,` is valid Transfer-Encoding creates a CL/TE desync. A parser that rejects the trailing comma falls back to Content-Length framing, while a parser that strips the empty element uses chunked framing. This is the mirror of the leading-comma test (SMUG-TE-LEADING-COMMA) and exploits the same §5.6.1 ambiguity from the opposite direction.

## Deep Analysis

### ABNF

```
Transfer-Encoding = #transfer-coding       ; RFC 9112 §6.1
transfer-coding   = token                  ; RFC 9110 §10.1.4
token             = 1*tchar                ; RFC 9110 §5.6.2
```

The `#` list rule allows comma-separated elements. The value `chunked,` consists of the element `chunked` followed by a comma and an implicit empty element. Per the `#` rule semantics in RFC 9110 section 5.6.1, empty list elements (produced by leading, trailing, or consecutive commas) should be ignored by recipients.

### RFC Evidence

> "A recipient MUST parse and ignore a reasonable number of empty list elements: enough to handle common mistakes by senders that merge values, but not so much that they could be used as a denial-of-service mechanism." -- RFC 9110 §5.6.1

> "A recipient MUST be able to parse the chunked transfer coding (Section 7.1) because it plays a crucial role in framing messages when the content size is not known in advance." -- RFC 9112 §6.1

> "If a message is received with both a Transfer-Encoding and a Content-Length header field, the Transfer-Encoding overrides the Content-Length. Such a message might indicate an attempt to perform request smuggling or response splitting and ought to be handled as an error." -- RFC 9112 §6.3

### Chain of Reasoning

1. The test sends `Transfer-Encoding: chunked,` alongside `Content-Length: 5`.
2. The trailing comma after `chunked` produces an empty list element. Per RFC 9110 section 5.6.1, recipients MUST parse and ignore a reasonable number of empty list elements.
3. After stripping the empty element, the effective value is `Transfer-Encoding: chunked` -- a valid transfer coding that the server MUST be able to parse per RFC 9112 section 6.1.
4. However, some parsers treat the trailing comma differently. A parser that does not strip empty elements may see `chunked,` as a single token that does not match any registered transfer coding. It would then fall back to Content-Length.
5. Other parsers may interpret the trailing comma as indicating an additional (empty or missing) transfer coding after `chunked`, possibly treating the entire value as malformed.
6. The dual presence of Transfer-Encoding and Content-Length triggers RFC 9112 section 6.3, which states the message "ought to be handled as an error."
7. This test is the mirror image of SMUG-TE-LEADING-COMMA, exploiting the same empty-element ambiguity from the trailing direction.

### Scored / SHOULD Justification

This test is scored as **SHOULD** reject because, while RFC 9110 section 5.6.1 requires recipients to handle empty list elements (making `2xx` defensible), the trailing comma creates ambiguity in how different parsers interpret the Transfer-Encoding value. Rejection is the safer behavior, especially given the CL/TE dual-header scenario.

- **Pass (400):** Strict rejection -- the server refuses to process the syntactically unusual trailing comma.
- **Warn (2xx):** RFC-compliant per section 5.6.1 -- the server correctly stripped the empty list element and processed `chunked`.

### Smuggling Attack Scenarios

- **Trailing-Comma Fallback Desync:** A front-end proxy that does not strip empty list elements sees `chunked,` as an unrecognized transfer coding and falls back to Content-Length framing. A back-end that correctly strips the trailing empty element sees `chunked` and uses chunked framing. The attacker can embed a second request in the chunked body that the front-end never parses.
- **Comma Suffix as WAF Bypass:** WAFs and intrusion detection systems that check for exact `Transfer-Encoding: chunked` will not match `Transfer-Encoding: chunked,`. The trailing comma bypasses the security check, but the origin server normalizes the value and processes chunked encoding, allowing smuggled payloads to reach the back-end.
- **Inconsistent Element Counting:** Some parsers count the number of transfer codings by splitting on commas. With `chunked,`, they count two elements: `chunked` and an empty string. A parser that requires exactly one transfer coding may reject the header, while a parser that ignores empty elements sees one coding. This counting disagreement can cause one side of a proxy chain to reject and the other to accept, creating connection state mismatches.

## Sources

- [RFC 9110 §5.6.1 -- Lists](https://www.rfc-editor.org/rfc/rfc9110#section-5.6.1)
- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
