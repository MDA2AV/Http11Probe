---
title: "LEADING-CRLF"
description: "LEADING-CRLF test documentation"
weight: 4
---

| | |
|---|---|
| **Test ID** | `COMP-LEADING-CRLF` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) |
| **Requirement** | SHOULD ignore |
| **Expected** | `400` or `2xx` |

## What it sends

Two leading CRLF sequences before the request-line.

```http
\r\n
\r\n
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

Two leading CRLF pairs precede the actual request-line.


## What the RFC says

> "In the interest of robustness, a server that is expecting to receive and parse a request-line SHOULD ignore at least one empty line (CRLF) received prior to the request-line." — RFC 9112 Section 2.2

This is a SHOULD, not a MUST. The RFC recommends tolerance for robustness, but strict rejection is also acceptable.

## Pass / Warn explanation

| Response | Verdict | Reasoning |
|---|---|---|
| `400` or close | Pass | Strict rejection — valid because SHOULD is not MUST |
| `2xx` | Warn | Tolerant behavior — matches the RFC recommendation but flagged for awareness |

## Why it matters

Leading CRLFs can appear on persistent connections due to extra bytes after a previous response. The RFC encourages tolerance as a robustness measure. Both strict and tolerant behaviors are acceptable, which is why both produce non-failing verdicts.

## Deep Analysis

### ABNF grammar for line endings

The HTTP message grammar from RFC 9112 Section 2.1:

```
HTTP-message = start-line CRLF
               *( field-line CRLF )
               CRLF
               [ message-body ]
```

From RFC 5234 Appendix B.1:

```
CRLF = CR LF        ; Internet standard newline
CR   = %x0D          ; carriage return
LF   = %x0A          ; linefeed
```

The `HTTP-message` grammar starts with `start-line` --- there is no provision for leading whitespace or empty lines before the start-line. Any CRLF sequences before the request-line are extra-grammatical; they do not appear in the ABNF.

### RFC evidence

**Quote 1 --- The robustness recommendation:**

> "In the interest of robustness, a server that is expecting to receive and parse a request-line SHOULD ignore at least one empty line (CRLF) received prior to the request-line." --- RFC 9112 Section 2.2

The keyword is SHOULD (RFC 2119): the server is recommended to ignore leading CRLFs but is not required to. The phrase "at least one" indicates that ignoring multiple leading CRLFs is also within scope of the recommendation.

**Quote 2 --- The whitespace-before-header prohibition (by contrast):**

> "A sender MUST NOT send whitespace between the start-line and the first header field." --- RFC 9112 Section 2.2

> "A recipient that receives whitespace between the start-line and the first header field MUST either reject the message as invalid or consume each whitespace-preceded line without further processing." --- RFC 9112 Section 2.2

These two sentences are relevant by contrast. The RFC is strict about whitespace **after** the start-line (MUST reject or consume) but lenient about empty lines **before** the start-line (SHOULD ignore). This asymmetry is deliberate: leading CRLFs on persistent connections are a known artifact, while whitespace after the start-line is an attack vector for response splitting.

**Quote 3 --- The smuggling context:**

> "Lenient parsing can result in request smuggling security vulnerabilities if there are multiple recipients of the message and each has its own unique interpretation of robustness." --- RFC 9112 Section 3

Even though the RFC recommends tolerance for leading CRLFs, the broader security context warns that any lenient parsing can create inconsistencies. If one parser in a chain ignores leading CRLFs and another treats them as the start of an empty request, the resulting disagreement could be exploitable --- though this is a lower-risk scenario compared to bare LF or bare CR.

### Chain of reasoning

1. **The payload:** The test sends `\r\n\r\nGET / HTTP/1.1\r\nHost: localhost:8080\r\n\r\n`. Two CRLF pairs precede the actual request-line.
2. **The ABNF mismatch:** The `HTTP-message` grammar starts with `start-line`. Leading CRLFs are not part of the grammar, so a strict parser would not expect them.
3. **The SHOULD recommendation:** RFC 9112 Section 2.2 says the server SHOULD ignore "at least one empty line (CRLF) received prior to the request-line." This payload has two leading CRLFs, which falls within "at least one."
4. **Why tolerance exists:** On persistent (keep-alive) connections, extra bytes after a previous response can manifest as leading CRLFs before the next request. The RFC accounts for this real-world artifact with the SHOULD recommendation.
5. **Why rejection is also valid:** SHOULD is not MUST. A server that rejects with `400` is non-conforming to the recommendation but not violating a requirement. Some security-focused servers may reject to avoid any ambiguity about message boundaries.
6. **Conclusion:** Both behaviors --- ignoring the leading CRLFs and processing the request normally (`2xx`), or rejecting with `400` --- are acceptable. Neither is a failure.

### Scored / Unscored justification

This test is **unscored (Pass/Warn, no Fail)**. The requirement level is SHOULD, and both acceptance and rejection are valid:

- **Pass** for `400` or connection close --- strict rejection is valid because SHOULD is not MUST. The server is declining the robustness recommendation, which is its prerogative.
- **Warn** for `2xx` --- the server is following the RFC's SHOULD recommendation, which is the intended behavior. It receives a Warn (not a Pass) because Http11Probe flags tolerant behaviors for awareness, even when they match the RFC recommendation.
- The test **cannot produce a Fail** because no MUST requirement is at stake.

The Pass/Warn asymmetry (strict = Pass, tolerant = Warn) may seem counterintuitive since the RFC recommends tolerance. The rationale is that Http11Probe prioritizes security awareness: any leniency in parsing, even RFC-recommended leniency, is worth flagging so operators can make informed decisions about their server's posture.

## Sources

- [RFC 9112 Section 2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2)
