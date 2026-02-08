---
title: "FRAGMENT-IN-TARGET"
description: "FRAGMENT-IN-TARGET test documentation"
weight: 3
---

| | |
|---|---|
| **Test ID** | `RFC9112-3.2-FRAGMENT-IN-TARGET` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2) |
| **Requirement** | SHOULD |
| **Expected** | `400` = Pass; `2xx` = Warn |

## What it sends

A request with a fragment identifier in the URI: `GET /path#frag HTTP/1.1`.

```http
GET /path#frag HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

The origin-form of request-target is defined as:

> "origin-form = absolute-path [ '?' query ]" -- RFC 9112 §3.2.1

There is no fragment component in this grammar. The `#` character and anything after it are not part of any valid request-target form. RFC 9110 confirms that fragments are stripped before transmission:

> "The target URI excludes the reference's fragment component, if any, since fragment identifiers are reserved for client-side processing." -- RFC 9110 §7.1

> "The fragment identifier component is not part of the scheme definition for a URI scheme (see Section 4.3 of [URI]), thus does not appear in the ABNF definitions for the 'http' and 'https' URI schemes." -- RFC 9110 §4.2.5

Since the request-line doesn't match any valid form, it is an invalid request-line:

> "Recipients of an invalid request-line SHOULD respond with either a 400 (Bad Request) error or a 301 (Moved Permanently) redirect with the request-target properly encoded." — RFC 9112 Section 3

This is a SHOULD, not a MUST -- servers that strip the fragment and process the path are not violating a mandatory requirement.

## Why it matters

Fragments are a client-side concept used to reference a position within a document. They should never appear on the wire. A server that silently strips fragments may process a different resource than what the client intended, though the practical security risk is low.

**Pass:** Server rejects with `400` (strict parsing).
**Warn:** Server returns `2xx` (likely strips the fragment and processes `/path`).

## Deep Analysis

### Relevant ABNF Grammar

```
request-line   = method SP request-target SP HTTP-version
request-target = origin-form / absolute-form / authority-form / asterisk-form
origin-form    = absolute-path [ "?" query ]
absolute-path  = 1*( "/" segment )
query          = *( pchar / "/" / "?" )
```

The `origin-form` production allows only an absolute path and an optional query component. There is no fragment production (`"#" fragment`) in the grammar. The `#` character is not a valid character in `absolute-path`, `query`, or any other component of `request-target`.

### RFC Evidence

**RFC 9112 Section 3.2.1** defines origin-form without fragments:

> "origin-form = absolute-path [ '?' query ]" -- RFC 9112 Section 3.2.1

**RFC 9110 Section 4.2.5** confirms fragments are excluded from the URI scheme definition:

> "The fragment identifier component is not part of the scheme definition for a URI scheme (see Section 4.3 of [URI]), thus does not appear in the ABNF definitions for the 'http' and 'https' URI schemes." -- RFC 9110 Section 4.2.5

**RFC 9110 Section 7.1** explicitly states fragments are stripped before transmission:

> "The target URI excludes the reference's fragment component, if any, since fragment identifiers are reserved for client-side processing." -- RFC 9110 Section 7.1

**RFC 9112 Section 3** covers invalid request-line handling:

> "Recipients of an invalid request-line SHOULD respond with either a 400 (Bad Request) error or a 301 (Moved Permanently) redirect with the request-target properly encoded." -- RFC 9112 Section 3

### Chain of Reasoning

1. The origin-form ABNF is `absolute-path [ "?" query ]`. There is no fragment component in this grammar.
2. The `#` character does not appear in the `pchar`, `query`, or `absolute-path` productions from RFC 3986. It is a delimiter reserved exclusively for fragment identification.
3. RFC 9110 Section 7.1 confirms that the target URI "excludes the reference's fragment component" because "fragment identifiers are reserved for client-side processing." Fragments should never be transmitted on the wire.
4. A request-target of `/path#frag` does not match `origin-form` (or any other valid form), making it an invalid request-line per Section 3.
5. The handling guidance is SHOULD (not MUST): servers SHOULD respond with 400 or 301. A server that strips the fragment and processes `/path` is not violating a mandatory requirement.

### Scoring Justification

**Scored (SHOULD) -- Pass/Warn.** The RFC uses SHOULD, not MUST, for invalid request-line handling. A server that rejects with `400` demonstrates strict, correct parsing and earns a Pass. A server that returns `2xx` (likely stripping the fragment) is not violating a mandatory requirement but is being lenient with invalid input, earning a Warn. Neither outcome is a Fail.

### Edge Cases

- **Fragment with query:** `GET /path?q=1#frag HTTP/1.1` -- the `#frag` portion is still invalid in the request-target. The server may strip both the fragment and process `/path?q=1`, or reject entirely.
- **Empty fragment:** `GET /path# HTTP/1.1` -- the `#` alone (with no fragment text) is still invalid since `#` is not part of the origin-form grammar.
- **Percent-encoded hash:** `GET /path%23frag HTTP/1.1` -- `%23` is the percent-encoding of `#`. This is valid in the path and should be processed as the literal path `/path#frag` after decoding. The server should NOT treat this as a fragment.

## Sources

- [RFC 9112 Section 3.2 -- origin-form](https://www.rfc-editor.org/rfc/rfc9112#section-3.2)
- [RFC 9112 Section 3 -- Request Line](https://www.rfc-editor.org/rfc/rfc9112#section-3)
- [RFC 9110 Section 4.1 -- URI References](https://www.rfc-editor.org/rfc/rfc9110#section-4.1)
