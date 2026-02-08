---
title: "MANY-HEADERS"
description: "MANY-HEADERS test documentation"
weight: 6
---

| | |
|---|---|
| **Test ID** | `MAL-MANY-HEADERS` |
| **Category** | Malformed Input |
| **Expected** | `400`, `431`, or close |

## What it sends

A request with 10,000 header fields.

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
X-H-0: value\r\n
X-H-1: value\r\n
X-H-2: value\r\n
... (10,000 headers total) ...\r\n
X-H-9999: value\r\n
\r\n
```


## What the RFC says

> "The 431 status code indicates that the server is unwilling to process the request because its header fields are too large. The request MAY be resubmitted after reducing the size of the request header fields." — RFC 6585 Section 5

> "It can be used both when the set of request header fields in total is too large, and when a single header field is at fault." — RFC 6585 Section 5

With 10,000 header fields, the set of request headers in total is too large. The server may respond with 431, 400, or close the connection.

## Why it matters

Servers typically allocate data structures for each header. 10,000 headers can cause excessive memory allocation, hash table collisions, or O(n^2) lookup behavior.

## Deep Analysis

### Relevant ABNF

```
HTTP-message = start-line CRLF *( field-line CRLF ) CRLF [ message-body ]
field-line   = field-name ":" OWS field-value OWS
field-name   = token
token        = 1*tchar
```

### RFC Evidence

> "The 431 status code indicates that the server is unwilling to process the request because its header fields are too large."
> -- RFC 6585 Section 5

> "It can be used both when the set of request header fields in total is too large, and when a single header field is at fault."
> -- RFC 6585 Section 5

> "Responses with the 431 status code MUST NOT be stored by a cache."
> -- RFC 6585 Section 5

### Chain of Reasoning

1. **Each header is individually valid.** Every `X-H-N: value` header conforms to the `field-line` grammar: `X-H-N` is a valid `token` (composed of `tchar` characters) and `value` is valid `field-content`.

2. **The HTTP grammar allows unlimited headers.** The `*( field-line CRLF )` production uses the `*` (zero or more) repetition operator with no upper bound. The grammar alone does not restrict the number of header fields.

3. **RFC 6585 provides the rejection mechanism.** The 431 status code was created specifically for this scenario. It applies to both the total size of all headers and to an excessive number of individual fields. With 10,000 headers, the aggregate size easily exceeds any reasonable limit.

4. **Server resource exhaustion is the concern.** Each header field requires parsing, memory allocation, and storage in internal data structures. 10,000 headers can trigger O(n) or O(n^2) behavior in hash table implementations, excessive memory allocation, and slow header lookup during request processing.

5. **400 and connection close are also acceptable.** A server may choose to respond with 400 (Bad Request) as a general rejection, or close the connection outright if it detects the header section exceeds its configured limits before reading the complete request.

## Sources

- [RFC 6585 Section 5](https://www.rfc-editor.org/rfc/rfc6585#section-5) — 431 Request Header Fields Too Large
