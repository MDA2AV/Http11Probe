---
title: "TE-DOUBLE-CHUNKED"
description: "TE-DOUBLE-CHUNKED test documentation"
weight: 13
---

| | |
|---|---|
| **Test ID** | `SMUG-TE-DOUBLE-CHUNKED` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **Requirement** | SHOULD |
| **Expected** | `400` or `2xx` |

## What it sends

`Transfer-Encoding: chunked, chunked` — duplicate `chunked` encoding with a Content-Length header also present.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked, chunked\r\n
Content-Length: 5\r\n
\r\n
hello
```


## What the RFC says

> "A sender MUST NOT apply the chunked transfer coding more than once to a message body." — RFC 9112 Section 6.1

> "If any transfer coding other than chunked is applied to a request's content, the sender MUST apply chunked as the final transfer coding." — RFC 9112 Section 6.1

The sender violates the MUST NOT rule by listing `chunked` twice. However, the server's obligation is to parse the Transfer-Encoding it receives, and a server that sees `chunked, chunked` might reasonably process it as a single `chunked` application or reject it.

## Pass / Warn

While the sender clearly violates RFC 9112 Section 6.1 by applying chunked twice, the RFC does not specify a mandatory server response for this case. The server may reject with `400` (strict) or deduplicate and process normally (lenient). Both behaviors are defensible since the MUST NOT applies to senders, not to how receivers handle the violation.

**Pass:** Server rejects with `400` (strict, safe).
**Warn:** Server accepts and responds `2xx` (lenient, deduplicates chunked).

## Deep Analysis

### ABNF

The Transfer-Encoding header uses the list syntax (`#rule`):

```
Transfer-Encoding = #transfer-coding       ; RFC 9112 §6.1
transfer-coding   = token                  ; RFC 9110 §10.1.4
token             = 1*tchar                ; RFC 9110 §5.6.2
tchar             = "!" / "#" / "$" / "%" / "&" / "'" / "*"
                    / "+" / "-" / "." / "^" / "_" / "`" / "|" / "~"
                    / DIGIT / ALPHA
```

The `#` construct means a comma-separated list of transfer-coding tokens. The value `chunked, chunked` is a syntactically valid list of two elements that both resolve to the same transfer coding.

### RFC Evidence

> "A sender MUST NOT apply the chunked transfer coding more than once to a message body (i.e., chunking an already chunked message is not allowed)." -- RFC 9112 §6.1

> "If any transfer coding other than chunked is applied to a request's content, the sender MUST apply chunked as the final transfer coding to ensure that the message is properly framed." -- RFC 9112 §6.1

> "If a message is received with both a Transfer-Encoding and a Content-Length header field, the Transfer-Encoding overrides the Content-Length. Such a message might indicate an attempt to perform request smuggling or response splitting and ought to be handled as an error." -- RFC 9112 §6.3

### Chain of Reasoning

1. The test sends `Transfer-Encoding: chunked, chunked` alongside `Content-Length: 5`.
2. RFC 9112 section 6.1 states that a sender MUST NOT apply the chunked transfer coding more than once. The value `chunked, chunked` declares chunked applied twice, directly violating this sender-side requirement.
3. However, this MUST NOT is directed at the **sender**, not the receiver. The RFC does not prescribe a specific receiver behavior when encountering a duplicated chunked coding.
4. A strict server may reject with `400` because the request is malformed per the sender rules. A lenient server may deduplicate and process a single chunked decoding, which is also defensible.
5. The presence of `Content-Length: 5` alongside Transfer-Encoding creates the dual-header smuggling setup described in RFC 9112 section 6.3.
6. Some servers may attempt to apply chunked decoding twice (chunked-within-chunked), leading to unpredictable behavior and potential parser confusion.

### Scored / SHOULD Justification

This test is scored at the **SHOULD** level because the MUST NOT in RFC 9112 section 6.1 applies to the sender, not to how receivers should handle the violation. No MUST-level requirement dictates a specific server response when `chunked` appears twice, but rejection is the safer behavior.

- **Pass (400):** The server rejects the malformed request, which is the safest response.
- **Warn (2xx):** The server deduplicates the encoding and processes normally, which is lenient but not a specification violation.

### Smuggling Attack Scenarios

- **Double-Chunked Confusion:** A front-end proxy sees `chunked, chunked` and applies chunked decoding once, then forwards the partially-decoded body. The back-end sees the forwarded data and may attempt to apply chunked decoding again, interpreting attacker-controlled data as chunk boundaries and enabling request smuggling.
- **Deduplication Disagreement:** A front-end deduplicates the list to a single `chunked` and uses chunked framing. A back-end that does not recognize the duplicate falls back to Content-Length. The parsers disagree on body boundaries, allowing the attacker to inject a second request.
- **Parser State Corruption:** Some HTTP libraries track encoding layers in a stack. Pushing `chunked` twice may corrupt internal state, causing the parser to miscount body boundaries or skip the terminating zero-length chunk.

## Sources

- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
- [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
