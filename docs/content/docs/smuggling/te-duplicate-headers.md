---
title: "TE-DUPLICATE-HEADERS"
description: "TE-DUPLICATE-HEADERS test documentation"
weight: 24
---

| | |
|---|---|
| **Test ID** | `SMUG-TE-DUPLICATE-HEADERS` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 Section 6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

Two TE headers (`chunked` and `identity`) plus Content-Length.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
Transfer-Encoding: identity\r\n
Content-Length: 5\r\n
\r\n
hello
```

Two separate `Transfer-Encoding` headers with different values, plus a `Content-Length`.


## What the RFC says

> "If a message is received with both a Transfer-Encoding and a Content-Length header field, the Transfer-Encoding overrides the Content-Length. Such a message might indicate an attempt to perform request smuggling or response splitting and ought to be handled as an error." -- RFC 9112 Section 6.3

> "A server MAY reject a request that contains both Content-Length and Transfer-Encoding or process such a request in accordance with the Transfer-Encoding alone. Regardless, the server MUST close the connection after responding to such a request to avoid the potential attacks." -- RFC 9112 Section 6.1

Two separate `Transfer-Encoding` headers with conflicting values (`chunked` and `identity`) create an additional layer of ambiguity -- different servers may pick different TE header values to determine framing. Combined with a Content-Length header, this is a textbook smuggling setup.

## Why it matters

When two TE headers carry different values, one parser may use `chunked` framing while another falls through to `identity` (and then to Content-Length). This disagreement on body length is the core of CL/TE request smuggling.

## Deep Analysis

### ABNF

```
Transfer-Encoding = #transfer-coding       ; RFC 9112 §6.1
transfer-coding   = token                  ; RFC 9110 §10.1.4
token             = 1*tchar                ; RFC 9110 §5.6.2
tchar             = "!" / "#" / "$" / "%" / "&" / "'" / "*"
                    / "+" / "-" / "." / "^" / "_" / "`" / "|" / "~"
                    / DIGIT / ALPHA
```

The `#` rule means Transfer-Encoding is a comma-separated list. When multiple header lines share the same field name, they are semantically equivalent to a single line with values joined by commas. Two separate `Transfer-Encoding` lines with `chunked` and `identity` are equivalent to `Transfer-Encoding: chunked, identity`.

### RFC Evidence

> "If a message is received with both a Transfer-Encoding and a Content-Length header field, the Transfer-Encoding overrides the Content-Length. Such a message might indicate an attempt to perform request smuggling or response splitting and ought to be handled as an error." -- RFC 9112 §6.3

> "A server MAY reject a request that contains both Content-Length and Transfer-Encoding or process such a request in accordance with the Transfer-Encoding alone. Regardless, the server MUST close the connection after responding to such a request to avoid the potential attacks." -- RFC 9112 §6.1

> "A server that receives a request message with a transfer coding it does not understand SHOULD respond with 501 (Not Implemented)." -- RFC 9112 §6.1

### Chain of Reasoning

1. The test sends two separate `Transfer-Encoding` header lines: one with `chunked` and one with `identity`. A `Content-Length: 5` header is also present.
2. Per HTTP semantics, multiple header lines with the same field name are combined as a comma-separated list. The effective value is `Transfer-Encoding: chunked, identity`.
3. The `identity` transfer coding was removed from the registry in RFC 7230 and is absent from RFC 9112. It is an unrecognized transfer coding.
4. With `chunked` not as the final encoding (it appears before `identity`), RFC 9112 section 6.3 requires: "the server MUST respond with the 400 (Bad Request) status code and then close the connection."
5. Additionally, the presence of both Transfer-Encoding and Content-Length triggers the smuggling warning in RFC 9112 section 6.3, which states such a message "ought to be handled as an error."
6. The combination of an unrecognized coding, chunked not being final, and a conflicting Content-Length makes this a triple-layered ambiguity.

### Scored / Unscored Justification

This test is **scored** (MUST reject). Two separate Transfer-Encoding headers with conflicting values create a scenario where `chunked` is not the final encoding. RFC 9112 section 6.3 uses MUST-level language requiring a `400` response when chunked is not final. The additional presence of Content-Length reinforces that this ought to be treated as an error.

- **Pass (400 or close):** The server correctly rejects the ambiguous framing.
- **Fail (2xx):** The server accepted a request with irreconcilable framing signals, violating MUST-level requirements.

### Smuggling Attack Scenarios

- **Header Precedence Disagreement:** A front-end proxy may process only the first `Transfer-Encoding: chunked` header, using chunked framing. A back-end may combine both headers and see `chunked, identity`, treating it as an unrecognized encoding and falling back to Content-Length. This framing disagreement enables CL/TE desync.
- **Last-Header-Wins vs. First-Header-Wins:** Different HTTP implementations follow different strategies when encountering duplicate headers. A front-end using "first wins" sees `chunked`; a back-end using "last wins" sees `identity` (an unrecognized coding) and may fall back to Content-Length. The attacker controls which framing each parser uses.
- **Selective Header Forwarding:** Some proxies forward only the first instance of a repeated header. If the front-end forwards only `Transfer-Encoding: chunked` but the back-end originally received both, the proxy's normalization creates a mismatch with the original Content-Length, enabling body-boundary manipulation.

## Sources

- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
- [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
