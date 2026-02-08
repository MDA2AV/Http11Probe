---
title: "INVALID-HEADER-NAME"
description: "INVALID-HEADER-NAME test documentation"
weight: 4
---

| | |
|---|---|
| **Test ID** | `RFC9112-5-INVALID-HEADER-NAME` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 5](https://www.rfc-editor.org/rfc/rfc9112#section-5) |
| **Requirement** | Implicit MUST (grammar violation) |
| **Expected** | `400` or close |

## What it sends

A header with non-token characters in the field name (e.g., characters outside the `tchar` set defined in RFC 9110 Section 5.6.2).

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
Bad[Name: value\r\n
\r\n
```

The header name contains `[` which is not a valid token character.


## What the RFC says

> "field-name = token" -- RFC 9110 Section 5.1

> "token = 1*tchar" -- RFC 9110 Section 5.6.2

> "tchar = '!' / '#' / '$' / '%' / '&' / ''' / '*' / '+' / '-' / '.' / '^' / '_' / '`' / '|' / '~' / DIGIT / ALPHA ; any VCHAR, except delimiters" -- RFC 9110 Section 5.6.2

The `[` character in `Bad[Name` is not in the `tchar` set, so the field name violates the grammar. Characters outside this set in a field name make the header line unparseable as a valid `field-line`.

## Why it matters

If a server accepts non-token characters in field names, it may interpret a header differently from other components in the request chain. Delimiter characters like `[`, `]`, `{`, `}`, or `@` in field names could cause parsing divergence between the server and upstream proxies.

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

The `tchar` set is a closed enumeration. Characters outside this set -- including `[`, `]`, `{`, `}`, `(`, `)`, `<`, `>`, `@`, `,`, `;`, `\`, `"`, `/`, `?`, `=`, and SP -- are delimiters or otherwise excluded.

### RFC Evidence

**RFC 9110 Section 5.1** ties field names to the token production:

> "field-name = token" -- RFC 9110 Section 5.1

**RFC 9110 Section 5.6.2** defines token as exclusively composed of tchar:

> "token = 1*tchar" -- RFC 9110 Section 5.6.2

**RFC 9110 Section 5.6.2** enumerates the allowed characters:

> "tchar = '!' / '#' / '$' / '%' / '&' / ''' / '*' / '+' / '-' / '.' / '^' / '_' / '`' / '|' / '~' / DIGIT / ALPHA ; any VCHAR, except delimiters" -- RFC 9110 Section 5.6.2

### Chain of Reasoning

1. The header name `Bad[Name` contains `[`, which is not in the `tchar` set.
2. Since `field-name = token = 1*tchar`, a field name containing non-tchar characters fails to match the grammar.
3. A line that does not match `field-line` is a malformed message element. RFC 9112 Section 2.2 states that when servers receive malformed requests, they "SHOULD respond with a 400 (Bad Request) response and close the connection."
4. The violation is structural -- the parser encounters a character that cannot appear in a field name, making the rest of the line ambiguous (is the `[` part of the name? a delimiter? the start of something else?).

### Scoring Justification

**Scored (implicit MUST, grammar violation).** Like EMPTY-HEADER-NAME, this is a grammar violation rather than an explicitly stated MUST. The ABNF is unambiguous -- `[` is not a `tchar` -- but no specific status code is mandated for this class of error. Both 400 and connection close are acceptable, so `AllowConnectionClose` is set. A server that processes the request normally (2xx) is accepting input that violates the grammar, which is a compliance failure.

## Sources

- [RFC 9112 Section 5 -- Field Syntax](https://www.rfc-editor.org/rfc/rfc9112#section-5)
- [RFC 9110 Section 5.6.2 -- Tokens](https://www.rfc-editor.org/rfc/rfc9110#section-5.6.2)
