---
title: "REQUEST-LINE-TAB"
description: "REQUEST-LINE-TAB test documentation"
weight: 14
---

| | |
|---|---|
| **Test ID** | `COMP-REQUEST-LINE-TAB` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 §3](https://www.rfc-editor.org/rfc/rfc9112#section-3) |
| **Requirement** | SHOULD reject, MAY accept |
| **Expected** | `400` or `2xx` |

## What it sends

A request-line that uses a horizontal tab (HT, 0x09) instead of a space (SP, 0x20) between the method and the request-target.

```http
GET\t/ HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

The `\t` between `GET` and `/` is a tab character, not a space.

## What the RFC says

The request-line grammar requires exactly one space (0x20) as separator:

> "request-line = method SP request-target SP HTTP-version" -- RFC 9112 §3

A tab character does not match `SP`, making the request-line technically invalid:

> "Recipients of an invalid request-line SHOULD respond with either a 400 (Bad Request) error or a 301 (Moved Permanently) redirect with the request-target properly encoded." — RFC 9112 Section 3

However, RFC 9112 §3 also permits lenient parsing:

> "Although the request-line grammar rule requires that each of the component elements be separated by a single SP octet, recipients MAY instead parse on whitespace-delimited word boundaries and, aside from the CRLF terminator, treat any form of whitespace as the SP separator while ignoring preceding or trailing whitespace; such whitespace includes one or more of the following octets: SP, HTAB, VT (%x0B), FF (%x0C), or bare CR." -- RFC 9112 §3

This explicitly lists HTAB as accepted whitespace, so a server that treats tab as a separator is also RFC-compliant.

**Pass:** Server rejects with `400` (strict, follows SHOULD).
**Warn:** Server accepts and responds `2xx` (RFC-valid per MAY parse leniently).

## Why it matters

If a front-end proxy collapses all whitespace (including tabs) while a back-end server only recognizes spaces, they may disagree on where the method, target, and version boundaries are. This kind of parser differential can be exploited for request smuggling or routing bypasses.

## Deep Analysis

### Relevant ABNF

```
request-line = method SP request-target SP HTTP-version
SP           = %x20   ; exactly one space octet
HTAB         = %x09   ; horizontal tab
```

The `SP` rule matches only `%x20`. A horizontal tab (`%x09` / HTAB) is a distinct octet and does not match the `SP` production. Therefore, `GET\t/ HTTP/1.1` with a tab between method and request-target does not conform to the `request-line` grammar.

### RFC Evidence

The grammar is stated with `SP` as the required separator:

> "A request-line begins with a method token, followed by a single space (SP), the request-target, and another single space (SP), and ends with the protocol version." -- RFC 9112 Section 3

The lenient parsing allowance explicitly includes HTAB in the set of accepted whitespace:

> "Although the request-line grammar rule requires that each of the component elements be separated by a single SP octet, recipients MAY instead parse on whitespace-delimited word boundaries and, aside from the CRLF terminator, treat any form of whitespace as the SP separator while ignoring preceding or trailing whitespace; such whitespace includes one or more of the following octets: SP, HTAB, VT (%x0B), FF (%x0C), or bare CR." -- RFC 9112 Section 3

The security warning applies equally to tab-based parsing differentials:

> "However, lenient parsing can result in request smuggling security vulnerabilities if there are multiple recipients of the message and each has its own unique interpretation of robustness." -- RFC 9112 Section 3

### Chain of Reasoning

1. The tab octet `%x09` does not match `SP` (`%x20`), so `GET\t/ HTTP/1.1` is syntactically invalid per the strict ABNF grammar.
2. The SHOULD-level recommendation for invalid request-lines is to respond with `400`.
3. However, the MAY clause explicitly lists `HTAB` as an acceptable whitespace character for lenient parsers. A server that treats the tab as equivalent to a space and successfully parses the request is RFC-compliant.
4. The security risk is a parser differential: if a front-end proxy recognizes only `SP` as the delimiter while a back-end server also accepts HTAB, they will parse the same bytes into different request-line components. This is a classic request smuggling vector.
5. Strict rejection is safer; lenient acceptance is permitted. Both are valid.

### Scoring Justification

This test is **scored with two valid outcomes**, mirroring the SHOULD/MAY pattern. `400` (strict rejection) = **Pass**, `2xx` (lenient acceptance per the explicit MAY listing of HTAB) = **Warn**. The MAY clause prevents treating lenient acceptance as a failure.

## Sources

- [RFC 9112 §3 -- Request Line](https://www.rfc-editor.org/rfc/rfc9112#section-3)
