---
title: "TE-TRAILING-SPACE"
description: "TE-TRAILING-SPACE test documentation"
weight: 6
---

| | |
|---|---|
| **Test ID** | `SMUG-TE-TRAILING-SPACE` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 §5.5](https://www.rfc-editor.org/rfc/rfc9110#section-5.5), [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

`Transfer-Encoding: chunked ` (with a trailing space). The value does not exactly match `chunked`.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked \r\n
Content-Length: 5\r\n
\r\n
hello
```

Note the trailing space after `chunked`.


## What the RFC says

> "A field value does not include leading or trailing whitespace. When a specific version of HTTP allows such whitespace to appear in a message, a field parsing implementation MUST exclude such whitespace prior to evaluating the field value." -- RFC 9110 Section 5.5

> "A server that receives a request message with a transfer coding it does not understand SHOULD respond with 501 (Not Implemented)." -- RFC 9112 Section 6.1

> "If a message is received with both a Transfer-Encoding and a Content-Length header field, the Transfer-Encoding overrides the Content-Length. Such a message might indicate an attempt to perform request smuggling or response splitting and ought to be handled as an error." -- RFC 9112 Section 6.3

RFC 9110 Section 5.5 requires stripping leading/trailing whitespace from the field value before evaluation. After stripping, the value is `chunked` -- a valid transfer coding. However, some parsers may not strip OWS correctly and treat `chunked ` (with trailing space) as an unrecognized coding name.

## Why it matters

If one parser trims trailing whitespace and recognizes `chunked` while another treats `chunked ` as an unknown encoding and falls back to Content-Length, they will disagree on body framing -- a CL/TE desync.

## Deep Analysis

### ABNF

```
field-line     = field-name ":" OWS field-value OWS  ; RFC 9112 §5
field-value    = *field-content                       ; RFC 9110 §5.5
field-content  = field-vchar
                 [ 1*( SP / HTAB / field-vchar ) field-vchar ]
Transfer-Encoding = #transfer-coding                  ; RFC 9112 §6.1
transfer-coding   = token                             ; RFC 9110 §10.1.4
token             = 1*tchar                           ; RFC 9110 §5.6.2
```

The `field-line` rule includes trailing `OWS` after `field-value`. Per RFC 9112 section 5.1, leading and trailing OWS is excluded when extracting the field value. The critical question is whether the trailing space in `chunked ` falls within the OWS that gets stripped, or within the `field-value` as part of the transfer-coding token.

### RFC Evidence

> "A field value does not include leading or trailing whitespace. When a specific version of HTTP allows such whitespace to appear in a message, a field parsing implementation MUST exclude such whitespace prior to evaluating the field value." -- RFC 9110 §5.5

> "A field line value might be preceded and/or followed by optional whitespace (OWS); a single SP preceding the field line value is preferred for consistent readability by humans. The field line value does not include that leading or trailing whitespace." -- RFC 9112 §5.1

> "A server MAY reject a request that contains both Content-Length and Transfer-Encoding or process such a request in accordance with the Transfer-Encoding alone. Regardless, the server MUST close the connection after responding to such a request to avoid the potential attacks." -- RFC 9112 §6.1

### Chain of Reasoning

1. The test sends `Transfer-Encoding: chunked ` (trailing space) alongside `Content-Length: 5`.
2. RFC 9110 section 5.5 states: "A field value does not include leading or trailing whitespace." The field parsing implementation "MUST exclude such whitespace prior to evaluating the field value."
3. RFC 9112 section 5.1 reinforces this: the trailing OWS in `field-line = field-name ":" OWS field-value OWS` is not part of the field value.
4. After stripping trailing OWS, the field value should be `chunked` -- a valid, recognized transfer coding.
5. However, real-world parsers may not properly strip trailing OWS from the field value. A parser that includes the trailing space sees `chunked ` as the transfer-coding token, which does not match `chunked` in a byte-for-byte comparison. It may treat this as an unrecognized encoding.
6. The dual presence of Transfer-Encoding and Content-Length triggers RFC 9112 section 6.3. If one parser strips the trailing space and uses chunked framing while another does not strip it and falls back to Content-Length, the framing disagreement enables smuggling.

### Scored / Unscored Justification

This test is **scored** (MUST reject). The combined presence of Transfer-Encoding and Content-Length triggers the MUST-level connection-closure requirement in RFC 9112 section 6.1. While RFC 9110 section 5.5 requires stripping trailing whitespace (which would make the value `chunked`), the trailing space creates a practical ambiguity that many parsers handle inconsistently. The server MUST at minimum close the connection after responding to a request with both TE and CL.

- **Pass (400 or close):** The server correctly rejects the request or closes the connection per the dual-header rules.
- **Fail (2xx):** The server processed the request without closing the connection, violating the MUST requirement in section 6.1.

### Smuggling Attack Scenarios

- **OWS Stripping Disagreement:** A front-end proxy does not strip trailing OWS and sees `chunked ` as an unrecognized transfer coding, falling back to Content-Length. A back-end correctly strips the trailing space per RFC 9110 section 5.5 and processes `chunked` framing. The desync lets the attacker embed a smuggled request in the chunked body.
- **Trailing Space as Encoding Obfuscation:** Some WAFs check for `Transfer-Encoding: chunked` (exact match). The trailing space causes the check to fail, and the WAF treats Transfer-Encoding as absent. The origin server strips the space and processes chunked encoding normally, allowing smuggled requests to bypass the WAF.
- **Whitespace-Sensitive Token Comparison:** Parsers that compare transfer-coding names using byte-for-byte matching (rather than stripping OWS first) will see `chunked ` (7 bytes) as different from `chunked` (7 bytes without space). If such a parser is on one side of a proxy chain and a whitespace-tolerant parser is on the other, the attacker controls which framing mechanism each parser uses.

## Sources

- [RFC 9110 §5.5](https://www.rfc-editor.org/rfc/rfc9110#section-5.5)
- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
- [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
