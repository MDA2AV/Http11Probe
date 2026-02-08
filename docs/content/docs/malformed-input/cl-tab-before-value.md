---
title: "CL-TAB-BEFORE-VALUE"
description: "CL-TAB-BEFORE-VALUE test documentation"
weight: 20
---

| | |
|---|---|
| **Test ID** | `MAL-CL-TAB-BEFORE-VALUE` |
| **Category** | Malformed Input |
| **RFC** | [RFC 9110 Section 5.5](https://www.rfc-editor.org/rfc/rfc9110#section-5.5) |
| **Requirement** | valid per RFC |
| **Expected** | `400` preferred; `2xx` is a warning |

## What it sends

`Content-Length:\t5` — a Content-Length header where a horizontal tab character separates the colon from the value, instead of a space.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length:\t5\r\n
\r\n
hello
```

A horizontal tab character (`\t` / `0x09`) separates the colon from the value instead of a space.


## What the RFC says

The field-line grammar explicitly includes optional whitespace between the colon and value:

> `field-line = field-name ":" OWS field-value OWS` — RFC 9112 Section 5

And OWS permits both spaces and horizontal tabs:

> `OWS = *( SP / HTAB )` — RFC 9110 Section 5.6.3

A tab character between the colon and value is technically valid per these grammars.

## Pass/Warn explanation

- **Pass (400):** The server rejects the request. While HTAB is valid OWS per the grammar, rejecting unusual whitespace is a defensively strict approach.
- **Warn (2xx):** The server accepted the tab as valid OWS and processed the request correctly. This is RFC-compliant behavior, but flagged as a warning because tab-separated Content-Length values are unusual in practice and may indicate parser inconsistencies.

## Why it matters

While tabs are valid OWS, they are rarely used in practice. Some parsers may not handle tab characters correctly -- for example, treating the tab as part of the value rather than whitespace, resulting in a failed integer parse or a different numeric interpretation. This edge case tests parser robustness.

## Deep Analysis

### ABNF context

The field-line grammar explicitly permits HTAB in OWS:

```
field-line = field-name ":" OWS field-value OWS
OWS        = *( SP / HTAB )
RWS        = 1*( SP / HTAB )
BWS        = OWS
```

After the colon, OWS consumes the tab character (`0x09`). The remaining field-value is `5`, which matches `Content-Length = 1*DIGIT`. The request `Content-Length:\t5` is **grammatically valid** per the combined ABNF of RFC 9112 Section 5 and RFC 9110 Section 5.6.3.

### RFC evidence

> "field-line = field-name ':' OWS field-value OWS" -- RFC 9112 Section 5

> "OWS = *( SP / HTAB )" -- RFC 9110 Section 5.6.3

> "Content-Length = 1*DIGIT" -- RFC 9110 Section 8.6

> "No whitespace is allowed between the field name and colon." -- RFC 9112 Section 5

The last quote is relevant context: the RFC is strict about whitespace *before* the colon but explicitly permissive *after* it via OWS. The tab character is part of OWS and therefore valid in this position.

### Chain of reasoning

1. The server receives `Content-Length:\t5\r\n`.
2. It parses the field-name (`Content-Length`), verifies no whitespace before the colon (correct), then reads OWS after the colon.
3. The OWS production matches the HTAB character (`0x09`).
4. The remaining field-value is `5`, which matches `1*DIGIT`.
5. The Content-Length value is correctly parsed as 5.
6. The server should read 5 bytes of body (`hello`) and respond with 2xx.
7. A server that rejects this request is being stricter than the RFC requires -- defensively conservative but not RFC-mandated. A server that accepts it is fully compliant.
8. The concern is not RFC compliance but parser robustness: does the server correctly treat HTAB as whitespace when extracting the numeric value?

### Security implications

- **Parser divergence on whitespace handling**: Some parsers may only strip SP (`0x20`) before the value, not HTAB (`0x09`). Such a parser would attempt to parse `\t5` as the Content-Length value, either failing (rejecting the request) or producing an unexpected numeric result.
- **Content-Length misinterpretation**: If one parser treats `\t5` as an invalid integer (rejecting or defaulting to 0) while another correctly strips the tab and reads `5`, the two systems disagree on body length. This disagreement between a proxy and a backend is a request smuggling primitive.
- **Character encoding confusion**: In some environments, HTAB may be converted to spaces or multiple spaces during processing. If the tab is converted to, say, 8 spaces before numeric parsing, the value becomes `        5`, which is still `5` after stripping -- but the intermediate representation may cause issues in parsers that do not expect leading whitespace in numeric fields.
- **Evasion of WAF rules**: Web Application Firewalls that inspect Content-Length values may not account for HTAB as a separator, potentially allowing an attacker to bypass Content-Length-based filtering or validation rules.

## Sources

- [RFC 9112 Section 5](https://www.rfc-editor.org/rfc/rfc9112#section-5) — field-line grammar with OWS
- [RFC 9110 Section 5.6.3](https://www.rfc-editor.org/rfc/rfc9110#section-5.6.3) — OWS = *( SP / HTAB )
