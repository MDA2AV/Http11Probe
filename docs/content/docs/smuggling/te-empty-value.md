---
title: "TE-EMPTY-VALUE"
description: "TE-EMPTY-VALUE test documentation"
weight: 22
---

| | |
|---|---|
| **Test ID** | `SMUG-TE-EMPTY-VALUE` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 Section 6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

`Transfer-Encoding: ` (empty value) with `Content-Length: 5`.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: \r\n
Content-Length: 5\r\n
\r\n
hello
```

The `Transfer-Encoding` header has an empty value.


## What the RFC says

> "A server that receives a request message with a transfer coding it does not understand SHOULD respond with 501 (Not Implemented)." -- RFC 9112 Section 6.1

> "If a message is received with both a Transfer-Encoding and a Content-Length header field, the Transfer-Encoding overrides the Content-Length. Such a message might indicate an attempt to perform request smuggling or response splitting and ought to be handled as an error." -- RFC 9112 Section 6.3

An empty Transfer-Encoding value contains no valid transfer coding name. The header is syntactically present but semantically empty, creating a framing ambiguity: should the server treat Transfer-Encoding as present (overriding Content-Length) or absent (falling back to Content-Length)?

## Why it matters

If a front-end sees Transfer-Encoding as present and ignores Content-Length, but a back-end sees an empty value and falls back to Content-Length, they will disagree on body framing -- a direct smuggling vector.

## Deep Analysis

### ABNF

```
Transfer-Encoding = #transfer-coding       ; RFC 9112 §6.1
transfer-coding   = token                  ; RFC 9110 §10.1.4
token             = 1*tchar                ; RFC 9110 §5.6.2
field-value       = *field-content          ; RFC 9110 §5.5
```

The `token` rule requires **at least one** `tchar` character (`1*tchar`). An empty string contains zero `tchar` characters and therefore does not match the `token` production. The `#` list rule (`#transfer-coding`) permits empty list elements (which should be ignored), but an entirely empty value contains no valid elements at all.

### RFC Evidence

> "A server that receives a request message with a transfer coding it does not understand SHOULD respond with 501 (Not Implemented)." -- RFC 9112 §6.1

> "If a message is received with both a Transfer-Encoding and a Content-Length header field, the Transfer-Encoding overrides the Content-Length. Such a message might indicate an attempt to perform request smuggling or response splitting and ought to be handled as an error." -- RFC 9112 §6.3

> "A server MAY reject a request that contains both Content-Length and Transfer-Encoding or process such a request in accordance with the Transfer-Encoding alone. Regardless, the server MUST close the connection after responding to such a request to avoid the potential attacks." -- RFC 9112 §6.1

### Chain of Reasoning

1. The test sends `Transfer-Encoding: ` (empty value) alongside `Content-Length: 5`.
2. An empty Transfer-Encoding value contains no valid `token` per the ABNF. The header is syntactically present but semantically void -- it declares a transfer coding without naming one.
3. The critical ambiguity: is Transfer-Encoding "present" or "absent"? If present, RFC 9112 section 6.3 says it overrides Content-Length. If absent (because the value is empty), Content-Length governs framing.
4. RFC 9112 section 6.3 states that receiving both Transfer-Encoding and Content-Length "ought to be handled as an error." The Transfer-Encoding header is syntactically present regardless of whether its value is empty.
5. RFC 9112 section 6.1 further requires the server to close the connection after responding to a request with both headers, regardless of whether it rejects or processes the request.
6. The empty value is not a recognized transfer coding, so the SHOULD-level guidance to respond with `501 (Not Implemented)` also applies.

### Scored / Unscored Justification

This test is **scored** (MUST reject). The Transfer-Encoding header is syntactically present, triggering the CL/TE dual-header rules of RFC 9112 section 6.1 and 6.3. The server MUST at minimum close the connection. The empty value provides no valid framing mechanism, making the message body length indeterminate -- a condition that demands rejection for safety.

- **Pass (400 or close):** The server correctly rejects the ambiguous request.
- **Fail (2xx):** The server silently accepted a request with indeterminate framing, creating a smuggling-exploitable condition.

### Smuggling Attack Scenarios

- **Presence vs. Value Disagreement:** A front-end proxy checks for the Transfer-Encoding header's **presence** and, finding it, ignores Content-Length per RFC 9112 section 6.3. But with an empty value, it cannot apply any transfer decoding and may stall or error. A back-end that evaluates the **value** finds it empty, treats Transfer-Encoding as absent, and uses Content-Length framing. The desync between presence-based and value-based logic allows body boundary manipulation.
- **Header Stripping Bypass:** Some proxies strip empty-valued headers during normalization. If the front-end strips `Transfer-Encoding: ` and forwards only `Content-Length: 5`, but the back-end receives the original request (e.g., via connection reuse), the back-end sees Transfer-Encoding as present and ignores Content-Length. The attacker can embed a smuggled request in the body that only the back-end parses.
- **Fallback Framing Exploit:** A parser that sees an empty Transfer-Encoding may fall through to Content-Length framing. Another parser that treats the header as present but unrecognizable may respond with an error or close the connection. This inconsistency in fallback behavior can be exploited to desynchronize request boundaries across a proxy chain.

## Sources

- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
- [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
