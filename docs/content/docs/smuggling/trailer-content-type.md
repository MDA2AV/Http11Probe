---
title: "TRAILER-CONTENT-TYPE"
description: "TRAILER-CONTENT-TYPE test documentation"
weight: 58
---

| | |
|---|---|
| **Test ID** | `SMUG-TRAILER-CONTENT-TYPE` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 §6.5.1](https://www.rfc-editor.org/rfc/rfc9110#section-6.5.1) |
| **Requirement** | Unscored |
| **Expected** | `400` or `2xx` |

## What it sends

A valid chunked request with a `Content-Type: text/evil` header in the trailer section (after the last chunk).

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
\r\n
5\r\n
hello\r\n
0\r\n
Content-Type: text/evil\r\n
\r\n
```

A `Content-Type: text/evil` header appears in the chunked trailers section.


## What the RFC says

> "Many fields cannot be processed outside the header section because their evaluation is necessary prior to receiving the content, such as those that describe message framing, routing, authentication, request modifiers, response controls, or content format. A sender MUST NOT generate a trailer field unless the sender knows the corresponding header field name's definition permits the field to be sent in trailers." — RFC 9110 §6.5.1

> "A recipient MUST NOT merge a received trailer field into the header section unless its corresponding header field definition explicitly permits and instructs how the trailer field value can be safely merged." — RFC 9112 §7.1.2

Content-Type describes content format and is therefore prohibited in trailers per the above rule. A compliant server should either reject the request or silently discard the prohibited trailer field.

## Why this test is unscored

The sender violates the RFC by placing Content-Type in a trailer. The server must either reject or ignore the prohibited trailer. Both `400` (reject) and `2xx` (process body, discard trailer) are defensible since the chunked body itself is valid.

**Pass:** Server rejects with `400` (strict, safe).
**Warn:** Server accepts and responds `2xx` (processes body and discards prohibited trailer).

## Why it matters

If a server or middleware processes the `Content-Type` trailer, it could retroactively change how the already-received body is interpreted. An attacker could send a benign `Content-Type` in the headers to pass WAF inspection, then inject a different `Content-Type` in the trailer to trick downstream processors into interpreting the body differently — for example, changing `application/json` to `text/xml` to trigger different parsing paths or bypass content-type-based security filters.

## Deep Analysis

### Relevant ABNF

From RFC 9112 Section 7.1:

```
chunked-body    = *chunk
                  last-chunk
                  trailer-section
                  CRLF

trailer-section = *( field-line CRLF )
```

The trailer section can contain any syntactic field-line, but RFC 9110 Section 6.5.1 semantically restricts which fields are permitted based on their function.

### RFC Evidence

> "Many fields cannot be processed outside the header section because their evaluation is necessary prior to receiving the content, such as those that describe message framing, routing, authentication, request modifiers, response controls, or content format. A sender MUST NOT generate a trailer field unless the sender knows the corresponding header field name's definition permits the field to be sent in trailers." -- RFC 9110 Section 6.5.1

> "A recipient MUST NOT merge a received trailer field into the header section unless its corresponding header field definition explicitly permits and instructs how the trailer field value can be safely merged." -- RFC 9112 Section 7.1.2

> "A sender MUST NOT generate a trailer that contains a field necessary for message framing, routing, request modifiers, authentication, response control, or determining how to process the payload." -- RFC 9110 Section 6.5.1

### Chain of Reasoning

1. **Content-Type describes "content format" -- an explicitly prohibited trailer category.** RFC 9110 Section 6.5.1 lists "content format" among the field categories that cannot be processed outside the header section. Content-Type is the canonical content format field. Its value must be known before the body is parsed so that the recipient can select the correct decoder (JSON parser, XML parser, multipart boundary scanner, etc.). Receiving it after the body has already been transmitted and parsed defeats its purpose.

2. **The MUST NOT merge rule prevents retroactive reinterpretation.** Even if a parser encounters `Content-Type: text/evil` in the trailer section, RFC 9112 Section 7.1.2 prohibits merging it into the header section. The Content-Type field definition does not permit trailer usage, so the merge condition is never satisfied. A compliant recipient must discard it.

3. **The attack exploits the gap between header-time and trailer-time processing.** Security filters (WAFs, input validators, content scanners) inspect headers before the body arrives. If the Content-Type header says `application/json`, the WAF applies JSON validation rules. But if a different Content-Type arrives in the trailer after the body has already passed through, and the back-end uses the trailer value to select its parser, the body may be reinterpreted under a completely different content type that the WAF never validated.

4. **Attack scenario.** An attacker sends a chunked POST with `Content-Type: application/json` in the headers and a body containing XML with embedded XXE payloads. The WAF sees JSON content-type and applies JSON rules (the XML passes because the WAF is not running XML checks). In the trailer, the attacker sends `Content-Type: application/xml`. A vulnerable back-end processes the trailer, switches to the XML parser, and the XXE payload executes -- all because the WAF and back-end disagreed on content type due to the trailer injection.

### Scored / Unscored Justification

This test is **unscored** (`Scored = false`). The sender is in clear violation of the RFC (MUST NOT generate Content-Type as a trailer), but the server has two compliant responses: reject the message (`400`) or accept the valid chunked body while discarding the prohibited trailer (`2xx`). A `2xx` response is ambiguous -- it could mean the server correctly discarded the trailer (safe) or that it processed it (vulnerable). Since the status code alone cannot distinguish these cases, the test flags `2xx` as a warning. The risk depends entirely on whether the back-end or any intermediary actually applies the trailer Content-Type value, which requires manual investigation.

## Sources

- [RFC 9110 §6.5.1](https://www.rfc-editor.org/rfc/rfc9110#section-6.5.1)
- [RFC 9110 §6.5.2](https://www.rfc-editor.org/rfc/rfc9110#section-6.5.2)
- [RFC 9112 §7.1.2](https://www.rfc-editor.org/rfc/rfc9112#section-7.1.2)
