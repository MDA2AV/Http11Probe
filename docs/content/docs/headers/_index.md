---
title: Header Syntax
description: "Header Syntax — Http11Probe documentation"
weight: 6
sidebar:
  open: false
---

HTTP header fields follow a strict grammar: `field-name ":" OWS field-value OWS`. RFC 9112 Section 5 and RFC 9110 Section 5.6.2 define what constitutes a valid header. Violations can lead to parser disagreements and smuggling.

## Key Rules

**Space before colon** — the only header syntax violation with an explicit MUST-400:

> "A server **MUST** reject, with a response status code of 400 (Bad Request), any received request message that contains whitespace between a header field name and colon." — RFC 9112 Section 5

**Obs-fold** (line folding with leading whitespace):

> "A server that receives an obs-fold in a request message that is not within a message/http container **MUST** either reject the message by sending a 400 (Bad Request)... or replace each received obs-fold with one or more SP octets prior to interpreting the field value." — RFC 9112 Section 5.2

**Field name** must be a `token` = `1*tchar`, meaning at least one valid token character. Empty names, non-ASCII bytes, and special characters are all violations.

## Tests

{{< cards >}}
  {{< card link="sp-before-colon" title="SP-BEFORE-COLON" subtitle="Space between field name and colon. MUST reject with 400." >}}
  {{< card link="obs-fold" title="OBS-FOLD" subtitle="Obsolete line folding. MUST reject with 400 or replace with SP." >}}
  {{< card link="empty-header-name" title="EMPTY-HEADER-NAME" subtitle="Leading colon with no field name." >}}
  {{< card link="invalid-header-name" title="INVALID-HEADER-NAME" subtitle="Non-token characters in field name." >}}
  {{< card link="header-no-colon" title="HEADER-NO-COLON" subtitle="Header line with no colon separator." >}}
  {{< card link="whitespace-before-headers" title="WHITESPACE-BEFORE-HEADERS" subtitle="Whitespace before the first header line." >}}
  {{< card link="expect-unknown" title="EXPECT-UNKNOWN" subtitle="Unknown Expect value. Should respond with 417." >}}
{{< /cards >}}
