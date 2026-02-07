---
title: Content-Length
description: "Content-Length — Http11Probe documentation"
weight: 6
sidebar:
  open: true
---

The `Content-Length` header indicates the size of the message body in bytes. Its grammar is strict: `Content-Length = 1*DIGIT`. Any deviation — non-numeric characters, plus signs, leading zeros, negative values, overflow — can cause parsers to disagree on body boundaries.

## Key Rules

**Grammar**: `1*DIGIT` means one or more ASCII digits (`0-9`). No signs, no spaces, no hex.

> “If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient **MUST** treat it as an unrecoverable error...” — RFC 9112 Section 6.3

## Tests

{{< cards >}}
  {{< card link="cl-non-numeric" title="CL-NON-NUMERIC" subtitle="Non-numeric Content-Length value." >}}
  {{< card link="cl-plus-sign" title="CL-PLUS-SIGN" subtitle="Content-Length with a + prefix." >}}
{{< /cards >}}
