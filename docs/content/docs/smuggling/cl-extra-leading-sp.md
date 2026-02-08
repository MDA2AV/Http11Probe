---
title: "CL-EXTRA-LEADING-SP"
description: "CL-EXTRA-LEADING-SP test documentation"
weight: 11
---

| | |
|---|---|
| **Test ID** | `SMUG-CL-EXTRA-LEADING-SP` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §5](https://www.rfc-editor.org/rfc/rfc9112#section-5) |
| **Requirement** | SHOULD |
| **Expected** | `400` or `2xx` |

## What it sends

`Content-Length:  5` — extra space between colon and value.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length:  5\r\n
\r\n
hello
```

Note the double space after the colon (extra leading OWS).

## What the RFC says

RFC 9112 §5 defines the field-line syntax:

> "field-line = field-name ":" OWS field-value OWS"

The specification explicitly permits optional whitespace before the field value:

> "A field line value might be preceded and/or followed by optional whitespace (OWS); a single SP preceding the field line value is preferred for consistent readability by humans. The field line value does not include that leading or trailing whitespace: OWS occurring before the first non-whitespace octet of the field line value, or after the last non-whitespace octet of the field line value, is excluded by parsers when extracting the field line value from a field line."

The double space after the colon is valid OWS. The field value after OWS stripping is `5`, which is a valid `1*DIGIT` Content-Length.

## Pass / Warn

Leading OWS before the field value is explicitly permitted by RFC 9112 §5. Whether one or two spaces appear, the parser must strip them. Both `400` (strict) and `2xx` (standard OWS trimming) are acceptable behaviors, but rejection is preferred.

**Pass:** Server rejects with `400` (strict, safe).
**Warn:** Server accepts and responds `2xx` (RFC-valid OWS trimming).

## Why it matters

While OWS is permitted, some parsers may fail to strip it correctly, causing the Content-Length value to be parsed as ` 5` (with a leading space) rather than `5`. This could lead to parser disagreements in proxy chains.

## Deep Analysis

### ABNF Analysis

RFC 9110 §8.6 defines the Content-Length grammar as:

> `Content-Length = 1*DIGIT`

The raw field value before OWS stripping is ` 5` (with an extra leading space). However, RFC 9112 §5 defines the field-line syntax:

> `field-line = field-name ":" OWS field-value OWS`

The OWS (optional whitespace) before the field value is explicitly permitted and must be stripped by parsers:

> "A field line value might be preceded and/or followed by optional whitespace (OWS); a single SP preceding the field line value is preferred for consistent readability by humans. The field line value does not include that leading or trailing whitespace: OWS occurring before the first non-whitespace octet of the field line value, or after the last non-whitespace octet of the field line value, is excluded by parsers when extracting the field line value from a field line."

After stripping the leading OWS (both spaces), the extracted field value is `5`, which matches `1*DIGIT` and is valid.

### RFC Evidence Chain

**Step 1 -- OWS is explicitly permitted.**

The double space between the colon and `5` is OWS. Whether one space or two, the RFC requires parsers to strip all leading whitespace before the first non-whitespace octet. The field value after extraction is `5`.

**Step 2 -- The extracted value is valid.**

After OWS stripping, `5` matches `1*DIGIT`. No invalid Content-Length rule is triggered. RFC 9112 §6.3 only applies to invalid Content-Length values:

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error."

Since the extracted value `5` is valid, no MUST-reject obligation arises.

**Step 3 -- Both responses are acceptable.**

A server that correctly strips OWS and processes the request normally (2xx) is following RFC 9112 §5 precisely. A server that rejects with 400 is being stricter than required but not violating any RFC rule.

### Scored / SHOULD Justification

This test is scored as **SHOULD** reject. The extra leading space is explicitly allowed by RFC 9112 §5's OWS rule. Both `400` (strict rejection) and `2xx` (standard OWS trimming) are compliant behaviors, but rejection is the safer choice in proxy chains.

### Real-World Smuggling Scenario

If a front-end proxy fails to strip OWS correctly and passes the raw value `  5` (with spaces) to its integer parser, the parse may fail or return 0. Meanwhile the back-end correctly strips OWS and reads 5 bytes of body. This disagreement on body length -- 0 vs. 5 -- means the front-end treats the body bytes as the start of the next request, enabling request smuggling. While the RFC is clear about OWS stripping, implementation bugs in this area are common.

## Sources

- [RFC 9112 §5](https://www.rfc-editor.org/rfc/rfc9112#section-5)
- [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
