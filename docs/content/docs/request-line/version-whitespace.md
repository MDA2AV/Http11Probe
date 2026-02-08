---
title: "VERSION-WHITESPACE"
description: "VERSION-WHITESPACE test documentation"
weight: 17
---

| | |
|---|---|
| **Test ID** | `COMP-VERSION-WHITESPACE` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 §2.3](https://www.rfc-editor.org/rfc/rfc9112#section-2.3) |
| **Requirement** | MUST |
| **Expected** | `400` or close |

## What it sends

A request with `HTTP/ 1.1` as the version -- a space character inserted between `HTTP/` and `1.1`.

```http
GET / HTTP/ 1.1\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

> "HTTP-version = HTTP-name '/' DIGIT '.' DIGIT" -- RFC 9112 §2.3

The HTTP-version is a single contiguous token with no internal whitespace. The space between the slash and the version digits breaks the token, making the request-line invalid. The grammar does not allow any SP or HTAB inside the version string.

> "Recipients of an invalid request-line SHOULD respond with either a 400 (Bad Request) error or a 301 (Moved Permanently) redirect with the request-target properly encoded." — RFC 9112 Section 3

## Why it matters

A server that is lenient about whitespace inside the version token could be tricked into parsing the request-line differently than a strict proxy in front of it. For example, a lenient parser might read `HTTP/` followed by ` 1.1` and strip the space, while a strict parser sees an invalid version. This differential creates opportunities for request smuggling.

## Deep Analysis

### Relevant ABNF

```
HTTP-version = HTTP-name "/" DIGIT "." DIGIT
HTTP-name    = %s"HTTP"
DIGIT        = %x30-39
```

The `HTTP-version` production is a single contiguous token: `HTTP` `/` `DIGIT` `.` `DIGIT` -- with no whitespace permitted between any of the components. The string `HTTP/ 1.1` inserts a `SP` (`%x20`) between the `/` and the first `DIGIT`, which does not match the grammar.

### RFC Evidence

The ABNF requires the version to be a contiguous token:

> "HTTP-version = HTTP-name '/' DIGIT '.' DIGIT" -- RFC 9112 Section 2.3

The version field terminates the request-line, and the request-line grammar provides the context:

> "A request-line begins with a method token, followed by a single space (SP), the request-target, and another single space (SP), and ends with the protocol version." -- RFC 9112 Section 3

The lenient whitespace parsing clause in Section 3 applies to the separators between the three request-line components (method, request-target, version) but not to whitespace within any individual component. For the version specifically, there is no relaxation:

> "Recipients of an invalid request-line SHOULD respond with either a 400 (Bad Request) error or a 301 (Moved Permanently) redirect with the request-target properly encoded." — RFC 9112 Section 3

### Chain of Reasoning

1. `HTTP/ 1.1` contains a space between `/` and `1`, which is not permitted by the `HTTP-version` ABNF. The grammar specifies a direct concatenation: `HTTP-name "/" DIGIT "." DIGIT`.
2. A whitespace-delimited parser splitting the request-line `GET / HTTP/ 1.1` on word boundaries would see four tokens: `GET`, `/`, `HTTP/`, and `1.1`. This does not match the three-component structure of `method SP request-target SP HTTP-version`.
3. The lenient parsing clause in RFC 9112 Section 3 permits treating whitespace as the `SP` separator between components, but it does not permit inserting whitespace inside any component. The version is one component, not two.
4. A proxy that reconstructs the version by concatenating `HTTP/` and `1.1` would silently "fix" the request, while a strict parser would reject it. This parser differential could allow an attacker to bypass front-end validation.
5. No MAY-level relaxation exists for whitespace inside the version token, making this a clear grammar violation.

### Scoring Justification

This test is **scored**. The `HTTP-version` ABNF is a normative MUST-level grammar rule that requires a contiguous token with no internal whitespace. The lenient parsing clause in Section 3 does not extend to whitespace within individual request-line components. `400` or close = **Pass**, `2xx` = **Fail**.

## Sources

- [RFC 9112 §2.3 -- HTTP Version](https://www.rfc-editor.org/rfc/rfc9112#section-2.3)
