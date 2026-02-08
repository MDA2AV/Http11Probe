---
title: "HEADER-NO-COLON"
description: "HEADER-NO-COLON test documentation"
weight: 5
---

| | |
|---|---|
| **Test ID** | `RFC9112-5-HEADER-NO-COLON` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 5](https://www.rfc-editor.org/rfc/rfc9112#section-5) |
| **Requirement** | Implicit MUST (grammar violation) |
| **Expected** | `400` or close |

## What it sends

A header line with no colon: `InvalidHeaderNoColon`.

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
NoColonHere\r\n
\r\n
```

A header line without any colon separator.


## What the RFC says

> "field-line = field-name ':' OWS field-value OWS" -- RFC 9112 Section 5

> "Each field line consists of a case-insensitive field name followed by a colon (':'), optional leading whitespace, the field line value, and optional trailing whitespace." -- RFC 9112 Section 5

A line without a colon does not match the `field-line` grammar. It could be misinterpreted as a continuation line, a new request, or garbage -- any of which is dangerous.

## Why it matters

A header line without a colon is structurally ambiguous. Some parsers may treat it as a malformed header and discard it, while others may interpret it as a continuation of the previous header value (obs-fold behavior) or even as the start of a new request. This parsing disagreement between components in a request chain is exactly the condition attackers exploit for request smuggling.

## Deep Analysis

### Relevant ABNF Grammar

```
HTTP-message = start-line CRLF
               *( field-line CRLF )
               CRLF
               [ message-body ]
field-line   = field-name ":" OWS field-value OWS
```

The `field-line` production requires a colon as a mandatory separator between `field-name` and `field-value`. Without a colon, the line cannot be parsed as a field-line.

### RFC Evidence

**RFC 9112 Section 5** describes the structure clearly:

> "Each field line consists of a case-insensitive field name followed by a colon (':'), optional leading whitespace, the field line value, and optional trailing whitespace." -- RFC 9112 Section 5

**RFC 9112 Section 5** provides the formal grammar:

> "field-line = field-name ':' OWS field-value OWS" -- RFC 9112 Section 5

**RFC 9112 Section 2.2** establishes the general error handling principle:

> "A recipient that receives whitespace between the start-line and the first header field MUST either reject the message as invalid or consume each whitespace-preceded line without further processing of it." -- RFC 9112 Section 2.2

### Chain of Reasoning

1. A line like `NoColonHere` in the header section does not contain a colon, so it cannot match the `field-line` production.
2. Without a colon, the parser cannot determine where the field name ends and the field value begins.
3. The line could be misinterpreted in multiple dangerous ways: as a continuation of the previous header (similar to obs-fold), as the start of a new request (if it looks like a request-line), or as part of the message body.
4. This ambiguity is precisely the kind of parsing disagreement that enables request smuggling. If one component treats it as a header and another treats it as body content, the message boundaries diverge.
5. RFC 9112 Section 2.2 states that when a server receives a malformed request, it "SHOULD respond with a 400 (Bad Request) response and close the connection."

### Scoring Justification

**Scored (implicit MUST, grammar violation).** The colon is a mandatory syntactic element in `field-line`. Its absence is an unambiguous grammar violation. Both 400 and connection close are acceptable responses because no specific status code is mandated for generic parse failures. The `AllowConnectionClose` flag is set because the message structure is so fundamentally broken that the server may not be able to generate a well-formed response.

## Sources

- [RFC 9112 Section 5 -- Field Syntax](https://www.rfc-editor.org/rfc/rfc9112#section-5)
