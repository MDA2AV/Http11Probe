---
title: "METHOD-CASE"
description: "METHOD-CASE test documentation"
weight: 10
---

| | |
|---|---|
| **Test ID** | `COMP-METHOD-CASE` |
| **Category** | Compliance |
| **RFC** | [RFC 9110 Section 9.1](https://www.rfc-editor.org/rfc/rfc9110#section-9.1) |
| **Requirement** | Case-sensitive |
| **Expected** | `400`/`405`/`501` or `2xx` |

## What it sends

`get / HTTP/1.1` — lowercase method name.

```http
get / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

The method `get` is lowercase instead of the standard `GET`.


## What the RFC says

> "The method token is case-sensitive because it might be used as a gateway to object-based systems with case-sensitive method names. By convention, standardized methods are defined in all-uppercase US-ASCII letters." — RFC 9110 Section 9.1

> "The request method is case-sensitive." — RFC 9112 Section 3.1

## Why this test is unscored

The RFC states that method tokens are case-sensitive, but there is no MUST-level requirement that servers reject lowercase methods. Many servers accept `get` as equivalent to `GET` in practice. Both behaviors are common, and neither represents a security risk -- this test observes the behavior without scoring it.

## Why it matters

The method token is case-sensitive by definition. A server that rejects `get` is strictly correct. A server that accepts `get` treats methods case-insensitively, which works in practice but deviates from the spec.

**Pass:** Server rejects with `400`, `405`, or `501` (strict case-sensitive parsing).
**Warn:** Server accepts with `2xx` (case-insensitive, common in practice).

## Deep Analysis

### Relevant ABNF Grammar

```
request-line = method SP request-target SP HTTP-version
method       = token
token        = 1*tchar
tchar        = "!" / "#" / "$" / "%" / "&" / "'" / "*"
             / "+" / "-" / "." / "^" / "_" / "`" / "|" / "~"
             / DIGIT / ALPHA
```

The `method` production is a `token`, which includes both uppercase and lowercase `ALPHA` characters. This means `get` is syntactically valid as a method token -- it matches the ABNF. The case-sensitivity constraint comes from the semantic layer, not the grammar.

### RFC Evidence

**RFC 9110 Section 9.1** defines method case sensitivity and its rationale:

> "The method token is case-sensitive because it might be used as a gateway to object-based systems with case-sensitive method names. By convention, standardized methods are defined in all-uppercase US-ASCII letters." -- RFC 9110 Section 9.1

**RFC 9112 Section 3.1** reinforces that the method is case-sensitive:

> "The request method is case-sensitive." -- RFC 9112 Section 3.1

**RFC 9110 Section 9.1** describes method registration:

> "The method token is case-sensitive and ought to be registered within the 'Hypertext Transfer Protocol (HTTP) Method Registry'." -- RFC 9110 Section 9.1

### Chain of Reasoning

1. The `method` ABNF production (`token`) permits lowercase letters. `get` is syntactically valid.
2. However, both RFC 9110 Section 9.1 and RFC 9112 Section 3.1 state that the method token is case-sensitive. `get` and `GET` are therefore different method tokens.
3. `GET` is a registered, standardized method. `get` is not registered in the IANA HTTP Method Registry. A server receiving `get` is receiving an unregistered method.
4. The RFC does not contain a MUST requirement to reject unrecognized methods. RFC 9110 Section 15.6.2 defines `501 Not Implemented` for methods the server does not recognize, and `405 Method Not Allowed` for methods not supported on the target resource.
5. Many servers treat methods case-insensitively as a pragmatic choice. This works because all standardized methods have unique uppercase names and there is no registered lowercase method that collides.

### Scoring Justification

**Unscored (no MUST).** The RFC defines method tokens as case-sensitive but does not mandate rejection of unrecognized (lowercase) methods with any specific behavior. A server that rejects `get` with `400`, `405`, or `501` is strictly correct. A server that accepts `get` as `GET` is being lenient but not violating a MUST requirement. Both behaviors are recorded without scoring.

### Edge Cases

- **Mixed case methods:** `Get`, `gEt`, `GEt` -- each is a distinct token under case-sensitive rules. None are registered methods.
- **WebDAV methods:** Methods like `PROPFIND` are registered in uppercase. A server handling `propfind` as `PROPFIND` may work but is technically receiving an unregistered method.
- **Custom methods:** If a server defines a custom method `get` (lowercase) for internal use, it would collide with case-insensitive handling of `GET`. This is the exact scenario the RFC warns about with "gateway to object-based systems."

## Sources

- [RFC 9110 Section 9.1](https://www.rfc-editor.org/rfc/rfc9110#section-9.1)
- [RFC 9112 Section 3.1 -- Method](https://www.rfc-editor.org/rfc/rfc9112#section-3.1)
