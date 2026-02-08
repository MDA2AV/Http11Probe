---
title: "EXPECT-UNKNOWN"
description: "EXPECT-UNKNOWN test documentation"
weight: 7
---

| | |
|---|---|
| **Test ID** | `COMP-EXPECT-UNKNOWN` |
| **Category** | Compliance |
| **RFC** | [RFC 9110 Section 10.1.1](https://www.rfc-editor.org/rfc/rfc9110#section-10.1.1) |
| **Requirement** | MAY respond with 417 |
| **Expected** | `417`; `2xx` is a warning |

## What it sends

`Expect: 200-ok` — an Expect header with a value the server cannot fulfill.

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
Expect: 200-ok\r\n
\r\n
```

The `Expect` header contains an unknown expectation value (not `100-continue`).


## What the RFC says

> "The only expectation defined by this specification is '100-continue' (with no defined parameters)." -- RFC 9110 Section 10.1.1

> "A server that receives an Expect field value containing a member other than 100-continue MAY respond with a 417 (Expectation Failed) status code to indicate that the unexpected expectation cannot be met." -- RFC 9110 Section 10.1.1

The RFC uses "MAY", not "MUST". A `417 Expectation Failed` is the semantically correct response for an unrecognized Expect value, but silently ignoring unknown expectations and processing the request normally is also permitted.

**Pass:** Server responds with `417 Expectation Failed`.
**Warn:** Server responds with `2xx` (valid per MAY, but less strict).

## Why it matters

The Expect mechanism is a contract between client and server. If a server ignores unknown Expect values, clients cannot rely on the mechanism for future extensions. Returning `417` signals clear rejection of unsupported expectations.

## Deep Analysis

### Relevant ABNF Grammar

```
Expect = "100-continue"
```

The ABNF for the Expect header field defines only a single valid expectation value: `100-continue`. There is no extensibility mechanism for additional expectation values in the current specification.

### RFC Evidence

**RFC 9110 Section 10.1.1** limits the defined expectations:

> "The only expectation defined by this specification is '100-continue' (with no defined parameters)." -- RFC 9110 Section 10.1.1

**RFC 9110 Section 10.1.1** provides the MAY-level guidance for unknown expectations:

> "A server that receives an Expect field value containing a member other than 100-continue MAY respond with a 417 (Expectation Failed) status code to indicate that the unexpected expectation cannot be met." -- RFC 9110 Section 10.1.1

**RFC 9110 Section 10.1.1** mandates server behavior for the known expectation:

> "A server that receives an Expect header field with a value of 100-continue MUST either respond with a 100 (Continue) status or respond with a final status code." -- RFC 9110 Section 10.1.1

### Chain of Reasoning

1. The test sends `Expect: 200-ok`, which is not `100-continue` and is therefore an unknown expectation.
2. The RFC uses MAY (not MUST) for the 417 response, meaning the server is permitted but not required to reject the request.
3. A server that ignores the unknown expectation and processes the request normally (returning 2xx) is technically compliant with the MAY.
4. However, 417 is the semantically precise response: it tells the client that the expectation cannot be met, which is the truth for any unrecognized value.
5. A server that silently ignores unknown expectations may cause problems for future protocol extensions that rely on the Expect mechanism.

### Scoring Justification

**Unscored (MAY).** Since the RFC uses MAY rather than MUST or SHOULD, there is no normative obligation to return 417. The test is therefore unscored: 417 is recorded as Pass (the server actively recognized and rejected the unknown expectation) and 2xx is recorded as Warn (the server ignored the expectation, which is permitted but less informative). No result is recorded as Fail because neither behavior violates the specification.

## Sources

- [RFC 9110 Section 10.1.1](https://www.rfc-editor.org/rfc/rfc9110#section-10.1.1)
