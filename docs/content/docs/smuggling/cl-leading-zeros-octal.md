---
title: "CL-LEADING-ZEROS-OCTAL"
description: "CL-LEADING-ZEROS-OCTAL test documentation"
weight: 49
---

| | |
|---|---|
| **Test ID** | `SMUG-CL-LEADING-ZEROS-OCTAL` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) |
| **Requirement** | SHOULD |
| **Expected** | `400` or `2xx` |

## What it sends

Content-Length with a leading-zero value that differs between decimal and octal interpretation: `Content-Length: 0200` with 200 bytes of body (`A` repeated).

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 0200\r\n
\r\n
AAAAAAAAAA... (200 bytes)
```


## What the RFC says

RFC 9110 §8.6 defines the Content-Length grammar:

> "Content-Length = 1*DIGIT"

The value `0200` matches the `1*DIGIT` grammar (four ASCII digits), so it is technically valid. However, `0200` can be parsed as decimal 200 or octal 128 depending on the parser implementation. This is the critical ambiguity that leading zeros create.

RFC 9112 §6.3 only mandates rejection for *invalid* Content-Length:

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error."

Since `0200` is grammatically valid, the MUST-reject rule does not strictly apply. However, rejecting it is the safer behavior.

## Pass / Warn

The value `0200` matches the `1*DIGIT` grammar, so it is technically valid per the RFC. The RFC does not mandate rejection of grammatically valid Content-Length values with leading zeros. Both `400` (strict rejection) and `2xx` (accepting the valid grammar and parsing as decimal 200) are defensible. However, rejecting leading zeros is strongly recommended because of the octal ambiguity risk.

**Pass:** Server rejects with `400` (strict, safe).
**Warn:** Server accepts and responds `2xx` (RFC-valid but dangerous in proxy chains).

## Why it matters

This is a classic smuggling vector. If a front-end proxy reads `0200` as decimal 200, it forwards all 200 bytes as the body. If the back-end reads `0200` as octal 128, it only consumes 128 bytes — the remaining 72 bytes "spill" into the connection and are interpreted as the start of the next request. An attacker can craft those 72 bytes to be a complete malicious request, achieving request smuggling through parser disagreement on a single Content-Length value.

## Deep Analysis

### ABNF Analysis

RFC 9110 §8.6 defines the Content-Length grammar as:

> `Content-Length = 1*DIGIT`

The `1*DIGIT` production requires one or more ASCII digits (`0`-`9`). The value `0200` consists of four ASCII digits: `0`, `2`, `0`, `0`. All four are valid DIGITs, so `0200` **does match** the grammar. It is syntactically valid per the ABNF.

### RFC Evidence Chain

**Step 1 -- The value is grammatically valid.**

`0200` satisfies `1*DIGIT` (four DIGITs). The ABNF does not distinguish between `200` and `0200`. There is no explicit RFC rule prohibiting leading zeros in Content-Length.

**Step 2 -- No MUST-reject rule applies.**

RFC 9112 §6.3 mandates rejection only for *invalid* Content-Length:

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error."

Since `0200` is valid per `1*DIGIT`, the MUST-reject rule does not trigger.

**Step 3 -- The forwarding rule does not prohibit it.**

> "a sender MUST NOT forward a message with a Content-Length header field value that does not match the ABNF above" -- RFC 9110 §8.6

Since `0200` matches the ABNF, intermediaries are not prohibited from forwarding it.

### The Critical Ambiguity

Unlike `005` (where decimal and octal agree), `0200` produces **different values** depending on interpretation:

- **Decimal:** `0200` = 200
- **Octal:** `0200` = 2 x 64 + 0 x 8 + 0 = 128

This 72-byte difference (200 - 128) is the smuggling payload window.

### Scored / SHOULD Justification

This test is scored as **SHOULD** reject (Pass for 400, Warn for 2xx). The value is grammatically valid, so no MUST-level obligation to reject exists. However, `0200` is the most dangerous leading-zero case because the decimal and octal interpretations diverge by 72 bytes -- enough to embed a complete smuggled HTTP request. The SHOULD scoring reflects the severe security risk despite the grammar being technically satisfied.

### Real-World Smuggling Scenario

This is a textbook CL-based smuggling attack. The request carries 200 bytes of body. A front-end proxy that interprets `0200` as decimal 200 forwards all 200 bytes. A back-end that interprets `0200` as octal 128 consumes only 128 bytes, leaving 72 bytes unconsumed on the connection. Those 72 bytes spill into the TCP stream and are parsed as the beginning of the next HTTP request. An attacker crafts those 72 bytes as:

```
GET /admin HTTP/1.1\r\nHost: internal\r\n\r\n
```

The back-end processes this as a legitimate request from the front-end's trusted connection, bypassing authentication and access controls. This is why leading zeros in Content-Length are dangerous even though the ABNF permits them.

## Sources

- [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
- [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
