---
title: Glossary
description: "Glossary — Http11Probe documentation"
sidebar:
  open: true
---

Reference documentation for every test in Http11Probe, organized by topic. Each page explains the RFC requirement, what the test sends, what response is expected, and why it matters.

{{< cards >}}
  {{< card link="rfc-basics" title="RFC Basics" subtitle="What RFCs are, how to read requirement levels (MUST/SHOULD/MAY), and which RFCs define HTTP/1.1." icon="book-open" >}}
  {{< card link="line-endings" title="Line Endings" subtitle="CRLF requirements, bare LF handling, and bare CR rejection per RFC 9112 Section 2.2." icon="code" >}}
  {{< card link="request-line" title="Request Line" subtitle="Request-line format, multiple spaces, missing target, fragments, HTTP version validation." icon="terminal" >}}
  {{< card link="headers" title="Header Syntax" subtitle="Obs-fold, space before colon, empty names, invalid characters, missing colon." icon="document-text" >}}
  {{< card link="host-header" title="Host Header" subtitle="Missing Host, duplicate Host — the only tests where RFC explicitly mandates 400." icon="server" >}}
  {{< card link="content-length" title="Content-Length" subtitle="Non-numeric CL, plus sign, integer overflow, leading zeros, negative values." icon="calculator" >}}
  {{< card link="smuggling" title="Request Smuggling" subtitle="CL+TE conflicts, TE obfuscation, pipeline injection, and why ambiguous framing is dangerous." icon="shield-exclamation" >}}
  {{< card link="malformed-input" title="Malformed Input" subtitle="Binary garbage, oversized fields, control characters, incomplete requests." icon="lightning-bolt" >}}
{{< /cards >}}
