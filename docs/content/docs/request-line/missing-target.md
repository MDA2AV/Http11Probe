---
title: "MISSING-TARGET"
description: "MISSING-TARGET test documentation"
weight: 2
---

| | |
|---|---|
| **Test ID** | `RFC9112-3-MISSING-TARGET` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 3](https://www.rfc-editor.org/rfc/rfc9112#section-3) |
| **Requirement** | MUST (grammar violation) |
| **Expected** | `400` or close |

## What it sends

A request-line with no request-target: `GET HTTP/1.1` (method directly followed by version, no URI).

```http
GET HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

The request-target (path) is missing — `GET` is followed directly by the version string with no path in between. The server is likely to parse `HTTP/1.1` as the request-target, leaving no version field at all.

## What the RFC says

The request-target is a required component of the request-line grammar:

> "request-line = method SP request-target SP HTTP-version" — RFC 9112 Section 3

Without a request-target, the line does not match the grammar and is invalid:

> "Recipients of an invalid request-line SHOULD respond with either a 400 (Bad Request) error or a 301 (Moved Permanently) redirect with the request-target properly encoded." — RFC 9112 Section 3

## Why it matters

A missing request-target produces a fundamentally malformed request-line. The server cannot determine what resource the client intended to access. Strict rejection prevents ambiguous parsing where `HTTP/1.1` could be misinterpreted as the target path.

## Deep Analysis

### Relevant ABNF

```
request-line   = method SP request-target SP HTTP-version
request-target = origin-form / absolute-form / authority-form / asterisk-form
origin-form    = absolute-path [ "?" query ]
```

The request-line grammar mandates three components separated by two `SP` octets. When the request-target is absent, the line contains only two whitespace-delimited tokens instead of three, and no token in the line matches any `request-target` production.

### RFC Evidence

The grammar itself is the primary evidence -- `request-target` is not optional:

> "A request-line begins with a method token, followed by a single space (SP), the request-target, and another single space (SP), and ends with the protocol version." -- RFC 9112 Section 3

When the request-target is missing, the server sees `GET HTTP/1.1` which a whitespace-delimited parser would split into just two tokens: `GET` as the method and `HTTP/1.1` as the request-target, with no version field at all. The RFC is explicit about how to handle such invalid lines:

> "Recipients of an invalid request-line SHOULD respond with either a 400 (Bad Request) error or a 301 (Moved Permanently) redirect with the request-target properly encoded." — RFC 9112 Section 3

The security rationale for strict rejection is also stated:

> "A recipient SHOULD NOT attempt to autocorrect and then process the request without a redirect, since the invalid request-line might be deliberately crafted to bypass security filters along the request chain." — RFC 9112 Section 3

### Chain of Reasoning

1. The ABNF `request-line = method SP request-target SP HTTP-version` requires exactly three whitespace-separated components. With the request-target missing, the line `GET HTTP/1.1` has only two.
2. A lenient parser splitting on whitespace boundaries would read `GET` as method and `HTTP/1.1` as the request-target, with no version token remaining. The version would be indeterminate.
3. Without a valid HTTP-version, the server cannot determine the protocol level. This makes the request fundamentally unparseable in a spec-compliant way.
4. Even if the server guesses the intent, doing so would mask a potentially malicious probe -- the RFC explicitly warns against autocorrection.
5. Because the grammar violation is unambiguous, both `400` and connection close are appropriate responses.

### Scoring Justification

This test is **scored**. The request-line grammar is a MUST-level requirement (the grammar itself is normative), and a missing component produces an invalid request-line. The RFC's SHOULD recommendation for `400` on invalid request-lines makes rejection the expected behavior. `400` or close = **Pass**, any other response = **Fail**.

## Sources

- [RFC 9112 Section 3 -- Request Line](https://www.rfc-editor.org/rfc/rfc9112#section-3)
