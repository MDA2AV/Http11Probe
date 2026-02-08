---
title: "NON-ASCII-HEADER-NAME"
description: "NON-ASCII-HEADER-NAME test documentation"
weight: 9
---

| | |
|---|---|
| **Test ID** | `MAL-NON-ASCII-HEADER-NAME` |
| **Category** | Malformed Input |
| **Expected** | `400` or close |

## What it sends

A request with non-ASCII bytes (`\x80`-`\xFF`) in a header field name.

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
X-T\xC3\xABst: value\r\n
\r\n
```

The header name `X-Tëst` contains UTF-8 encoded `ë` (`\xC3\xAB`) — non-ASCII bytes in a header name.


## What the RFC says

> `field-name = token` — RFC 9110 Section 5.1

> `token = 1*tchar` — RFC 9110 Section 5.6.2

> `tchar = "!" / "#" / "$" / "%" / "&" / "'" / "*" / "+" / "-" / "." / "^" / "_" / "`" / "|" / "~" / DIGIT / ALPHA` — RFC 9110 Section 5.6.2

The `tchar` production is restricted to a specific set of ASCII characters. The bytes `0xC3 0xAB` (UTF-8 `e` with diaeresis) are outside this set, making the header name invalid.

> "When a server listening only for HTTP request messages...receives a sequence of octets that does not match the HTTP-message grammar...the server SHOULD respond with a 400 (Bad Request) response and close the connection." — RFC 9112 Section 2.2

## Deep Analysis

### Relevant ABNF

```
field-line  = field-name ":" OWS field-value OWS
field-name  = token
token       = 1*tchar
tchar       = "!" / "#" / "$" / "%" / "&" / "'" / "*"
            / "+" / "-" / "." / "^" / "_" / "`" / "|" / "~"
            / DIGIT / ALPHA
            ; ALPHA = %x41-5A / %x61-7A  (ASCII letters only)
            ; DIGIT = %x30-39            (ASCII digits only)
```

### RFC Evidence

> "A field name is a token."
> -- RFC 9110 Section 5.1

> `token = 1*tchar`
> `tchar = "!" / "#" / "$" / "%" / "&" / "'" / "*" / "+" / "-" / "." / "^" / "_" / "`" / "|" / "~" / DIGIT / ALPHA`
> -- RFC 9110 Section 5.6.2

> "When a server listening only for HTTP request messages...receives a sequence of octets that does not match the HTTP-message grammar aside from the robustness exceptions listed above, the server SHOULD respond with a 400 (Bad Request) response and close the connection."
> -- RFC 9112 Section 2.2

### Chain of Reasoning

1. **The `tchar` set is exclusively ASCII.** The `ALPHA` rule in ABNF core rules (RFC 5234) covers only `%x41-5A` (A-Z) and `%x61-7A` (a-z). The `DIGIT` rule covers only `%x30-39` (0-9). The explicitly listed special characters are all ASCII. No byte above `0x7E` is included.

2. **The header name contains bytes `0xC3 0xAB`.** These two bytes are the UTF-8 encoding of U+00EB (Latin small letter e with diaeresis). Both `0xC3` and `0xAB` fall outside the `tchar` character set, which only spans a subset of `%x21-7E`.

3. **The grammar violation is unambiguous.** Since `field-name = token = 1*tchar`, and `0xC3` is not a `tchar`, the parser fails at the first non-ASCII byte. The entire `field-line` does not match the HTTP-message grammar.

4. **The SHOULD-level rejection applies.** RFC 9112 Section 2.2 instructs servers to respond with 400 and close the connection when receiving octets that do not match the HTTP-message grammar. This is a direct, unambiguous application of that rule.

5. **No autocorrection is appropriate.** RFC 9112 Section 3 states that "A recipient SHOULD NOT attempt to autocorrect and then process the request without a redirect, since the invalid request-line might be deliberately crafted to bypass security filters." The same principle extends to header field parsing -- silently accepting non-ASCII header names could enable filter bypasses.

## Sources

- [RFC 9110 Section 5.1](https://www.rfc-editor.org/rfc/rfc9110#section-5.1) — field-name = token
- [RFC 9110 Section 5.6.2](https://www.rfc-editor.org/rfc/rfc9110#section-5.6.2) — token and tchar ABNF
- [RFC 9112 Section 2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) — rejection of invalid messages
