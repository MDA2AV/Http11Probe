---
title: "CONTROL-CHARS-HEADER"
description: "CONTROL-CHARS-HEADER test documentation"
weight: 8
---

| | |
|---|---|
| **Test ID** | `MAL-CONTROL-CHARS-HEADER` |
| **Category** | Malformed Input |
| **RFC** | [RFC 9110 Section 5.5](https://www.rfc-editor.org/rfc/rfc9110#section-5.5) |
| **Expected** | `400` or close |

## What it sends

A request with control characters (`\x01`-`\x08`, `\x0E`-`\x1F`) in a header field value.

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
X-Test: abc\x07\x08\x0Bdef\r\n
\r\n
```

The header value contains BEL (`\x07`), BS (`\x08`), and VT (`\x0B`) control characters.


## What the RFC says

The field-value ABNF permits only visible ASCII, SP, HTAB, and obs-text:

> `field-value = *field-content`
> `field-content = field-vchar [ 1*( SP / HTAB / field-vchar ) field-vchar ]`
> `field-vchar = VCHAR / obs-text` — RFC 9110 Section 5.5

`VCHAR` is `%x21-7E` (printable ASCII). Control characters like BEL (`0x07`), BS (`0x08`), and VT (`0x0B`) are in the `%x00-1F` range and fall outside `VCHAR`, `SP` (`0x20`), and `HTAB` (`0x09`).

> "Field values containing other CTL characters are also invalid; however, recipients MAY retain such characters for the sake of robustness when they appear within a safe context (e.g., an application-specific quoted string that will not be processed by any downstream HTTP parser)." — RFC 9110 Section 5.5

While the RFC allows recipients some leniency with non-NUL/CR/LF control characters, they are still grammatically invalid.

## Deep Analysis

### ABNF violation

The field-value grammar strictly defines which characters are permitted:

```
field-value   = *field-content
field-content = field-vchar
                [ 1*( SP / HTAB / field-vchar ) field-vchar ]
field-vchar   = VCHAR / obs-text
VCHAR         = %x21-7E
obs-text      = %x80-FF
SP             = %x20
HTAB           = %x09
```

The allowed byte ranges in a field-value are:
- `HTAB` = `0x09`
- `SP` = `0x20`
- `VCHAR` = `0x21-7E` (printable ASCII)
- `obs-text` = `0x80-FF` (high bytes, for legacy compatibility)

Control characters BEL (`0x07`), BS (`0x08`), and VT (`0x0B`) fall in the range `0x00-0x08` and `0x0A-0x1F` (excluding `HTAB` at `0x09`). They are not `VCHAR`, not `SP`, not `HTAB`, and not `obs-text`. They do not match any production in the `field-value` grammar.

### RFC evidence

> "field-value = *field-content" -- RFC 9110 Section 5.5

> "field-vchar = VCHAR / obs-text" -- RFC 9110 Section 5.5

> "Field values containing CR, LF, or NUL characters are invalid and dangerous, due to the varying ways that implementations might parse and interpret those characters; a recipient of field content containing those characters is typically unable to handle them properly and MUST either reject the message or replace each of those characters with SP before further processing or forwarding of that message." -- RFC 9110 Section 5.5

> "Field values containing other CTL characters are also invalid; however, recipients MAY retain such characters for the sake of robustness when they appear within a safe context (e.g., an application-specific quoted string that will not be processed by any downstream HTTP parser)." -- RFC 9110 Section 5.5

> "When a server listening only for HTTP request messages, or processing what appears from the start-line to be an HTTP request message, receives a sequence of octets that does not match the HTTP-message grammar aside from the robustness exceptions listed above, the server SHOULD respond with a 400 (Bad Request) response and close the connection." -- RFC 9112 Section 2.2

The RFC makes a distinction: CR/LF/NUL are "invalid and dangerous" with a MUST reject-or-replace requirement, while other CTL characters are "also invalid" but with a MAY-retain exception for safe contexts. For a header like `X-Test` that has no application-specific quoted-string semantics, there is no "safe context" -- rejection is the appropriate response.

### Chain of reasoning

1. The server receives `X-Test: abc\x07\x08\x0Bdef\r\n`.
2. It parses the field-value: `abc` matches `field-vchar` characters (all `VCHAR`).
3. It then encounters `\x07` (BEL). This byte is `0x07`, which is not `VCHAR` (`0x21-7E`), not `SP` (`0x20`), not `HTAB` (`0x09`), and not `obs-text` (`0x80-FF`).
4. The `field-content` production cannot match `\x07`. The grammar match fails.
5. The RFC says these CTL characters are "also invalid." While the MAY-retain clause allows leniency in safe contexts, a generic header name like `X-Test` does not provide such a context.
6. The server SHOULD reject with 400 per RFC 9112 Section 2.2's general grammar-mismatch guidance.

### Security implications

- **Header injection**: Control characters can be used to manipulate how different parsers interpret header boundaries. For example, some parsers may treat VT (`0x0B`) as a line separator, effectively injecting a new header line within what appears to be a single header value.
- **Log poisoning**: BEL (`0x07`) causes terminal bells, BS (`0x08`) causes backspace overwriting in terminal displays. An attacker embedding these in headers can manipulate server log output, hiding malicious requests or injecting misleading log entries.
- **WAF bypass**: Web Application Firewalls that scan header values for malicious patterns may not account for interspersed control characters. An attacker could use `\x07` or `\x08` to break up signature patterns (e.g., `<scr\x07ipt>`) and evade detection.
- **Downstream parser confusion**: If a proxy retains the control characters (per the MAY-retain clause) and forwards them to a backend that interprets them differently, the two systems may disagree on header structure -- a potential header-smuggling vector.

## Sources

- [RFC 9110 Section 5.5](https://www.rfc-editor.org/rfc/rfc9110#section-5.5) — field values and control characters
