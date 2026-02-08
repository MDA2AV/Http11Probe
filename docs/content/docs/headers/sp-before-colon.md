---
title: "SP-BEFORE-COLON"
description: "SP-BEFORE-COLON test documentation"
weight: 1
---

| | |
|---|---|
| **Test ID** | `RFC9110-5.6.2-SP-BEFORE-COLON` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 5](https://www.rfc-editor.org/rfc/rfc9112#section-5) |
| **Requirement** | MUST |
| **Expected** | `400` |

## What it sends

A request with a space between the header field name and the colon: `Host : localhost`.

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
X-Test : value\r\n
\r\n
```

Note the space between `X-Test` and the colon.


## What the RFC says

> "No whitespace is allowed between the field name and colon. In the past, differences in the handling of such whitespace have led to security vulnerabilities in request routing and response handling. A server MUST reject, with a response status code of 400 (Bad Request), any received request message that contains whitespace between a header field name and colon." -- RFC 9112 Section 5.1

This is one of the strongest requirements in the HTTP spec -- **MUST reject with 400 specifically**. Not close, not 500 -- exactly 400.

## Why it matters

This requirement was added specifically because of real-world security vulnerabilities. When different parsers handle `Header : value` vs `Header: value` differently, attackers can craft requests that are interpreted as having different headers by different components.

The `Transfer-Encoding` smuggling variant (`Transfer-Encoding : chunked`) exploits exactly this.

## Deep Analysis

### Relevant ABNF Grammar

```
field-line   = field-name ":" OWS field-value OWS
field-name   = token
token        = 1*tchar
```

The colon immediately follows `field-name` with no intervening whitespace permitted by the grammar. The optional whitespace (OWS) is only allowed *after* the colon, between `":"` and `field-value`.

### RFC Evidence

**RFC 9112 Section 5.1** provides the definitive prohibition:

> "No whitespace is allowed between the field name and colon. In the past, differences in the handling of such whitespace have led to security vulnerabilities in request routing and response handling. A server MUST reject, with a response status code of 400 (Bad Request), any received request message that contains whitespace between a header field name and colon." -- RFC 9112 Section 5.1

**RFC 9112 Section 5.1** also mandates proxy behavior:

> "A proxy MUST remove any such whitespace from a response message before forwarding the message downstream." -- RFC 9112 Section 5.1

**RFC 9112 Section 2.2** reinforces the broader parsing principle:

> "A sender MUST NOT send whitespace between the start-line and the first header field." -- RFC 9112 Section 2.2

### Chain of Reasoning

1. The `field-line` ABNF grammar places the colon immediately after `field-name` with zero intervening characters.
2. The RFC explicitly calls out that past differences in handling this whitespace caused real security vulnerabilities.
3. The requirement is MUST reject with specifically 400 -- not close, not 500, not any other status code. This is one of the most prescriptive requirements in the entire HTTP specification.
4. The RFC chose to require 400 specifically (rather than allowing connection close as an alternative) because the security implications demand an unambiguous signal to the client.

### Scoring Justification

**Scored (MUST).** The RFC mandates exactly one acceptable server behavior: respond with 400 (Bad Request). There is no alternative disposition such as "or close the connection." Connection close alone does not satisfy this requirement because the RFC explicitly specifies the status code. A server that closes the connection without sending 400 is non-compliant.

## Sources

- [RFC 9112 Section 5 -- Field Syntax](https://www.rfc-editor.org/rfc/rfc9112#section-5)
- [RFC 9110 Section 16.3.1 -- Request Smuggling](https://www.rfc-editor.org/rfc/rfc9110#section-16.3.1)
