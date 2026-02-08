---
title: "MULTI-SP-REQUEST-LINE"
description: "MULTI-SP-REQUEST-LINE test documentation"
weight: 1
---

| | |
|---|---|
| **Test ID** | `RFC9112-3-MULTI-SP-REQUEST-LINE` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 §3](https://www.rfc-editor.org/rfc/rfc9112#section-3) |
| **Requirement** | SHOULD reject, MAY parse leniently |
| **Expected** | `400` or `2xx` |

## What it sends

A request-line with multiple spaces between components: `GET  /  HTTP/1.1` (double spaces).

```http
GET  / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

Note the double space between `GET` and `/`.


## What the RFC says

The request-line grammar requires exactly one space between components:

> "request-line = method SP request-target SP HTTP-version" -- RFC 9112 §3

Multiple spaces do not match this grammar, making the request-line invalid:

> "Recipients of an invalid request-line SHOULD respond with either a 400 (Bad Request) error or a 301 (Moved Permanently) redirect with the request-target properly encoded." — RFC 9112 Section 3

However, RFC 9112 §3 also permits lenient parsing:

> "Although the request-line grammar rule requires that each of the component elements be separated by a single SP octet, recipients MAY instead parse on whitespace-delimited word boundaries and, aside from the CRLF terminator, treat any form of whitespace as the SP separator while ignoring preceding or trailing whitespace; such whitespace includes one or more of the following octets: SP, HTAB, VT (%x0B), FF (%x0C), or bare CR." -- RFC 9112 §3

This means a server that collapses multiple spaces and processes the request is also RFC-compliant.

**Pass:** Server rejects with `400` (strict, follows SHOULD).
**Warn:** Server accepts and responds `2xx` (RFC-valid per MAY parse leniently).

## Why it matters

Some parsers are lenient and collapse multiple spaces. If a front-end collapses spaces but a back-end does not, they may parse the method, target, or version differently — leading to routing confusion or bypass.

## Deep Analysis

### Relevant ABNF

```
request-line = method SP request-target SP HTTP-version
SP           = %x20   ; a single space octet
```

The `SP` rule in HTTP ABNF (inherited from RFC 5234) matches exactly one `%x20` octet. The request-line grammar calls for `SP` (singular), not `*SP` or `1*SP`. Therefore, `GET  / HTTP/1.1` with a double space between `GET` and `/` does not match the `request-line` production.

### RFC Evidence

The specification is explicit that the grammar requires a single space:

> "A request-line begins with a method token, followed by a single space (SP), the request-target, and another single space (SP), and ends with the protocol version." -- RFC 9112 Section 3

It then immediately acknowledges that recipients may be lenient:

> "Although the request-line grammar rule requires that each of the component elements be separated by a single SP octet, recipients MAY instead parse on whitespace-delimited word boundaries and, aside from the CRLF terminator, treat any form of whitespace as the SP separator while ignoring preceding or trailing whitespace; such whitespace includes one or more of the following octets: SP, HTAB, VT (%x0B), FF (%x0C), or bare CR." -- RFC 9112 Section 3

But the specification warns about the consequences of leniency:

> "However, lenient parsing can result in request smuggling security vulnerabilities if there are multiple recipients of the message and each has its own unique interpretation of robustness." -- RFC 9112 Section 3

### Chain of Reasoning

1. The double space `GET  / HTTP/1.1` does not match the ABNF production `method SP request-target SP HTTP-version` because `SP` is exactly one `%x20`.
2. This makes the request-line technically invalid. The SHOULD-level recommendation is to respond with `400`.
3. However, the MAY clause explicitly permits lenient whitespace parsing, so a server that collapses the double space and processes the request normally is also conformant.
4. The security concern is real: if a front-end proxy collapses multiple spaces but a back-end does not, they may disagree on the boundary between method and request-target. An attacker could exploit this parser differential for routing confusion or request smuggling.
5. Both strict rejection (400) and lenient acceptance (2xx) are RFC-compliant behaviors.

### Scoring Justification

This test is **scored with two valid outcomes**. The SHOULD/MAY duality means that `400` (strict rejection) = **Pass** and `2xx` (lenient parsing per the explicit MAY) = **Warn**. Neither outcome is a failure, because the RFC explicitly permits both behaviors. A server that returns an unexpected status (e.g., `500`) would be a concern.

## Sources

- [RFC 9112 §3 -- Request Line](https://www.rfc-editor.org/rfc/rfc9112#section-3)
