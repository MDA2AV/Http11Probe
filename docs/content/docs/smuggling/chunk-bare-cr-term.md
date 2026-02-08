---
title: "CHUNK-BARE-CR-TERM"
description: "CHUNK-BARE-CR-TERM test documentation"
weight: 53
---

| | |
|---|---|
| **Test ID** | `SMUG-CHUNK-BARE-CR-TERM` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) |
| **Requirement** | MUST |
| **Expected** | `400` or close |

## What it sends

A chunked request where the chunk size line is terminated by bare CR (`\r`) without LF.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
\r\n
5\r
hello\r\n
0\r\n
\r\n
```

The chunk size `5` is followed by a bare CR (`\r`) instead of the required CRLF (`\r\n`). The bytes `hello` immediately follow the bare CR.


## What the RFC says

The chunk line grammar requires CRLF terminators:

> chunk = chunk-size [ chunk-ext ] CRLF chunk-data CRLF
>
> — RFC 9112 §7.1

And RFC 9112 §2.2 explicitly forbids bare CR:

> "A sender MUST NOT generate a bare CR (a CR character not immediately followed by LF) within any protocol elements other than the content. A recipient of such a bare CR MUST consider that element to be invalid or replace each bare CR with SP before processing the element or forwarding the message."
>
> — RFC 9112 §2.2

Note: the RFC permits MAY-accept for bare LF (§2.2), but makes no such allowance for bare CR. A bare CR in a chunk-size line is explicitly invalid.

## Why it matters

Some parsers treat CR alone as a line ending (a behavior inherited from old Mac-style line endings). If one parser accepts bare CR as a chunk-size terminator and another requires CRLF, they disagree on where the chunk data begins. The strict parser sees `5\rhello` as a malformed chunk size (containing `\r`, `h`, `e`, `l`, `l`, `o`), while the lenient parser sees chunk size 5 followed by `hello` as chunk data. This boundary disagreement enables chunk-level desynchronization.

## Deep Analysis

### Relevant ABNF (RFC 9112 §7.1)

```
chunked-body = *chunk last-chunk trailer-section CRLF

chunk        = chunk-size [ chunk-ext ] CRLF
               chunk-data CRLF
chunk-size   = 1*HEXDIG
chunk-data   = 1*OCTET ; a sequence of chunk-size octets
last-chunk   = 1*("0") [ chunk-ext ] CRLF
```

### RFC Evidence

**RFC 9112 §7.1** defines the `chunk` production as requiring CRLF in two positions -- after the chunk-size line and after the chunk-data:

> "chunk = chunk-size [ chunk-ext ] CRLF chunk-data CRLF"

**RFC 9112 §2.2** explicitly addresses bare CR:

> "A sender MUST NOT generate a bare CR (a CR character not immediately followed by LF) within any protocol elements other than the content. A recipient of such a bare CR MUST consider that element to be invalid or replace each bare CR with SP before processing the element or forwarding the message."

**RFC 9112 §2.2** also includes a MAY-level allowance for bare LF, but notably provides **no such allowance for bare CR**:

> "Although the line terminator for the start-line and fields is the sequence CRLF, a recipient MAY recognize a single LF as a line terminator and ignore any preceding CR."

### Step-by-Step ABNF Violation

1. The parser begins reading a `chunk` and expects `chunk-size`, which is `1*HEXDIG`. It reads `5` -- valid so far.
2. Next the parser expects either `chunk-ext` (starting with `;`) or `CRLF` (the sequence `\r\n`).
3. The parser encounters `\r` (0x0D). This could be the start of CRLF, so it looks for `\n` (0x0A) next.
4. Instead of `\n`, the next byte is `h` (0x68, the start of `hello`). The `\r` is therefore a **bare CR** -- it is not followed by LF.
5. The ABNF requires `CRLF` at this position. A bare CR does not satisfy the `CRLF` production. The parse fails.
6. Per §2.2, the recipient MUST either consider the element invalid (reject with 400) or replace the bare CR with SP. Replacing with SP yields ` 5 hello`, which is not a valid chunk-size line either.

### Real-World Smuggling Scenario

Bare CR as a line terminator is a legacy behavior from classic Mac OS (pre-OS X). Some parsers inherited from that era treat `\r` alone as a line ending.

**Attack vector:** An attacker sends `5\rhello\r\n0\r\n\r\n`. A lenient parser that treats bare CR as a line terminator reads chunk-size `5`, then reads 5 bytes of chunk data (`hello`), and processes the message normally. A strict parser rejects the message or interprets it differently -- for example, it may try to parse `5\rhello` as a single chunk-size token, fail, and close the connection. If the lenient parser is a front-end proxy and the strict parser is the back-end, the front-end forwards a request the back-end cannot parse, potentially leaving leftover bytes in the connection that get prepended to the next request.

This class of bare-CR chunk terminator confusion is closely related to the techniques described in research on HTTP request smuggling via chunked encoding ambiguities (e.g., the "TERM" vector class identified by James Kettle's HTTP/2 downgrade smuggling research).

## Sources

- [RFC 9112 §2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2)
- [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1)
