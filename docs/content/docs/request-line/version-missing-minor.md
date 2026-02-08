---
title: "VERSION-MISSING-MINOR"
description: "VERSION-MISSING-MINOR test documentation"
weight: 15
---

| | |
|---|---|
| **Test ID** | `COMP-VERSION-MISSING-MINOR` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 §2.3](https://www.rfc-editor.org/rfc/rfc9112#section-2.3) |
| **Requirement** | MUST |
| **Expected** | `400` or close |

## What it sends

A request with `HTTP/1` as the version -- missing the dot and minor version digit.

```http
GET / HTTP/1\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

> "HTTP-version = HTTP-name '/' DIGIT '.' DIGIT" -- RFC 9112 §2.3

> "HTTP-version is case-sensitive." -- RFC 9112 §2.3

The HTTP version string requires exactly one digit, a dot, and one digit after `HTTP/`. `HTTP/1` omits the dot and the minor version digit entirely, so it does not match the grammar. Since the version field is malformed, the entire request-line is invalid:

> "Recipients of an invalid request-line SHOULD respond with either a 400 (Bad Request) error or a 301 (Moved Permanently) redirect with the request-target properly encoded." — RFC 9112 Section 3

## Why it matters

A truncated version string creates ambiguity about the client's capabilities. If a server guesses the minor version (e.g., assumes `HTTP/1.0` or `HTTP/1.1`), it may enable or disable features like persistent connections, chunked encoding, or Host header requirements incorrectly. Strict parsing prevents this guesswork.

## Deep Analysis

### Relevant ABNF

```
HTTP-version = HTTP-name "/" DIGIT "." DIGIT
HTTP-name    = %s"HTTP"
DIGIT        = %x30-39
```

The `HTTP-version` production requires five fixed components in sequence: the literal `HTTP`, a `/`, one `DIGIT`, a `.`, and one `DIGIT`. The string `HTTP/1` is missing the final two components (the dot and the minor version digit), so it does not match the grammar.

### RFC Evidence

The ABNF defines the required structure:

> "HTTP-version = HTTP-name '/' DIGIT '.' DIGIT" -- RFC 9112 Section 2.3

The version string is explicitly described as a major.minor pair:

> "HTTP uses a '<major>.<minor>' numbering scheme to indicate versions of the protocol. This specification defines version '1.1'." -- RFC 9112 Section 2.3

The version field is also case-sensitive, reinforcing that it must be parsed exactly:

> "HTTP-version is case-sensitive." -- RFC 9112 Section 2.3

### Chain of Reasoning

1. `HTTP/1` contains only the HTTP-name, a `/`, and one `DIGIT`. The required `.` and second `DIGIT` are absent. This is a clear grammar violation -- the string is truncated.
2. A server that receives `HTTP/1` cannot determine the minor version. Is it `HTTP/1.0` (no persistent connections by default, no chunked TE requirement) or `HTTP/1.1` (persistent connections, Host header required, chunked TE supported)?
3. Guessing the minor version is dangerous. If the server assumes `HTTP/1.1` and sends a chunked response, an `HTTP/1.0`-only client will fail to parse it. If the server assumes `HTTP/1.0` and closes the connection, it may break pipelining expectations of an `HTTP/1.1` client.
4. The RFC provides no leniency clause for truncated version strings. Unlike the whitespace flexibility in Section 3, the version grammar in Section 2.3 has no MAY-level relaxation.
5. Because the grammar is strictly defined and the ambiguity is unresolvable, rejection with `400` or connection close is the correct behavior.

### Scoring Justification

This test is **scored**. The `HTTP-version` ABNF is a normative MUST-level grammar rule. `HTTP/1` fails to match the production because it is missing the dot and minor digit. There is no MAY clause that permits accepting a truncated version. `400` or close = **Pass**, `2xx` = **Fail**.

## Sources

- [RFC 9112 §2.3 -- HTTP Version](https://www.rfc-editor.org/rfc/rfc9112#section-2.3)
