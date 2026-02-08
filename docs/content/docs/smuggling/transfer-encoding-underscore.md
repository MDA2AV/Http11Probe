---
title: "TRANSFER_ENCODING"
description: "TRANSFER_ENCODING test documentation"
weight: 30
---

| | |
|---|---|
| **Test ID** | `SMUG-TRANSFER_ENCODING` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 Section 6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **Requirement** | Unscored |
| **Expected** | `400` or `2xx` |

## What it sends

`Transfer_Encoding: chunked` (underscore instead of hyphen) with `Content-Length: 5`.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer_Encoding: chunked\r\n
Content-Length: 5\r\n
\r\n
hello
```

Note `Transfer_Encoding` with an underscore instead of a hyphen.


## What the RFC says

> "field-name = token" -- RFC 9110 Section 5.1

> "token = 1\*tchar" where "tchar = '!' / '#' / '$' / '%' / '&' / ''' / '\*' / '+' / '-' / '.' / '^' / '\_' / '\`' / '|' / '~' / DIGIT / ALPHA" -- RFC 9110 Section 5.6.2

The underscore character (`_`) is explicitly included in the `tchar` production, making `Transfer_Encoding` a syntactically valid field name (token). However, it is not the registered `Transfer-Encoding` header field and has no defined semantics. A server receiving this header should treat it as an unknown custom header, not as Transfer-Encoding.

## Why this test is unscored

`Transfer_Encoding` is a valid token but not the `Transfer-Encoding` header. The server is correct to ignore it as an unknown header (resulting in `2xx` using Content-Length for framing) or to reject it with `400` (strict policy). The test is unscored because neither response is wrong per the RFC.

**Pass:** Server rejects with `400` (strict, safe).
**Warn:** Server accepts and responds `2xx` (treats it as unknown header, uses CL).

## Why it matters

Some proxies normalize underscores to hyphens (notably certain Python/Ruby frameworks like Gunicorn and WEBrick), making this a known smuggling vector. If a front-end passes `Transfer_Encoding` through as-is but a back-end normalizes the underscore to a hyphen and processes it as `Transfer-Encoding: chunked`, the two parsers disagree on message framing.

## Deep Analysis

### ABNF

```
field-line   = field-name ":" OWS field-value OWS  ; RFC 9112 §5
field-name   = token                                ; RFC 9110 §5.1
token        = 1*tchar                              ; RFC 9110 §5.6.2
tchar        = "!" / "#" / "$" / "%" / "&" / "'" / "*"
               / "+" / "-" / "." / "^" / "_" / "`" / "|" / "~"
               / DIGIT / ALPHA
```

The underscore (`_`) is explicitly listed as a valid `tchar` character. Therefore, `Transfer_Encoding` is a syntactically valid `token` and a valid `field-name`. However, HTTP header field names are matched by their registered names, and the registered name is `Transfer-Encoding` (with a hyphen). `Transfer_Encoding` is a completely different, unregistered header field name.

### RFC Evidence

> "Each field line consists of a case-insensitive field name followed by a colon (':'), optional leading whitespace, the field line value, and optional trailing whitespace." -- RFC 9112 §5

> "A server MAY reject a request that contains both Content-Length and Transfer-Encoding or process such a request in accordance with the Transfer-Encoding alone. Regardless, the server MUST close the connection after responding to such a request to avoid the potential attacks." -- RFC 9112 §6.1

> "If a message is received with both a Transfer-Encoding and a Content-Length header field, the Transfer-Encoding overrides the Content-Length. Such a message might indicate an attempt to perform request smuggling or response splitting and ought to be handled as an error." -- RFC 9112 §6.3

### Chain of Reasoning

1. The test sends `Transfer_Encoding: chunked` (underscore) alongside `Content-Length: 5`.
2. The header field name `Transfer_Encoding` is syntactically valid -- the underscore is a permitted `tchar`. However, it is **not** the registered `Transfer-Encoding` header.
3. A compliant server should treat `Transfer_Encoding` as an unknown/custom header with no defined semantics. Since the actual `Transfer-Encoding` header is absent, the server should use `Content-Length: 5` for framing and respond normally with `2xx`.
4. The danger arises from server or proxy implementations that normalize header field names. Some frameworks (notably Python's WSGI/CGI interface, Ruby's WEBrick, and Gunicorn) convert header names to uppercase with underscores replacing hyphens (`HTTP_TRANSFER_ENCODING`). If a back-end framework reverses this normalization and converts underscores back to hyphens, `Transfer_Encoding` becomes `Transfer-Encoding`.
5. If the back-end sees `Transfer-Encoding: chunked` (after normalization) while the front-end saw `Transfer_Encoding` (an unknown header) and used Content-Length, the two parsers disagree on message framing.
6. Since `Transfer_Encoding` is not `Transfer-Encoding`, the CL/TE dual-header rules in RFC 9112 section 6.3 do not technically apply to a compliant server. The server simply has `Content-Length: 5` and an unknown header.

### Scored / Unscored Justification

This test is **unscored** because `Transfer_Encoding` is a syntactically valid but unregistered header name. The RFC does not mandate any specific behavior for unknown headers beyond ignoring them. A server that treats it as unknown and uses Content-Length is correct. A server that rejects with `400` is being defensively strict. Neither behavior violates the specification.

- **Pass (400):** Strict rejection -- the server flags the suspicious header name.
- **Warn (2xx):** Correct behavior -- the server treated `Transfer_Encoding` as an unknown header and used Content-Length.

### Smuggling Attack Scenarios

- **Underscore-to-Hyphen Normalization Desync:** A front-end proxy passes `Transfer_Encoding: chunked` through as-is (an unknown header). The back-end, running a framework that normalizes underscores to hyphens (e.g., Gunicorn behind Nginx), sees `Transfer-Encoding: chunked` and uses chunked framing. The front-end used Content-Length; the back-end uses chunked encoding. The attacker injects a second request in the body that only the back-end parses.
- **CGI/WSGI Environment Variable Poisoning:** In CGI and WSGI environments, headers are converted to environment variables like `HTTP_TRANSFER_ENCODING`. Some reverse mappings do not distinguish between `Transfer-Encoding` (original) and `Transfer_Encoding` (underscore variant) because both map to the same environment variable. An attacker can use the underscore variant to inject a Transfer-Encoding header that the front-end never intended to forward.
- **Double Header Injection:** An attacker sends both `Transfer-Encoding: chunked` and `Transfer_Encoding: identity`. A front-end proxy processes the legitimate `Transfer-Encoding: chunked`. A back-end that normalizes underscores ends up with two `Transfer-Encoding` headers with conflicting values, creating an additional layer of ambiguity in transfer coding selection.

## Sources

- [RFC 9110 §5.1](https://www.rfc-editor.org/rfc/rfc9110#section-5.1)
- [RFC 9110 §5.6.2](https://www.rfc-editor.org/rfc/rfc9110#section-5.6.2)
- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
