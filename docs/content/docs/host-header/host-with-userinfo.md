---
title: "HOST-WITH-USERINFO"
description: "HOST-WITH-USERINFO test documentation"
weight: 4
---

| | |
|---|---|
| **Test ID** | `COMP-HOST-WITH-USERINFO` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2) |
| **Requirement** | MUST respond with 400 |
| **Expected** | `400` or close |

## What it sends

A request with `Host: user@hostname:port`.

```http
GET / HTTP/1.1\r\n
Host: user@localhost:8080\r\n
\r\n
```

The `Host` header contains a `user@` userinfo prefix.


## What the RFC says

> "A server MUST respond with a 400 (Bad Request) status code to any HTTP/1.1 request message that lacks a Host header field and to any request message that contains more than one Host header field line or a Host header field with an invalid field value." -- RFC 9112 Section 3.2

The Host header grammar is defined as:

> "Host = uri-host [ ':' port ]" -- RFC 9110 Section 7.2

No userinfo component (`user@`) is permitted in the Host grammar. A value like `user@localhost:8080` does not match `uri-host [ ":" port ]` and is therefore an invalid field value.

## Why it matters

The userinfo component (`user@`) is not part of the Host grammar. A server that accepts it may be tricked into routing requests incorrectly.

## Deep Analysis

### Relevant ABNF Grammar

```
Host     = uri-host [ ":" port ]
uri-host = <host, see [URI], Section 3.2.2>
port     = <port, see [URI], Section 3.2.3>
```

The Host header grammar is `uri-host [ ":" port ]`. Notably absent from this grammar is the `userinfo "@"` component that appears in the broader URI `authority` production:

```
; From RFC 3986:
authority = [ userinfo "@" ] host [ ":" port ]
userinfo  = *( unreserved / pct-encoded / sub-delims / ":" )
```

The Host header uses only `uri-host` and optionally `port`, deliberately excluding the `userinfo` subcomponent.

### RFC Evidence

**RFC 9110 Section 7.2** defines the Host grammar without userinfo:

> "Host = uri-host [ ':' port ]" -- RFC 9110 Section 7.2

**RFC 9112 Section 3.2** mandates rejection of invalid Host values:

> "A server MUST respond with a 400 (Bad Request) status code to any HTTP/1.1 request message that lacks a Host header field and to any request message that contains more than one Host header field line or a Host header field with an invalid field value." -- RFC 9112 Section 3.2

**RFC 9110 Section 4.2.1** prohibits userinfo in http URIs:

> "A sender MUST NOT generate an 'http' URI with an empty host identifier." -- RFC 9110 Section 4.2.1

### Chain of Reasoning

1. The test sends `Host: user@localhost:8080`. The `user@` prefix is a userinfo component.
2. The Host header grammar is `uri-host [ ":" port ]`. There is no `userinfo "@"` element in this production.
3. The `@` character is not part of `uri-host` syntax. When a parser encounters `user@localhost:8080`, the value does not match `uri-host [ ":" port ]`.
4. Since the value does not match the Host grammar, it is an "invalid field value" per RFC 9112 Section 3.2, triggering the MUST-400 requirement.
5. A server that strips the userinfo and uses only the host:port portion is performing normalization that the RFC does not authorize. The RFC says "invalid field value" triggers 400, not "normalize and proceed."

### Scoring Justification

**Scored (MUST).** The Host value `user@localhost:8080` is an invalid field value because it does not match the `Host = uri-host [ ":" port ]` grammar. RFC 9112 Section 3.2 mandates 400 for invalid Host values. Both 400 and connection close are accepted because the "invalid field value" clause is broader than the "missing Host" and "duplicate Host" clauses, and some servers may close the connection during URI parsing before generating a response.

## Sources

- [RFC 9112 Section 3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2)
- [RFC 3986 Section 3.2.1](https://www.rfc-editor.org/rfc/rfc3986#section-3.2.1)
