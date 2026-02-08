---
title: "CL-TRAILING-SPACE"
description: "CL-TRAILING-SPACE test documentation"
weight: 10
---

| | |
|---|---|
| **Test ID** | `SMUG-CL-TRAILING-SPACE` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §5](https://www.rfc-editor.org/rfc/rfc9112#section-5) |
| **Requirement** | SHOULD |
| **Expected** | `400` or `2xx` |

## What it sends

`Content-Length: 5 ` — trailing space after the value.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 5 \r\n
\r\n
hello
```

Note the trailing space after `5` in the Content-Length value.

## What the RFC says

RFC 9112 §5 defines the field-line syntax:

> "field-line = field-name ":" OWS field-value OWS"

The specification explicitly requires parsers to strip trailing whitespace:

> "A field line value might be preceded and/or followed by optional whitespace (OWS); a single SP preceding the field line value is preferred for consistent readability by humans. The field line value does not include that leading or trailing whitespace: OWS occurring before the first non-whitespace octet of the field line value, or after the last non-whitespace octet of the field line value, is excluded by parsers when extracting the field line value from a field line."

After OWS stripping, the remaining value `5` is a valid `1*DIGIT` Content-Length.

## Pass / Warn

OWS (optional whitespace) after the field value is explicitly permitted by RFC 9112 §5. Trimming it and processing normally is valid behavior. However, `400` is the stricter/safer choice. Both responses are RFC-compliant, but rejection is preferred.

## Why it matters

While trailing OWS is permitted, some parsers may include the trailing space in the field value, causing the Content-Length to be parsed as `5 ` rather than `5`. This could lead to parser disagreements or numeric conversion failures in proxy chains.

## Deep Analysis

### ABNF Analysis

RFC 9110 §8.6 defines the Content-Length grammar as:

> `Content-Length = 1*DIGIT`

The raw field-line value (before OWS processing) is `5 ` (digit followed by a space). However, RFC 9112 §5 defines the field-line syntax:

> `field-line = field-name ":" OWS field-value OWS`

The trailing OWS after the field value is explicitly permitted and must be stripped:

> "A field line value might be preceded and/or followed by optional whitespace (OWS); a single SP preceding the field line value is preferred for consistent readability by humans. The field line value does not include that leading or trailing whitespace: OWS occurring before the first non-whitespace octet of the field line value, or after the last non-whitespace octet of the field line value, is excluded by parsers when extracting the field line value from a field line."

After stripping the trailing OWS, the extracted field value is `5`, which matches `1*DIGIT` and is valid.

### RFC Evidence Chain

**Step 1 -- Trailing OWS is explicitly permitted.**

The space after `5` falls after the last non-whitespace octet of the field value. Per RFC 9112 §5, parsers must exclude it. The resulting field value is `5`.

**Step 2 -- The extracted value is valid.**

After OWS stripping, `5` matches `1*DIGIT`. No invalid Content-Length rule is triggered. RFC 9112 §6.3 only applies to invalid Content-Length values:

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error."

Since the extracted value `5` is valid, no MUST-reject obligation arises.

**Step 3 -- Both responses are acceptable.**

A server that correctly strips trailing OWS and processes the request normally (2xx) is following RFC 9112 §5 precisely. A server that rejects with 400 is being stricter than required but not violating any rule.

### Scored / SHOULD Justification

This test is scored as **SHOULD** reject. Trailing OWS is explicitly allowed by RFC 9112 §5. Both `400` (strict rejection) and `2xx` (standard OWS trimming) are compliant behaviors, but rejection is the safer choice in proxy chains where OWS handling inconsistencies may arise.

### Real-World Smuggling Scenario

If a front-end proxy includes the trailing space in the field value and passes `5 ` (with space) to its integer parser, the behavior depends on the parser: some return 5 (ignoring trailing non-digits), some return 0 (parse failure), and some throw an error. Meanwhile the back-end correctly strips OWS and reads 5 bytes of body. A parser that returns 0 reads no body, causing the 5 body bytes to be interpreted as the next request. A parser that throws an error might close the connection or forward unpredictably. The trailing space is a subtle probe for parser differential behavior.

## Sources

- [RFC 9112 §5](https://www.rfc-editor.org/rfc/rfc9112#section-5)
- [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
