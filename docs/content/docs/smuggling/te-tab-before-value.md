---
title: "TE-TAB-BEFORE-VALUE"
description: "TE-TAB-BEFORE-VALUE test documentation"
weight: 56
---

| | |
|---|---|
| **Test ID** | `SMUG-TE-TAB-BEFORE-VALUE` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 §5.6.3](https://www.rfc-editor.org/rfc/rfc9110#section-5.6.3) |
| **Requirement** | Unscored |
| **Expected** | `400` or `2xx` |

## What it sends

Transfer-Encoding with a horizontal tab (HTAB) instead of space as the OWS separator before the value, alongside Content-Length.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding:\tchunked\r\n
Content-Length: 5\r\n
\r\n
hello
```

The tab character (`\t`, 0x09) separates the colon from `chunked` instead of the usual space (0x20).


## What the RFC says

> "OWS = \*( SP / HTAB ) ; optional whitespace" -- RFC 9110 Section 5.6.3

> "The OWS rule is used where zero or more linear whitespace octets might appear." -- RFC 9110 Section 5.6.3

Tab (HTAB) is explicitly defined as valid optional whitespace alongside space (SP). A compliant server should parse `Transfer-Encoding:\tchunked` identically to `Transfer-Encoding: chunked`.

## Why this test is unscored

HTAB is explicitly valid as OWS per the RFC. A server that accepts `Transfer-Encoding:\tchunked` is RFC-compliant. A server that rejects it is being strict but safe. Since both behaviors are valid, the test cannot be scored.

**Pass:** Server rejects with `400` (strict, safe -- some parsers only accept SP).
**Warn:** Server accepts and responds `2xx` (RFC-valid per OWS definition).

## Why it matters

Despite HTAB being valid OWS per the RFC, some real-world parsers only accept SP (0x20) as whitespace before the field value. When a front-end strips tabs or rejects the header while a back-end accepts it, they disagree on whether Transfer-Encoding is present — creating a CL/TE desync with Content-Length as the fallback.

## Deep Analysis

### ABNF

```
field-line   = field-name ":" OWS field-value OWS  ; RFC 9112 §5
OWS          = *( SP / HTAB )                       ; RFC 9110 §5.6.3
Transfer-Encoding = #transfer-coding                ; RFC 9112 §6.1
transfer-coding   = token                           ; RFC 9110 §10.1.4
token             = 1*tchar                         ; RFC 9110 §5.6.2
```

The `OWS` production between the colon and the field-value explicitly includes both SP (0x20) and HTAB (0x09). The `field-line` rule uses `OWS` after the colon, meaning `Transfer-Encoding:\tchunked` has a tab character in the position where OWS is permitted. The ABNF unambiguously permits this.

### RFC Evidence

> "OWS = \*( SP / HTAB ) ; optional whitespace" -- RFC 9110 §5.6.3

> "A field line value might be preceded and/or followed by optional whitespace (OWS); a single SP preceding the field line value is preferred for consistent readability by humans. The field line value does not include that leading or trailing whitespace." -- RFC 9112 §5.1

> "A server MAY reject a request that contains both Content-Length and Transfer-Encoding or process such a request in accordance with the Transfer-Encoding alone. Regardless, the server MUST close the connection after responding to such a request to avoid the potential attacks." -- RFC 9112 §6.1

### Chain of Reasoning

1. The test sends `Transfer-Encoding:\tchunked` (tab before the value) alongside `Content-Length: 5`.
2. RFC 9110 section 5.6.3 defines OWS as `*( SP / HTAB )`, explicitly including the horizontal tab character. RFC 9112 section 5 uses OWS after the colon in the `field-line` production.
3. RFC 9112 section 5.1 notes that "a single SP preceding the field line value is preferred for consistent readability by humans" -- indicating that SP is the convention, but HTAB is still valid per the grammar.
4. A compliant parser should strip the leading OWS (whether SP or HTAB) and evaluate the field value as `chunked`. This is a valid transfer coding, and the request should be processed with chunked framing.
5. However, real-world parsers often only accept SP (0x20) as whitespace. A parser that treats HTAB as an invalid character in this position may reject the header or include the tab as part of the field value, resulting in `\tchunked` as the transfer coding name -- which does not match `chunked`.
6. The presence of `Content-Length: 5` alongside Transfer-Encoding creates the CL/TE dual-header scenario. If the HTAB causes one parser to not recognize Transfer-Encoding while another parser handles it correctly, the framing disagreement enables smuggling.

### Scored / Unscored Justification

This test is **unscored** because HTAB is explicitly valid as OWS per the RFC ABNF. A server that accepts the tab and processes `chunked` is fully RFC-compliant. A server that rejects with `400` is being overly strict but safe. Since both behaviors are defensible and no MUST-level requirement is violated by either, the test cannot be scored.

- **Pass (400):** Strict rejection -- the server only accepts SP as OWS, which is safer but stricter than the RFC requires.
- **Warn (2xx):** RFC-compliant -- the server correctly parsed the HTAB as valid OWS and processed chunked encoding.

### Smuggling Attack Scenarios

- **SP-Only Parser Desync:** A front-end proxy that only recognizes SP as whitespace sees `\tchunked` as part of the field value, treats it as an unrecognized transfer coding, and falls back to Content-Length. A back-end that correctly handles HTAB as OWS sees `chunked` and uses chunked framing. The attacker embeds a smuggled request inside the chunked body.
- **Tab Stripping Inconsistency:** Some proxies strip all whitespace (including tabs) from header values during normalization, while others only strip spaces. If a front-end strips tabs but uses Content-Length for its own framing decision before normalizing, and the normalized `Transfer-Encoding: chunked` is what the back-end receives, the proxy's pre-normalization framing may have consumed different bytes than the back-end expects.
- **Binary Character Confusion:** The HTAB character (0x09) is a control character that some parsers may flag as suspicious or invalid in header values. A WAF that rejects or sanitizes control characters in headers may strip the entire Transfer-Encoding header, while the origin server processes the tab as valid OWS. The WAF's framing analysis (based on Content-Length) diverges from the server's framing (based on chunked encoding).

## Sources

- [RFC 9110 §5.6.3](https://www.rfc-editor.org/rfc/rfc9110#section-5.6.3)
- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
