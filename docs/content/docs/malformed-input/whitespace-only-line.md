---
title: "WHITESPACE-ONLY-LINE"
description: "WHITESPACE-ONLY-LINE test documentation"
weight: 14
---

| | |
|---|---|
| **Test ID** | `MAL-WHITESPACE-ONLY-LINE` |
| **Category** | Malformed Input |
| **Expected** | `400`, close, or timeout |

## What it sends

A line consisting only of spaces and tabs -- no method, URI, or version.

```http
   \r\n
\r\n
```

The request-line consists of three spaces followed by CRLF — no method, target, or version.


## What the RFC says

> "In the interest of robustness, a server that is expecting to receive and parse a request-line SHOULD ignore at least one empty line (CRLF) received prior to the request-line." — RFC 9112 Section 2.2

An empty line is defined as a bare CRLF. A line containing spaces is not empty -- it is a non-empty sequence of octets that does not match the request-line grammar:

> `request-line = method SP request-target SP HTTP-version` — RFC 9112 Section 3

Three spaces followed by CRLF cannot be parsed as `method SP request-target SP HTTP-version`.

> "When a server listening only for HTTP request messages...receives a sequence of octets that does not match the HTTP-message grammar...the server SHOULD respond with a 400 (Bad Request) response and close the connection." — RFC 9112 Section 2.2

## Why it matters

A line of only whitespace is neither empty (CRLF) nor a valid request-line. If a server treats whitespace-only lines as empty lines and ignores them, it may be tricked into accepting subsequent malicious data as a valid request.

## Deep Analysis

### Relevant ABNF

```
HTTP-message   = start-line CRLF *( field-line CRLF ) CRLF [ message-body ]
start-line     = request-line / status-line
request-line   = method SP request-target SP HTTP-version
method         = token
token          = 1*tchar
tchar          = "!" / "#" / "$" / "%" / "&" / "'" / "*"
               / "+" / "-" / "." / "^" / "_" / "`" / "|" / "~"
               / DIGIT / ALPHA
```

### RFC Evidence

> "In the interest of robustness, a server that is expecting to receive and parse a request-line SHOULD ignore at least one empty line (CRLF) received prior to the request-line."
> -- RFC 9112 Section 2.2

> `request-line = method SP request-target SP HTTP-version`
> -- RFC 9112 Section 3

> "When a server listening only for HTTP request messages...receives a sequence of octets that does not match the HTTP-message grammar aside from the robustness exceptions listed above, the server SHOULD respond with a 400 (Bad Request) response and close the connection."
> -- RFC 9112 Section 2.2

### Chain of Reasoning

1. **A whitespace-only line is not an empty line.** The robustness exception in RFC 9112 Section 2.2 allows ignoring "at least one empty line (CRLF)" before the request-line. An empty line is precisely `CRLF` -- zero content octets followed by the line terminator. A line containing `SP SP SP CRLF` has three content octets; it is not empty.

2. **The line cannot be parsed as a `request-line`.** The grammar requires `method SP request-target SP HTTP-version`. The `method` production is `token = 1*tchar`, requiring at least one `tchar` character. `SP` (`0x20`) is not a `tchar` (the lowest `tchar` value is `0x21`). The parser cannot extract a `method` from a sequence of spaces, so the line fails at the very first production.

3. **No robustness exception applies.** The only robustness exception in RFC 9112 Section 2.2 is for empty CRLF lines. There is no exception for whitespace-only lines, lines containing only control characters, or any other non-empty, non-matching input. The whitespace-only line falls squarely into the "sequence of octets that does not match the HTTP-message grammar" clause.

4. **The 400 rejection rule triggers.** RFC 9112 Section 2.2 instructs the server to "respond with a 400 (Bad Request) response and close the connection" for input that does not match the grammar. This is a SHOULD-level requirement.

5. **Treating whitespace as empty is a security risk.** If a server ignores whitespace-only lines as if they were empty CRLF lines, an attacker could use this behavior to desynchronize request boundaries in a pipeline. For example, inserting `SP SP SP CRLF` between pipelined requests might cause one server to skip the line while another treats it as the start of a malformed request, leading to request smuggling.

## Sources

- [RFC 9112 Section 2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) — message parsing robustness
- [RFC 9112 Section 3](https://www.rfc-editor.org/rfc/rfc9112#section-3) — request-line grammar
