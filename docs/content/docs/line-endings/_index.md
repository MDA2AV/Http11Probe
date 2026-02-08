---
title: Line Endings
description: "Line Endings — Http11Probe documentation"
weight: 2
sidebar:
  open: false
---

RFC 9112 Section 2.2 defines that HTTP/1.1 messages use **CRLF** (`\r\n`) as the line terminator for the request-line and header fields. Deviations from this — bare LF, bare CR — are framing violations that can lead to request smuggling or parser confusion.

## The Rule

> "Although the line terminator for the start-line and fields is the sequence CRLF, a recipient **MAY** recognize a single LF as a line terminator and ignore any preceding CR." — RFC 9112 Section 2.2

> "A sender **MUST NOT** generate a bare CR (a CR character not immediately followed by LF) within any protocol elements other than the content. A recipient of such a bare CR **MUST** consider that element to be invalid or replace each bare CR with SP before processing the element or forwarding the message." — RFC 9112 Section 2.2

## Tests

{{< cards >}}
  {{< card link="bare-lf-request-line" title="BARE-LF-REQUEST-LINE" subtitle="Bare LF in the request-line. Recipients MAY accept." >}}
  {{< card link="bare-lf-header" title="BARE-LF-HEADER" subtitle="Bare LF in a header field. Recipients MAY accept." >}}
  {{< card link="cr-only-line-ending" title="CR-ONLY-LINE-ENDING" subtitle="CR without LF. MUST consider invalid or replace with SP." >}}
{{< /cards >}}

### Unscored

{{< cards >}}
  {{< card link="leading-crlf" title="LEADING-CRLF" subtitle="Leading CRLF before request-line. Server MAY ignore." >}}
{{< /cards >}}
