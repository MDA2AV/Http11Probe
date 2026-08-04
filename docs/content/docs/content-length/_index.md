---
title: Content-Length
description: "Content-Length header validation tests covering non-numeric values, plus signs, integer overflow, and other malformed framing per RFC 9112."
weight: 8
sidebar:
  open: false
---

The `Content-Length` header indicates the size of the message body in bytes. Its grammar is strict: `Content-Length = 1*DIGIT`. Any deviation — non-numeric characters, plus signs, leading zeros, negative values, overflow — can cause parsers to disagree on body boundaries.

## Key Rules

**Grammar**: `1*DIGIT` means one or more ASCII digits (`0-9`). No signs, no spaces, no hex.

> “If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient **MUST** treat it as an unrecoverable error...” — RFC 9112 Section 6.3

## Tests

{{< cards >}}
  {{< card link="cl-non-numeric" title="CL-NON-NUMERIC" subtitle="Non-numeric Content-Length value." >}}
  {{< card link="cl-plus-sign" title="CL-PLUS-SIGN" subtitle="Content-Length with a + prefix." >}}
  {{< card link="no-cl-in-204" title="NO-CL-IN-204" subtitle="Content-Length forbidden in 204 responses." >}}
{{< /cards >}}
