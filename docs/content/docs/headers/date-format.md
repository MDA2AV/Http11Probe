---
title: "DATE-FORMAT"
description: "DATE-FORMAT test documentation"
weight: 20
---

| | |
|---|---|
| **Test ID** | `COMP-DATE-FORMAT` |
| **Category** | Compliance |
| **Scored** | No |
| **RFC** | [RFC 9110 §5.6.7](https://www.rfc-editor.org/rfc/rfc9110#section-5.6.7) |
| **RFC Level** | SHOULD |
| **Expected** | IMF-fixdate format |

## What it does

Sends a standard GET request and checks whether the `Date` response header uses the preferred IMF-fixdate format.

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

The test inspects the `Date` header value in the response.

## What the RFC says

> "An HTTP-date value represents time as an instance of Coordinated Universal Time (UTC). The first two formats [IMF-fixdate and rfc850-date] indicate UTC by the three-letter abbreviation for Greenwich Mean Time, 'GMT'... **A recipient that parses a timestamp value in an HTTP field MUST accept all three HTTP-date formats.**" -- RFC 9110 §5.6.7

> "HTTP-date = IMF-fixdate / obs-date" -- RFC 9110 §5.6.7

> "A sender MUST generate timestamps in the IMF-fixdate format." -- RFC 9110 §5.6.7 (quoted from RFC 7231 §7.1.1.1, carried forward)

The preferred format is **IMF-fixdate**:

```
Sun, 06 Nov 1994 08:49:37 GMT
```

## Why it matters

While all three date formats are valid for *recipients* to accept, **senders** (including origin servers) should generate the IMF-fixdate format. Servers using obsolete formats (RFC 850 or asctime) are technically non-conforming senders, though recipients must still parse them.

## Verdicts

- **Pass** -- Date header present and uses IMF-fixdate format
- **Warn** -- Date header missing or uses a non-standard format

## Sources

- [RFC 9110 §5.6.7](https://www.rfc-editor.org/rfc/rfc9110#section-5.6.7)
- [RFC 9110 §6.6.1](https://www.rfc-editor.org/rfc/rfc9110#section-6.6.1)
