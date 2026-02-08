---
title: "NUL-IN-HEADER-VALUE"
description: "NUL-IN-HEADER-VALUE test documentation"
weight: 15
---

| | |
|---|---|
| **Test ID** | `MAL-NUL-IN-HEADER-VALUE` |
| **Category** | Malformed Input |
| **Expected** | `400` or close |

## What it sends

A request with a NUL byte (0x00) embedded in a header value.

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
X-Test: val\x00ue\r\n
\r\n
```

The header value contains a NUL byte (`\x00`) between `val` and `ue`.


## What the RFC says

> "Field values containing CR, LF, or NUL characters are invalid and dangerous, due to the varying ways that implementations might parse and interpret those characters; a recipient of CR, LF, or NUL within a field value MUST either reject the message or replace each of those characters with SP before further processing or forwarding of that message." — RFC 9110 Section 5.5

The field-value ABNF grammar also confirms NUL is excluded:

> `field-value = *field-content`
> `field-content = field-vchar [ 1*( SP / HTAB / field-vchar ) field-vchar ]`
> `field-vchar = VCHAR / obs-text` — RFC 9110 Section 5.5

`VCHAR` is `%x21-7E` (printable ASCII) and `obs-text` is `%x80-FF`. NUL (`0x00`) falls outside both ranges.

## Why it matters

NUL bytes are not valid in HTTP header field values. They can cause string truncation in C-based parsers, potentially hiding or injecting header content. A robust server must reject any request containing NUL bytes in headers.

## Deep Analysis

### Relevant ABNF

```
field-line    = field-name ":" OWS field-value OWS
field-value   = *field-content
field-content = field-vchar [ 1*( SP / HTAB / field-vchar ) field-vchar ]
field-vchar   = VCHAR / obs-text
VCHAR         = %x21-7E       ; visible (printing) characters
obs-text      = %x80-FF       ; obsolete text (non-ASCII bytes)
```

### RFC Evidence

> "Field values containing CR, LF, or NUL characters are invalid and dangerous, due to the varying ways that implementations might parse and interpret those characters; a recipient of CR, LF, or NUL within a field value MUST either reject the message or replace each of those characters with SP before further processing or forwarding of that message."
> -- RFC 9110 Section 5.5

> `field-vchar = VCHAR / obs-text`
> -- RFC 9110 Section 5.5

> "When a server listening only for HTTP request messages...receives a sequence of octets that does not match the HTTP-message grammar aside from the robustness exceptions listed above, the server SHOULD respond with a 400 (Bad Request) response and close the connection."
> -- RFC 9112 Section 2.2

### Chain of Reasoning

1. **NUL (`0x00`) falls outside all valid field-value ranges.** The `field-vchar` production covers `VCHAR` (`%x21-7E`) and `obs-text` (`%x80-FF`). The only other characters allowed within `field-content` are `SP` (`%x20`) and `HTAB` (`%x09`). NUL (`%x00`) is below all of these ranges and matches no alternative in the grammar.

2. **The RFC uses MUST-level language.** RFC 9110 Section 5.5 states that a recipient "MUST either reject the message or replace each of those characters with SP." This is not a SHOULD -- it is an absolute requirement. There is no third option of silently accepting the NUL.

3. **Rejection is the safer choice.** While the RFC permits replacement with SP as an alternative to rejection, replacement changes the semantics of the header value. For a server processing incoming requests, rejecting with 400 is the more conservative and secure response, as it avoids the risk of processing altered header values whose original intent was to exploit parser inconsistencies.

4. **NUL is explicitly called out as "dangerous."** The RFC singles out CR, LF, and NUL by name as "invalid and dangerous" due to "the varying ways that implementations might parse and interpret those characters." This language reflects real-world attacks where NUL bytes cause C-based string functions to truncate values, potentially hiding malicious content after the NUL.

5. **The grammar violation is independent of the semantic rule.** Even without the explicit MUST in Section 5.5, the NUL byte fails the `field-vchar` grammar, making the entire `field-line` syntactically invalid under the HTTP-message grammar.

## Sources

- [RFC 9110 Section 5.5](https://www.rfc-editor.org/rfc/rfc9110#section-5.5) — field values and prohibited characters
