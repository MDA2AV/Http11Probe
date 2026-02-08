---
title: "TE-OBS-FOLD"
description: "TE-OBS-FOLD test documentation"
weight: 50
---

| | |
|---|---|
| **Test ID** | `SMUG-TE-OBS-FOLD` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §5.2](https://www.rfc-editor.org/rfc/rfc9112#section-5.2) |
| **Requirement** | MUST |
| **Expected** | `400` |

## What it sends

Transfer-Encoding header value wrapped using obs-fold (obsolete line folding), with Content-Length also present.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding:\r\n
 chunked\r\n
Content-Length: 5\r\n
\r\n
hello
```

The Transfer-Encoding value `chunked` is placed on a continuation line (preceded by CRLF and a space), using the obsolete line folding syntax.


## What the RFC says

> "A server that receives an obs-fold in a request message that is not within a 'message/http' container MUST either reject the message by sending a 400 (Bad Request), preferably with a representation explaining that obsolete line folding is unacceptable, or replace each received obs-fold with one or more SP octets prior to interpreting the field value or forwarding the message downstream." -- RFC 9112 Section 5.2

When obs-fold is used on the Transfer-Encoding header with Content-Length also present, the risk is acute: a folding-aware parser unfolds the value and sees `Transfer-Encoding: chunked`, while a strict parser that does not recognize the fold sees an empty Transfer-Encoding value and falls back to Content-Length. This creates a direct CL/TE desync.

## Why it matters

This is a high-confidence smuggling vector. The obs-fold mechanism was deprecated precisely because of parser disagreements. When applied to Transfer-Encoding — the header that determines message framing — it creates a situation where one parser uses chunked encoding and another uses Content-Length, enabling request smuggling. The RFC requires rejection (MUST), and no `AllowConnectionClose` alternative is acceptable because the server must actively reject the malformed header rather than simply closing the connection.

## Deep Analysis

### ABNF

```
field-line   = field-name ":" OWS field-value OWS  ; RFC 9112 §5
obs-fold     = OWS CRLF RWS                        ; RFC 9112 §5.2
OWS          = *( SP / HTAB )                       ; RFC 9110 §5.6.3
RWS          = 1*( SP / HTAB )                      ; RFC 9110 §5.6.3
```

The `obs-fold` rule (obsolete line folding) allows a field value to be continued on the next line if that line begins with at least one space or tab (`RWS`). In this test, the Transfer-Encoding value is split: the colon is followed by `\r\n` (CRLF) and then ` chunked` (space + value), matching the `obs-fold` production.

### RFC Evidence

> "A server that receives an obs-fold in a request message that is not within a message/http container MUST either reject the message by sending a 400 (Bad Request), preferably with a representation explaining that obsolete line folding is unacceptable, or replace each received obs-fold with one or more SP octets prior to interpreting the field value or forwarding the message downstream." -- RFC 9112 §5.2

> "A sender MUST NOT generate a message that includes line folding (i.e., that has any field line value that contains a match to the obs-fold rule) unless the message is intended for packaging within the message/http media type." -- RFC 9112 §5.2

> "If a message is received with both a Transfer-Encoding and a Content-Length header field, the Transfer-Encoding overrides the Content-Length. Such a message might indicate an attempt to perform request smuggling or response splitting and ought to be handled as an error." -- RFC 9112 §6.3

### Chain of Reasoning

1. The test sends a Transfer-Encoding header where the value `chunked` is placed on a continuation line using obs-fold syntax: `Transfer-Encoding:\r\n chunked`. A `Content-Length: 5` header is also present.
2. RFC 9112 section 5.2 explicitly states that a sender MUST NOT generate messages with obs-fold. The sender is in violation.
3. For the server, RFC 9112 section 5.2 provides a MUST-level requirement with two options: either reject with `400` **or** replace the obs-fold with spaces and interpret the field value normally.
4. If the server chooses to unfold, it replaces the `\r\n ` with a space and sees `Transfer-Encoding: chunked`. Combined with `Content-Length: 5`, this triggers the dual-header rules of RFC 9112 section 6.3.
5. The critical danger is that a parser **unaware** of obs-fold sees the `Transfer-Encoding:` header as having an empty value (since the value after the colon on that line is empty), followed by what looks like a new header line starting with ` chunked`. The space-prefixed line may be discarded as malformed or interpreted as a separate entity. This parser sees no valid Transfer-Encoding and falls back to Content-Length.
6. Meanwhile, a folding-aware parser unfolds the value and uses chunked framing. This disagreement between folding-aware and folding-unaware parsers is the core of the smuggling vector.

### Scored / Unscored Justification

This test is **scored** (MUST reject with `400`). RFC 9112 section 5.2 provides a MUST-level requirement for servers receiving obs-fold. While the RFC allows two options (reject or unfold), this test expects strict `400` rejection because the obs-fold is applied to the Transfer-Encoding header -- the header that determines message framing. Allowing an unfolded interpretation when Content-Length is also present would require the server to then handle the CL/TE dual-header scenario, adding further complexity and risk. No `AllowConnectionClose` alternative is acceptable because the server must actively reject the malformed header.

- **Pass (400):** The server correctly rejects the obs-fold per the MUST requirement.
- **Fail (2xx or close):** The server either silently accepted the folded header or merely closed the connection without the required `400` response.

### Smuggling Attack Scenarios

- **Fold-Aware vs. Fold-Unaware Desync:** A front-end proxy that does not implement obs-fold parsing sees `Transfer-Encoding:` with an empty value and ignores it, using Content-Length for framing. A back-end that implements obs-fold unfolding sees `Transfer-Encoding: chunked` and uses chunked framing. The attacker embeds a second request inside the chunked body that the front-end never sees.
- **Header Injection via Fold Confusion:** A parser that does not recognize obs-fold may interpret the continuation line ` chunked` as a malformed header line. Some parsers silently discard lines starting with whitespace, while others attempt to parse them as headers. This inconsistency can cause different hops in a proxy chain to see different sets of headers.
- **Selective Obs-Fold Processing:** Even among folding-aware parsers, some may only unfold certain headers. A proxy that unfolds general headers but not Transfer-Encoding specifically would see an empty TE value, while the back-end unfolds all headers and processes chunked encoding. The selective unfolding creates the exact framing disagreement attackers need.

## Sources

- [RFC 9112 §5.2](https://www.rfc-editor.org/rfc/rfc9112#section-5.2)
- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
