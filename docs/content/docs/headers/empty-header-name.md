---
title: "EMPTY-HEADER-NAME"
description: "EMPTY-HEADER-NAME test documentation"
weight: 3
---

| | |
|---|---|
| **Test ID** | `RFC9112-5-EMPTY-HEADER-NAME` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 5](https://www.rfc-editor.org/rfc/rfc9112#section-5) |
| **Requirement** | Implicit MUST (grammar violation) |
| **Expected** | `400` or close |

## What it sends

A header line starting with a colon — effectively an empty field name: `: value`.

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
: empty-name\r\n
\r\n
```

A header line starting with `:` — the header name is empty.


## What the RFC says

The field-line grammar requires a non-empty field name:

> "field-line = field-name ':' OWS field-value OWS" -- RFC 9112 Section 5

> "field-name = token" -- RFC 9110 Section 5.1

> "token = 1*tchar" -- RFC 9110 Section 5.6.2

Field names are defined as `token = 1*tchar`, requiring **at least one** valid token character. An empty string does not match `1*tchar`. While there is no explicit "MUST reject empty field names with 400" statement, a line starting with `:` fails to match the `field-line` grammar entirely.

## Why it matters

A header line with an empty name is structurally ambiguous. Different parsers may treat `: value` as a valid header with an empty name, as a continuation of the previous header, or as garbage. This disagreement between parsers is a classic smuggling precondition.

## Deep Analysis

### Relevant ABNF Grammar

```
field-line   = field-name ":" OWS field-value OWS
field-name   = token
token        = 1*tchar
tchar        = "!" / "#" / "$" / "%" / "&" / "'" / "*"
             / "+" / "-" / "." / "^" / "_" / "`" / "|"
             / "~" / DIGIT / ALPHA
```

The critical production is `token = 1*tchar`. The `1*` operator requires at least one `tchar` character. An empty string (zero characters) cannot match this production.

### RFC Evidence

**RFC 9112 Section 5** defines the structure of a header field line:

> "Each field line consists of a case-insensitive field name followed by a colon (':'), optional leading whitespace, the field line value, and optional trailing whitespace." -- RFC 9112 Section 5

**RFC 9110 Section 5.6.2** defines the token rule used by field-name:

> "token = 1*tchar" -- RFC 9110 Section 5.6.2

**RFC 9112 Section 5** states the grammar:

> "field-line = field-name ':' OWS field-value OWS" -- RFC 9112 Section 5

### Chain of Reasoning

1. A line beginning with `:` (e.g., `: empty-name`) has an empty string where `field-name` should appear.
2. The `field-name` production requires `token`, and `token` requires `1*tchar` -- at least one valid token character.
3. An empty string does not match `1*tchar`, so the line fails to parse as a valid `field-line`.
4. A line that does not match the `field-line` grammar is unparseable. The general principle from RFC 9112 Section 2.2 applies: when servers receive malformed requests, they "SHOULD respond with a 400 (Bad Request) response and close the connection."
5. Although there is no explicit "MUST reject empty field names" statement, the grammar violation is unambiguous -- no conformant parser can produce a valid field-name from an empty string.

### Scoring Justification

**Scored (implicit MUST, grammar violation).** The requirement is implicit rather than explicit: the ABNF grammar makes it impossible for a conformant parser to accept an empty field name. Both 400 and connection close are acceptable outcomes because the RFC does not prescribe a specific status code for generic grammar violations. The `AllowConnectionClose` flag is set because closing the connection is a reasonable response to an unparseable message.

## Sources

- [RFC 9112 Section 5 -- Field Syntax](https://www.rfc-editor.org/rfc/rfc9112#section-5)
- [RFC 9110 Section 5.1 -- Field Names](https://www.rfc-editor.org/rfc/rfc9110#section-5.1)
