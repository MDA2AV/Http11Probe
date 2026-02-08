---
title: "ASTERISK-WITH-GET"
description: "ASTERISK-WITH-GET test documentation"
weight: 6
---

| | |
|---|---|
| **Test ID** | `COMP-ASTERISK-WITH-GET` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 3.2.4](https://www.rfc-editor.org/rfc/rfc9112#section-3.2.4) |
| **Requirement** | MUST only be used with OPTIONS |
| **Expected** | `400` or close |

## What it sends

`GET * HTTP/1.1` — the asterisk-form request-target with a non-OPTIONS method.

```http
GET * HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```


## What the RFC says

> "The 'asterisk-form' of request-target is only used for a server-wide OPTIONS request." -- RFC 9112 §3.2.4

> "When a client wishes to request OPTIONS for the server as a whole, as opposed to a specific named resource of that server, the client MUST send only '*' (%x2A) as the request-target." -- RFC 9112 §3.2.4

Since the asterisk-form is defined exclusively for OPTIONS, using it with GET produces an invalid request-target for that method.

## Why it matters

Asterisk-form with any method other than OPTIONS is invalid. Accepting it could lead to unexpected server behavior.

## Deep Analysis

### Relevant ABNF Grammar

```
request-line   = method SP request-target SP HTTP-version
request-target = origin-form / absolute-form / authority-form / asterisk-form
asterisk-form  = "*"
```

The `asterisk-form` production is the literal character `*` (%x2A). The grammar allows it as one of four valid request-target forms, but its usage is constrained by prose in the RFC to a single method.

### RFC Evidence

**RFC 9112 Section 3.2.4** restricts asterisk-form to OPTIONS only:

> "The 'asterisk-form' of request-target is only used for a server-wide OPTIONS request." -- RFC 9112 Section 3.2.4

**RFC 9112 Section 3.2.4** further reinforces the constraint with a client MUST:

> "When a client wishes to request OPTIONS for the server as a whole, as opposed to a specific named resource of that server, the client MUST send only '*' (%x2A) as the request-target." -- RFC 9112 Section 3.2.4

**RFC 9112 Section 3** defines the invalid request-line handling:

> "Recipients of an invalid request-line SHOULD respond with either a 400 (Bad Request) error or a 301 (Moved Permanently) redirect with the request-target properly encoded." -- RFC 9112 Section 3

### Chain of Reasoning

1. The ABNF allows `asterisk-form` as a valid `request-target`, but the RFC prose restricts it: "only used for a server-wide OPTIONS request."
2. A request like `GET * HTTP/1.1` uses the asterisk-form with a method other than OPTIONS, violating this restriction.
3. Since the request-target form is invalid for the given method, the request-line is effectively malformed from the server's perspective.
4. The server SHOULD respond with `400` per the invalid request-line guidance in Section 3, or may close the connection.
5. The "only used for" phrasing is a definitional constraint rather than a MUST requirement, but a server that accepts `GET *` is processing a request with no meaningful target resource.

### Scoring Justification

**Scored (MUST).** The asterisk-form is definitionally restricted to OPTIONS. Sending `GET *` produces a request-target that has no valid interpretation under any method other than OPTIONS. A server that accepts this is processing a syntactically invalid request, which could lead to unpredictable behavior. The test expects `400` or connection close.

### Edge Cases

- **HEAD * HTTP/1.1:** HEAD is semantically identical to GET without a body. `HEAD *` is equally invalid since asterisk-form is restricted to OPTIONS.
- **POST * HTTP/1.1:** Even more problematic since POST with an asterisk target has no defined resource to act upon.
- **OPTIONS * HTTP/1.1:** This is the valid case -- the server should respond with `200` and applicable Allow/capability headers.

## Sources

- [RFC 9112 Section 3.2.4](https://www.rfc-editor.org/rfc/rfc9112#section-3.2.4)
