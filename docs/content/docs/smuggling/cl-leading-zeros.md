---
title: "CL-LEADING-ZEROS"
description: "CL-LEADING-ZEROS test documentation"
weight: 3
---

| | |
|---|---|
| **Test ID** | `SMUG-CL-LEADING-ZEROS` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) |
| **Requirement** | SHOULD |
| **Expected** | `400` or `2xx` |

## What it sends

Content-Length with leading zeros: `Content-Length: 005`.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 005\r\n
\r\n
hello
```


## What the RFC says

RFC 9110 §8.6 defines the Content-Length grammar:

> "Content-Length = 1*DIGIT"

Since `005` matches the `1*DIGIT` grammar (three ASCII digits), it is technically valid per the RFC. However, leading zeros create ambiguity — some parsers may interpret the value as octal (base-8), while others treat it as decimal.

RFC 9112 §6.3 only mandates rejection for *invalid* Content-Length:

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error."

Since `005` is grammatically valid, the MUST-reject rule does not strictly apply. However, rejecting it is the safer behavior.

## Pass / Warn

The value `005` matches the `1*DIGIT` grammar, so it is technically valid. The RFC does not mandate rejection of grammatically valid Content-Length values. Both `400` (strict rejection of leading zeros) and `2xx` (accepting the valid grammar and parsing as decimal 5) are defensible.

## Why it matters

This is a **security vs. strict RFC compliance** tension. The value `005` is grammatically valid, so a server that accepts it and parses it as decimal 5 is not violating the RFC. However, if a front-end and back-end disagree on whether `005` means 5 (decimal) or 5 (octal), they agree by coincidence. For values like `010` (decimal 10 vs. octal 8), disagreement causes body boundary misalignment — a smuggling vector.

**Pass:** Server rejects with `400` (strict, safe).
**Warn:** Server accepts and responds `2xx` (RFC-valid but potentially risky in proxy chains).

## Deep Analysis

### ABNF Analysis

RFC 9110 §8.6 defines the Content-Length grammar as:

> `Content-Length = 1*DIGIT`

The `1*DIGIT` production requires one or more ASCII digits (`0`-`9`). The value `005` consists of three ASCII digits: `0`, `0`, `5`. All three are valid DIGITs, so `005` **does match** the grammar. It is syntactically valid per the ABNF.

### RFC Evidence Chain

**Step 1 -- The value is grammatically valid.**

`005` satisfies `1*DIGIT` (three DIGITs). The ABNF makes no distinction between `5`, `05`, and `005`. There is no rule in RFC 9110 or RFC 9112 that prohibits leading zeros in Content-Length values.

**Step 2 -- No MUST-reject rule applies.**

RFC 9112 §6.3 mandates rejection only for *invalid* Content-Length:

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error."

Since `005` is valid per `1*DIGIT`, the MUST-reject rule does not trigger.

**Step 3 -- The forwarding rule does not prohibit it.**

> "a sender MUST NOT forward a message with a Content-Length header field value that does not match the ABNF above" -- RFC 9110 §8.6

Since `005` matches the ABNF, intermediaries are not prohibited from forwarding it.

### Scored / SHOULD Justification

This test is scored as **SHOULD** reject (Pass for 400, Warn for 2xx). While `005` is grammatically valid and a server accepting it does not violate any MUST-level requirement, rejecting leading zeros is the safer behavior. The test cannot be scored as MUST because the ABNF explicitly permits it, but the security implications justify a SHOULD-level expectation.

### Real-World Smuggling Scenario

The value `005` happens to be unambiguous (decimal 5, octal 5) because all digits are below 8. However, accepting `005` reveals that the server's parser tolerates leading zeros, which means it likely also accepts `010`. In a proxy chain, if the front-end interprets `010` as decimal 10 and the back-end interprets it as octal 8, they disagree on the body length by 2 bytes. The front-end forwards 10 bytes, but the back-end only consumes 8 -- the remaining 2 bytes are treated as the start of the next request. An attacker uses `005` as a harmless probe to confirm leading-zero tolerance before escalating to `010` or `0200` for the actual attack.

## Sources

- [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
- [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
