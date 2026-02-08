---
title: "VERSION-LEADING-ZEROS"
description: "VERSION-LEADING-ZEROS test documentation"
weight: 16
---

| | |
|---|---|
| **Test ID** | `COMP-VERSION-LEADING-ZEROS` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 §2.3](https://www.rfc-editor.org/rfc/rfc9112#section-2.3) |
| **Requirement** | MUST |
| **Expected** | `400` or close |

## What it sends

A request with `HTTP/01.01` as the version -- leading zeros on both the major and minor version digits.

```http
GET / HTTP/01.01\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

> "HTTP-version = HTTP-name '/' DIGIT '.' DIGIT" -- RFC 9112 §2.3

The grammar specifies exactly one `DIGIT` on each side of the dot. `01` is two digits, not one. `HTTP/01.01` does not match the production rule, making it a syntactically invalid version string. Since the version is malformed, the entire request-line is invalid:

> "Recipients of an invalid request-line SHOULD respond with either a 400 (Bad Request) error or a 301 (Moved Permanently) redirect with the request-target properly encoded." — RFC 9112 Section 3

## Why it matters

Leading zeros may cause version comparison bugs. A parser that strips leading zeros might interpret `HTTP/01.01` as `HTTP/1.1`, while another parser might reject it or treat it as an unknown version. This disagreement between parsers can lead to inconsistent behavior in proxy chains, where one component processes the request differently than another.

## Deep Analysis

### Relevant ABNF

```
HTTP-version = HTTP-name "/" DIGIT "." DIGIT
HTTP-name    = %s"HTTP"
DIGIT        = %x30-39  ; 0-9
```

The `DIGIT` rule (from RFC 5234) matches exactly one octet in the range `%x30-39`. It is not `1*DIGIT` or `*DIGIT` -- the production calls for a single `DIGIT` on each side of the dot. `HTTP/01.01` has two digits (`01`) for both major and minor, which means neither `01` matches the `DIGIT` production. The version string is syntactically invalid.

### RFC Evidence

The ABNF is stated clearly in the version definition:

> "HTTP-version = HTTP-name '/' DIGIT '.' DIGIT" -- RFC 9112 Section 2.3

The version field is part of the request-line, and a malformed version makes the entire line invalid:

> "A request-line begins with a method token, followed by a single space (SP), the request-target, and another single space (SP), and ends with the protocol version." -- RFC 9112 Section 3

The recommendation for invalid request-lines applies:

> "Recipients of an invalid request-line SHOULD respond with either a 400 (Bad Request) error or a 301 (Moved Permanently) redirect with the request-target properly encoded." — RFC 9112 Section 3

### Chain of Reasoning

1. The ABNF specifies `DIGIT` (singular), not `1*DIGIT`. `HTTP/01.01` uses two digits for both major and minor version numbers, which exceeds the grammar's allowance.
2. There is no leniency clause in RFC 9112 Section 2.3 for leading zeros -- unlike the request-line SP/whitespace flexibility in Section 3, the version grammar has no MAY-level relaxation.
3. A parser that strips leading zeros would interpret `HTTP/01.01` as `HTTP/1.1`, but a strict parser would reject it. This creates a parser differential: one component in a proxy chain might accept the request while another might not.
4. If the front-end strips zeros and forwards `HTTP/1.1` but the back-end sees the original `HTTP/01.01` and rejects it, the front-end has already committed to the connection. If the front-end accepts and the back-end also accepts after stripping, they may still disagree on version semantics.
5. The strict grammar with no relaxation clause makes this a clear MUST-level violation.

### Scoring Justification

This test is **scored**. The `HTTP-version` ABNF is a normative grammar rule with no MAY-level leniency for extra digits. `HTTP/01.01` is unambiguously invalid. `400` or close = **Pass**, `2xx` (accepting a malformed version) = **Fail**.

## Sources

- [RFC 9112 §2.3 -- HTTP Version](https://www.rfc-editor.org/rfc/rfc9112#section-2.3)
