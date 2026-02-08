---
title: "WHITESPACE-BEFORE-HEADERS"
description: "WHITESPACE-BEFORE-HEADERS test documentation"
weight: 6
---

| | |
|---|---|
| **Test ID** | `COMP-WHITESPACE-BEFORE-HEADERS` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) |
| **Requirement** | MUST reject or ignore |
| **Expected** | `400` or close |

## What it sends

A request with whitespace (SP) before the first header line, between the request-line and the headers.

```http
GET / HTTP/1.1\r\n
 \r\n
Host: localhost:8080\r\n
\r\n
```

A line with a single space appears between the request-line and the first header.


## What the RFC says

> "A sender MUST NOT send whitespace between the start-line and the first header field." -- RFC 9112 Section 2.2

> "A recipient that receives whitespace between the start-line and the first header field MUST either reject the message as invalid or consume each whitespace-preceded line without further processing of it (i.e., ignore the entire line, along with any subsequent lines preceded by whitespace, until a properly formed header field is received or the header section is terminated)." -- RFC 9112 Section 2.2

> "Rejection or removal of invalid whitespace-preceded lines is necessary to prevent their misinterpretation by downstream recipients that might be vulnerable to request smuggling (Section 11.2) or response splitting (Section 11.1) attacks." -- RFC 9112 Section 2.2

## Why it matters

Whitespace before headers can confuse parsers about where headers begin, potentially enabling smuggling.

## Deep Analysis

### Relevant ABNF Grammar

```
HTTP-message = start-line CRLF
               *( field-line CRLF )
               CRLF
               [ message-body ]
```

The grammar transitions directly from `start-line CRLF` to `*( field-line CRLF )`. There is no provision for whitespace-only lines between the start-line and the first header field.

### RFC Evidence

**RFC 9112 Section 2.2** establishes the sender prohibition:

> "A sender MUST NOT send whitespace between the start-line and the first header field." -- RFC 9112 Section 2.2

**RFC 9112 Section 2.2** mandates the recipient behavior with two alternatives:

> "A recipient that receives whitespace between the start-line and the first header field MUST either reject the message as invalid or consume each whitespace-preceded line without further processing of it (i.e., ignore the entire line, along with any subsequent lines preceded by whitespace, until a properly formed header field is received or the header section is terminated)." -- RFC 9112 Section 2.2

**RFC 9112 Section 2.2** explains the security motivation:

> "Rejection or removal of invalid whitespace-preceded lines is necessary to prevent their misinterpretation by downstream recipients that might be vulnerable to request smuggling (Section 11.2) or response splitting (Section 11.1) attacks." -- RFC 9112 Section 2.2

### Chain of Reasoning

1. The test inserts a line with a single SP between the request-line and the first header (`Host:`).
2. This whitespace-preceded line does not match `field-line` (which requires `field-name ":" ...`), so the parser enters an error state.
3. The RFC provides two MUST-level alternatives: reject as invalid, or silently consume the offending lines.
4. If the server rejects, 400 is the expected response. If the server consumes, it should skip the whitespace line and process `Host: localhost:8080` as the first header, returning a normal response.
5. The explicit mention of request smuggling and response splitting as motivations underscores that ignoring this requirement is a security vulnerability, not merely a compliance gap.

### Scoring Justification

**Scored (MUST).** The RFC mandates one of two behaviors at the MUST level: reject or consume. Both 400 (rejection) and connection close are acceptable outcomes. A 2xx response is also acceptable if the server correctly consumed the whitespace-preceded line and processed the remaining headers normally. The `AllowConnectionClose` flag is set because rejection by closing the connection satisfies the "reject the message as invalid" alternative.

## Sources

- [RFC 9112 Section 2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2)
