---
title: "TE-SP-BEFORE-COLON"
description: "TE-SP-BEFORE-COLON test documentation"
weight: 7
---

| | |
|---|---|
| **Test ID** | `SMUG-TE-SP-BEFORE-COLON` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §5.1](https://www.rfc-editor.org/rfc/rfc9112#section-5.1) |
| **Requirement** | MUST |
| **Expected** | `400` or close |

## What it sends

`Transfer-Encoding : chunked` — space before the colon.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding : chunked\r\n
Content-Length: 5\r\n
\r\n
hello
```

Note the space between `Transfer-Encoding` and the colon.


## What the RFC says

> "No whitespace is allowed between the field name and colon. In the past, differences in the handling of such whitespace have led to security vulnerabilities in request routing and response handling." -- RFC 9112 Section 5.1

> "A server MUST reject, with a response status code of 400 (Bad Request), any received request message that contains whitespace between a header field name and colon." -- RFC 9112 Section 5.1

This is MUST-level language -- servers have no discretion here.

## Why it matters

This is the Transfer-Encoding variant of the SP-BEFORE-COLON smuggling technique. If one parser ignores the space and processes chunked encoding while another rejects or ignores the header, they will frame the body differently -- a direct CL/TE desync.

## Deep Analysis

### ABNF

```
field-line   = field-name ":" OWS field-value OWS  ; RFC 9112 §5
field-name   = token                                ; RFC 9110 §5.1
token        = 1*tchar                              ; RFC 9110 §5.6.2
```

The ABNF for `field-line` shows the colon immediately follows `field-name` with no intervening whitespace. The `OWS` (optional whitespace) only appears **after** the colon, not before it. The space in `Transfer-Encoding : chunked` falls between the field-name and the colon, which is a position where the grammar explicitly permits nothing.

### RFC Evidence

> "No whitespace is allowed between the field name and colon. In the past, differences in the handling of such whitespace have led to security vulnerabilities in request routing and response handling." -- RFC 9112 §5.1

> "A server MUST reject, with a response status code of 400 (Bad Request), any received request message that contains whitespace between a header field name and colon." -- RFC 9112 §5.1

> "If a message is received with both a Transfer-Encoding and a Content-Length header field, the Transfer-Encoding overrides the Content-Length. Such a message might indicate an attempt to perform request smuggling or response splitting and ought to be handled as an error." -- RFC 9112 §6.3

### Chain of Reasoning

1. The test sends `Transfer-Encoding : chunked` (space before the colon) alongside `Content-Length: 5`.
2. RFC 9112 section 5.1 uses **MUST-level** language: the server "MUST reject, with a response status code of 400 (Bad Request), any received request message that contains whitespace between a header field name and colon."
3. The RFC explicitly explains the security rationale: "In the past, differences in the handling of such whitespace have led to security vulnerabilities in request routing and response handling."
4. A lenient parser that strips the space and processes `Transfer-Encoding: chunked` would use chunked framing. A strict parser that sees `Transfer-Encoding ` (with trailing space) as the field-name would not recognize it as `Transfer-Encoding` and would fall back to Content-Length.
5. This is exactly the kind of parser disagreement the RFC warns about. The MUST-level rejection requirement exists precisely because permitting whitespace before the colon creates exploitable ambiguity.
6. The ABNF is unambiguous: `field-line = field-name ":" OWS field-value OWS`. There is no OWS or BWS before the colon.

### Scored / Unscored Justification

This test is **scored** (MUST reject with `400`). RFC 9112 section 5.1 contains one of the clearest MUST-level requirements in the specification, explicitly mandating `400 (Bad Request)` for any whitespace between field name and colon. No `AllowConnectionClose` alternative is acceptable because the RFC specifies both the response code (`400`) and the action (reject). The historical security vulnerabilities cited in the RFC underscore why this requirement exists.

- **Pass (400):** The server correctly rejects the request per the explicit MUST requirement.
- **Fail (2xx or close):** The server failed to issue the required `400` response, either accepting the malformed header or merely closing the connection.

### Smuggling Attack Scenarios

- **Field-Name Mismatch Desync:** A lenient front-end strips the space before the colon and sees `Transfer-Encoding: chunked`, using chunked framing. A strict back-end treats `Transfer-Encoding ` (with trailing space) as an unknown header name and falls back to `Content-Length: 5`. The front-end consumes the body as chunked data while the back-end consumes only 5 bytes, leaving attacker-controlled data on the socket as the next request.
- **Proxy Normalization Bypass:** Some proxies normalize headers by removing spaces around colons. The front-end normalizes `Transfer-Encoding : chunked` to `Transfer-Encoding: chunked` before forwarding. The back-end processes the normalized header normally. But if the proxy made its own framing decision based on the **original** malformed header (possibly treating it as unrecognized), the proxy and back-end disagree on body boundaries.
- **WAF Evasion via Space Injection:** Security devices that check for `Transfer-Encoding:` (without a space before the colon) will not match `Transfer-Encoding :` (with the space). The request bypasses the WAF's smuggling detection, but the origin server's lenient parser strips the space and processes chunked encoding, allowing the attacker to smuggle requests past the security layer.

## Sources

- [RFC 9112 §5.1](https://www.rfc-editor.org/rfc/rfc9112#section-5.1)
