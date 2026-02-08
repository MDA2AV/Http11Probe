---
title: "HOST-WITH-PATH"
description: "HOST-WITH-PATH test documentation"
weight: 5
---

| | |
|---|---|
| **Test ID** | `COMP-HOST-WITH-PATH` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2) |
| **Requirement** | MUST respond with 400 |
| **Expected** | `400` or close |

## What it sends

A request with `Host: hostname:port/path`.

```http
GET / HTTP/1.1\r\n
Host: localhost:8080/path\r\n
\r\n
```

The `Host` header includes a `/path` component.


## What the RFC says

> "A server MUST respond with a 400 (Bad Request) status code to any HTTP/1.1 request message that lacks a Host header field and to any request message that contains more than one Host header field line or a Host header field with an invalid field value." -- RFC 9112 Section 3.2

The Host header grammar is defined as:

> "Host = uri-host [ ':' port ]" -- RFC 9110 Section 7.2

No path component is permitted. A value like `localhost:8080/path` does not match `uri-host [ ":" port ]` and is therefore an invalid field value.

## Why it matters

A path in the Host header is a clear sign of manipulation. If a reverse proxy uses the Host to route, a path component could alter routing.

## Deep Analysis

### Relevant ABNF Grammar

```
Host     = uri-host [ ":" port ]
uri-host = <host, see [URI], Section 3.2.2>
port     = *DIGIT
```

The Host grammar terminates after the optional `port` component. There is no provision for a path (`/` followed by path segments) or any other URI component after the port. The `/` character is not valid in `uri-host` or `port`.

### RFC Evidence

**RFC 9110 Section 7.2** defines the Host grammar:

> "Host = uri-host [ ':' port ]" -- RFC 9110 Section 7.2

**RFC 9112 Section 3.2** mandates rejection of invalid Host values:

> "A server MUST respond with a 400 (Bad Request) status code to any HTTP/1.1 request message that lacks a Host header field and to any request message that contains more than one Host header field line or a Host header field with an invalid field value." -- RFC 9112 Section 3.2

**RFC 9112 Section 3.2** describes how the Host relates to the request-target:

> "When a proxy receives a request with an absolute-form of request-target, the proxy MUST ignore the received Host header field (if any) and instead replace it with the host information of the request-target." -- RFC 9112 Section 3.2

### Chain of Reasoning

1. The test sends `Host: localhost:8080/path`. The `/path` component follows the port.
2. The Host grammar is `uri-host [ ":" port ]`. The `port` production is `*DIGIT` (zero or more digits). After parsing `8080` as the port, the `/path` remainder does not match any part of the Host grammar.
3. Since the value does not conform to the Host grammar, it is an "invalid field value" per RFC 9112 Section 3.2, triggering the MUST-400 requirement.
4. A path component in the Host header is a strong indicator of manipulation. In a reverse proxy configuration, the Host header is used for routing decisions. If a proxy interprets the path component as part of the backend route, the attacker can redirect the request to an unintended backend path.
5. Some servers may attempt to parse the value as a full URI, extracting just the host and port. This normalization is not authorized by the RFC and bypasses the security protection that the grammar validation provides.

### Scoring Justification

**Scored (MUST).** The Host value `localhost:8080/path` is an invalid field value because it does not match the `Host = uri-host [ ":" port ]` grammar. RFC 9112 Section 3.2 mandates 400. Both 400 and connection close are accepted because the invalid value may cause parsing failures before the server can generate a response.

## Sources

- [RFC 9112 Section 3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2)
