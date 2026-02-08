---
title: "CL-DOUBLE-ZERO"
description: "CL-DOUBLE-ZERO test documentation"
weight: 48
---

| | |
|---|---|
| **Test ID** | `SMUG-CL-DOUBLE-ZERO` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) |
| **Requirement** | SHOULD |
| **Expected** | `400` or `2xx` |

## What it sends

Content-Length with a double-zero value: `Content-Length: 00`.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 00\r\n
\r\n
```


## What the RFC says

RFC 9110 §8.6 defines the Content-Length grammar:

> "Content-Length = 1*DIGIT"

The value `00` matches the `1*DIGIT` grammar (two digits), so it is technically valid per the RFC. However, leading zeros create ambiguity when parsers interpret them differently — particularly when some treat leading-zero values as octal notation.

RFC 9112 §6.3 states that an invalid Content-Length is an unrecoverable error:

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error."

Since `00` is grammatically valid, the MUST-reject rule does not apply here. However, rejecting it is the safer behavior.

## Pass / Warn

The value `00` matches the `1*DIGIT` grammar, so it is technically valid. The RFC does not mandate rejection of grammatically valid Content-Length values. Both `400` (strict rejection of leading zeros) and `2xx` (accepting the valid grammar) are defensible, but rejection is preferred.

**Pass:** Server rejects with `400` (strict, safe).
**Warn:** Server accepts and responds `2xx` (RFC-valid but risky in proxy chains).

## Why it matters

While `00` happens to equal `0` in both decimal and octal, accepting leading zeros sets a precedent. If a server accepts `00`, it likely also accepts `010` (decimal 10 vs. octal 8) or `0200` (decimal 200 vs. octal 128). The safer behavior is to reject any Content-Length with leading zeros to eliminate the entire class of octal ambiguity attacks.

## Deep Analysis

### ABNF Analysis

RFC 9110 §8.6 defines the Content-Length grammar as:

> `Content-Length = 1*DIGIT`

The `1*DIGIT` production requires one or more ASCII digits (`0`-`9`). The value `00` consists of two ASCII digits, so it **does match** the grammar. It is syntactically valid per the ABNF.

### RFC Evidence Chain

**Step 1 -- The value is grammatically valid.**

`00` satisfies `1*DIGIT` (two DIGITs). There is no RFC rule that prohibits leading zeros in Content-Length. The ABNF does not distinguish between `0`, `00`, and `000` -- all are sequences of one or more digits.

**Step 2 -- No MUST-reject rule applies.**

RFC 9112 §6.3 mandates rejection only for *invalid* Content-Length:

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error."

Since `00` is valid per `1*DIGIT`, this MUST does not trigger.

**Step 3 -- The forwarding rule still applies to intermediaries.**

> "a sender MUST NOT forward a message with a Content-Length header field value that does not match the ABNF above" -- RFC 9110 §8.6

Since `00` does match the ABNF, this rule does not prohibit forwarding it either.

### Scored / SHOULD Justification

This test is scored as **SHOULD** reject. While `00` is grammatically valid, rejecting it is the safer behavior because accepting leading zeros opens the door to octal interpretation ambiguity. The test awards Pass for 400 (strict) and Warn for 2xx (technically valid but risky). It is not scored as MUST because the RFC grammar explicitly permits it.

### Real-World Smuggling Scenario

The value `00` is a degenerate case where decimal (0) and octal (0) agree. However, accepting `00` reveals that the server's parser tolerates leading zeros. An attacker can escalate: if `00` is accepted, the server likely accepts `010` (decimal 10 vs. octal 8) or `0200` (decimal 200 vs. octal 128). In a proxy chain where the front-end interprets leading zeros as decimal and the back-end as octal, the body length disagreement enables request smuggling. Rejecting `00` eliminates the entire class of octal ambiguity attacks at the root.

## Sources

- [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
- [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
