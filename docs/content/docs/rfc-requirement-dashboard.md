---
title: "RFC Requirement Dashboard"
description: "Complete RFC 2119 requirement-level analysis for all 194 Http11Probe tests"
weight: 2
breadcrumbs: false
---

This dashboard classifies every Http11Probe test by its [RFC 2119](https://www.rfc-editor.org/rfc/rfc2119) requirement level (**MUST**, **SHOULD**, **MAY**, **"ought to"**), with the exact RFC quote that proves it. Tests are grouped by requirement level, then by suite.

## Summary

| Requirement Level | Count | Meaning (RFC 2119) |
|---|---|---|
| **MUST** | 113 | Absolute requirement — no compliant implementation may deviate |
| **SHOULD** | 29 | Recommended — valid exceptions exist but must be understood |
| **MAY** | 10 | Truly optional — either behavior is fully compliant |
| **"ought to"** | 1 | Weaker than SHOULD — recommended but not normative |
| **Unscored** | 30 | Informational — no pass/fail judgement |
| **N/A** | 11 | Best-practice / no single RFC verb applies |

**Total: 194 tests**

---

## MUST-Level Requirements (113 tests)

These tests enforce absolute RFC requirements. A compliant server has no discretion — it **MUST** behave as specified.

### MUST — Reject with 400 (No Alternatives)

These are the strictest tests: the RFC mandates exactly `400 (Bad Request)`. Connection close alone does **not** satisfy the requirement.

| # | Test ID | Suite | RFC | RFC Quote |
|---|---------|-------|-----|-----------|
| 1 | `RFC9110-5.6.2-SP-BEFORE-COLON` | Compliance | [RFC 9112 §5.1](https://www.rfc-editor.org/rfc/rfc9112#section-5) | "A server **MUST** reject, with a response status code of 400 (Bad Request), any received request message that contains whitespace between a header field name and colon." |
| 2 | `RFC9112-5.1-OBS-FOLD` | Compliance | [RFC 9112 §5.2](https://www.rfc-editor.org/rfc/rfc9112#section-5.2) | "A server that receives an obs-fold in a request message that is not within a 'message/http' container **MUST** either reject the message by sending a 400 (Bad Request)... or replace each received obs-fold with one or more SP octets." |
| 3 | `RFC9112-7.1-MISSING-HOST` | Compliance | [RFC 9112 §3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2) | "A server **MUST** respond with a 400 (Bad Request) status code to any HTTP/1.1 request message that lacks a Host header field and to any request message that contains more than one Host header field line or a Host header field with an invalid field value." |
| 4 | `RFC9110-5.4-DUPLICATE-HOST` | Compliance | [RFC 9112 §3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2) | "A server **MUST** respond with a 400 (Bad Request) status code to any... request message that contains more than one Host header field line or a Host header field with an invalid field value." |
| 5 | `COMP-DUPLICATE-HOST-SAME` | Compliance | [RFC 9112 §3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2) | "A server **MUST** respond with a 400 (Bad Request) status code to any... request message that contains more than one Host header field line..." (applies even when both values are identical) |
| 6 | `RFC9112-3-CR-ONLY-LINE-ENDING` | Compliance | [RFC 9112 §2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) | "A recipient of such a bare CR **MUST** consider that element to be invalid or replace each bare CR with SP before processing the element or forwarding the message." |

### MUST — Reject (400 or Connection Close Acceptable)

The RFC requires rejection, but the mechanism (400 status or connection close) has some flexibility.

| # | Test ID | Suite | RFC | RFC Quote |
|---|---------|-------|-----|-----------|
| 7 | `RFC9112-5-EMPTY-HEADER-NAME` | Compliance | [RFC 9112 §5](https://www.rfc-editor.org/rfc/rfc9112#section-5) | Grammar: `field-name = token`, `token = 1*tchar`. An empty field name violates 1*tchar (requires at least one character). |
| 8 | `RFC9112-5-INVALID-HEADER-NAME` | Compliance | [RFC 9112 §5](https://www.rfc-editor.org/rfc/rfc9112#section-5) | Grammar: `token = 1*tchar`, `tchar = "!" / "#" / ...`. Brackets are not in the tchar set — implicit **MUST** reject. |
| 9 | `RFC9112-5-HEADER-NO-COLON` | Compliance | [RFC 9112 §5](https://www.rfc-editor.org/rfc/rfc9112#section-5) | Grammar: `field-line = field-name ":" OWS field-value OWS`. The colon is mandatory — implicit **MUST** reject. |
| 10 | `COMP-WHITESPACE-BEFORE-HEADERS` | Compliance | [RFC 9112 §2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) | "A recipient that receives whitespace between the start-line and the first header field **MUST** either reject the message as invalid or consume each whitespace-preceded line without further processing." |
| 11 | `RFC9112-3-MISSING-TARGET` | Compliance | [RFC 9112 §3](https://www.rfc-editor.org/rfc/rfc9112#section-3) | Grammar: `request-line = method SP request-target SP HTTP-version`. Missing target violates mandatory grammar — implicit **MUST**. |
| 12 | `COMP-ASTERISK-WITH-GET` | Compliance | [RFC 9112 §3.2.4](https://www.rfc-editor.org/rfc/rfc9112#section-3.2.4) | "If a proxy receives an OPTIONS request with an absolute-form of request-target in which the URI has an empty path and no query component, then the last proxy on the request chain **MUST** send a request-target of '*' when it forwards the request to the indicated origin server." Only OPTIONS may use asterisk-form. |
| 13 | `COMP-HOST-WITH-USERINFO` | Compliance | [RFC 9112 §3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2) | "A server **MUST** respond with a 400 (Bad Request)... to any request message that contains... a Host header field with an invalid field value." (userinfo component is not valid in Host) |
| 14 | `COMP-HOST-WITH-PATH` | Compliance | [RFC 9112 §3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2) | "A server **MUST** respond with a 400 (Bad Request)... to any request message that contains... a Host header field with an invalid field value." (path component is not valid in Host) |
| 15 | `COMP-HOST-EMPTY-VALUE` | Compliance | [RFC 9112 §3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2) | "A server **MUST** respond with a 400 (Bad Request)... to any request message that contains... a Host header field with an invalid field value." |
| 16 | `COMP-VERSION-MISSING-MINOR` | Compliance | [RFC 9112 §2.3](https://www.rfc-editor.org/rfc/rfc9112#section-2.3) | Grammar: `HTTP-version = HTTP-name "/" DIGIT "." DIGIT`. "HTTP/1" has no minor version digit — violates the grammar. **MUST** reject. |
| 17 | `COMP-VERSION-LEADING-ZEROS` | Compliance | [RFC 9112 §2.3](https://www.rfc-editor.org/rfc/rfc9112#section-2.3) | Grammar: `HTTP-version = HTTP-name "/" DIGIT "." DIGIT`. Each version component is exactly one DIGIT — "01" is two digits. **MUST** reject. |
| 18 | `COMP-VERSION-WHITESPACE` | Compliance | [RFC 9112 §2.3](https://www.rfc-editor.org/rfc/rfc9112#section-2.3) | Grammar: `HTTP-version = HTTP-name "/" DIGIT "." DIGIT`. No whitespace is permitted within the version token. **MUST** reject. |
| 19 | `COMP-VERSION-CASE` | Compliance | [RFC 9112 §2.3](https://www.rfc-editor.org/rfc/rfc9112#section-2.3) | "HTTP-version is case-sensitive." `HTTP-name = %x48.54.54.50` — only uppercase octets match. **MUST** reject lowercase `http/1.1`. |
| 20 | `COMP-SPACE-IN-TARGET` | Compliance | [RFC 9112 §3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2) | Unencoded space in request-target makes the request-line ambiguous (four tokens instead of three). Grammar: `request-line = method SP request-target SP HTTP-version`. **MUST** reject. |
| 19 | `RFC9112-6.1-CL-NON-NUMERIC` | Compliance | [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3) | "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient **MUST** treat it as an unrecoverable error... the server **MUST** respond with a 400 (Bad Request) status code and then close the connection." |
| 20 | `RFC9112-6.1-CL-PLUS-SIGN` | Compliance | [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3) | Same as above — `Content-Length = 1*DIGIT`. A plus sign is not a DIGIT. **MUST** reject as invalid Content-Length. |
| 21 | `SMUG-DUPLICATE-CL` | Smuggling | [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) | "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient **MUST** treat it as an unrecoverable error." Duplicate CL with different values = invalid. |
| 22 | `SMUG-CL-COMMA-DIFFERENT` | Smuggling | [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) | Same as above — comma-separated Content-Length values that differ are invalid. **MUST** treat as unrecoverable error. |
| 23 | `SMUG-CL-NEGATIVE` | Smuggling | [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) | `Content-Length = 1*DIGIT`. A minus sign is not a DIGIT. Invalid Content-Length — **MUST** reject. |
| 24 | `SMUG-CL-NEGATIVE-ZERO` | Smuggling | [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) | `Content-Length = 1*DIGIT`. "-0" starts with a minus sign. Invalid Content-Length — **MUST** reject. |
| 25 | `SMUG-CL-UNDERSCORE` | Smuggling | [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) | `Content-Length = 1*DIGIT`. Underscore is not a DIGIT. Invalid Content-Length — **MUST** reject. |
| 26 | `SMUG-CL-OCTAL` | Smuggling | [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) | `Content-Length = 1*DIGIT`. "0o5" contains non-DIGIT characters. **MUST** reject. |
| 27 | `SMUG-CL-HEX-PREFIX` | Smuggling | [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) | `Content-Length = 1*DIGIT`. "0x5" contains 'x'. **MUST** reject. |
| 28 | `SMUG-CL-INTERNAL-SPACE` | Smuggling | [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) | `Content-Length = 1*DIGIT`. Space is not a DIGIT and not OWS within the value. **MUST** reject. |
| 29 | `SMUG-TE-SP-BEFORE-COLON` | Smuggling | [RFC 9112 §5.1](https://www.rfc-editor.org/rfc/rfc9112#section-5) | "A server **MUST** reject, with a response status code of 400 (Bad Request), any received request message that contains whitespace between a header field name and colon." (Same rule as SP-BEFORE-COLON, applied to Transfer-Encoding.) |
| 30 | `SMUG-TE-NOT-FINAL-CHUNKED` | Smuggling | [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3) | "If a Transfer-Encoding header field is present in a request and the chunked transfer coding is not the final encoding, the message body length cannot be determined reliably; the server **MUST** respond with the 400 (Bad Request) status code and then close the connection." |
| 31 | `SMUG-TE-HTTP10` | Smuggling | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) | "A server or client that receives an HTTP/1.0 message containing a Transfer-Encoding header field **MUST** treat the message as if the framing is faulty, even if a Content-Length is present, and close the connection after processing the message." |
| 32 | `SMUG-TE-EMPTY-VALUE` | Smuggling | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) | `Transfer-Encoding = #transfer-coding`, `transfer-coding = token = 1*tchar`. An empty value does not match 1*tchar. **MUST** reject. |
| 33 | `SMUG-TE-DUPLICATE-HEADERS` | Smuggling | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) | Two Transfer-Encoding headers with conflicting Content-Length creates ambiguous framing. The combined TE + CL violates "A sender **MUST NOT** send a Content-Length header field in any message that contains a Transfer-Encoding header field." **MUST** reject. |
| 34 | `SMUG-TE-IDENTITY` | Smuggling | [RFC 9112 §7](https://www.rfc-editor.org/rfc/rfc9112#section-7) | "The 'identity' transfer coding was used in HTTP/1.1 and has been removed from the registry." An unrecognized coding with CL present. **MUST** reject per §6.1. |
| 35 | `SMUG-TE-VTAB` | Smuggling | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) | `transfer-coding = token = 1*tchar`. Vertical tab (0x0B) is not a tchar. The TE value is syntactically invalid — **MUST** reject. |
| 36 | `SMUG-TE-FORMFEED` | Smuggling | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) | `transfer-coding = token = 1*tchar`. Form feed (0x0C) is not a tchar. The TE value is syntactically invalid — **MUST** reject. |
| 37 | `SMUG-TE-NULL` | Smuggling | [RFC 9110 §5.5](https://www.rfc-editor.org/rfc/rfc9110#section-5.5) | "Field values containing CR, LF, or NUL characters are invalid and dangerous... a recipient of CR, LF, or NUL within a field value **MUST** either reject the message or replace each of those characters with SP before further processing." |
| 38 | `SMUG-BARE-CR-HEADER-VALUE` | Smuggling | [RFC 9112 §2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) | "A recipient of such a bare CR **MUST** consider that element to be invalid or replace each bare CR with SP before processing the element or forwarding the message." |
| 39 | `SMUG-MULTIPLE-HOST-COMMA` | Smuggling | [RFC 9110 §7.2](https://www.rfc-editor.org/rfc/rfc9110#section-7.2) | "A server **MUST** respond with a 400 (Bad Request)... to any request message that contains... a Host header field with an invalid field value." Comma-separated values in Host are invalid. |
| 40 | `SMUG-TE-OBS-FOLD` | Smuggling | [RFC 9112 §5.2](https://www.rfc-editor.org/rfc/rfc9112#section-5.2) | "A server that receives an obs-fold in a request message... **MUST** either reject the message by sending a 400 (Bad Request)... or replace each received obs-fold with one or more SP octets." |
| 41 | `SMUG-CHUNK-BARE-SEMICOLON` | Smuggling | [RFC 9112 §7.1.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1.1) | Grammar: `chunk-ext = *( BWS ";" BWS chunk-ext-name [ "=" chunk-ext-val ] )`, `chunk-ext-name = token = 1*tchar`. Bare semicolon with no name violates the grammar. **MUST** reject. |
| 42 | `SMUG-CHUNK-HEX-PREFIX` | Smuggling | [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) | Grammar: `chunk-size = 1*HEXDIG`. "0x" prefix is not valid HEXDIG. **MUST** reject. |
| 43 | `SMUG-CHUNK-UNDERSCORE` | Smuggling | [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) | Grammar: `chunk-size = 1*HEXDIG`. Underscore is not a HEXDIG. **MUST** reject. |
| 44 | `SMUG-CHUNK-LEADING-SP` | Smuggling | [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) | Grammar: `chunk = chunk-size [ chunk-ext ] CRLF chunk-data CRLF`. No leading whitespace is permitted before chunk-size. **MUST** reject. |
| 45 | `SMUG-CHUNK-MISSING-TRAILING-CRLF` | Smuggling | [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) | Grammar: `chunk = chunk-size [ chunk-ext ] CRLF chunk-data CRLF`. The trailing CRLF after chunk-data is mandatory. **MUST** reject. |
| 46 | `SMUG-CHUNK-SPILL` | Smuggling | [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) | Chunk declares size 5 but sends 7 bytes. Server **MUST** read exactly chunk-size octets. Extra bytes corrupt framing. **MUST** reject. |
| 47 | `SMUG-CHUNK-NEGATIVE` | Smuggling | [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) | Grammar: `chunk-size = 1*HEXDIG`. A minus sign is not a HEXDIG. **MUST** reject. |
| 48 | `SMUG-CHUNK-EXT-CTRL` | Smuggling | [RFC 9112 §7.1.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1.1) | NUL byte (0x00) in chunk extension. NUL is not valid in any HTTP protocol element. RFC 9110 §5.5: "a recipient of CR, LF, or NUL within a field value **MUST** either reject the message or replace each of those characters with SP." |
| 49 | `SMUG-CHUNK-EXT-CR` | Smuggling | [RFC 9112 §2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) | "A recipient of such a bare CR **MUST** consider that element to be invalid or replace each bare CR with SP before processing the element or forwarding the message." Bare CR in chunk extension is invalid. |
| 50 | `SMUG-CHUNK-BARE-CR-TERM` | Smuggling | [RFC 9112 §2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) | "A recipient of such a bare CR **MUST** consider that element to be invalid or replace each bare CR with SP." Bare CR as line terminator is invalid — **MUST** reject. |
| 51 | `SMUG-CLTE-PIPELINE` | Smuggling | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) | "Regardless, the server **MUST** close the connection after responding to such a request to avoid the potential attacks." CL+TE combined — **MUST** close connection. |
| 52 | `SMUG-TECL-PIPELINE` | Smuggling | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) | Same as above — TE+CL combined in reverse smuggling direction. **MUST** close connection. |
| 53 | `SMUG-TE-XCHUNKED` | Smuggling | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) | Unknown TE with CL present: "Regardless, the server **MUST** close the connection after responding to such a request." Combined with §6.1: "A server that receives a request message with a transfer coding it does not understand **SHOULD** respond with 501." |
| 54 | `SMUG-CLTE-CONN-CLOSE` | Smuggling | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) | Sequence test: CL+TE combined, then follow-up GET on same socket. "The server **MUST** close the connection after responding to such a request." If follow-up receives a response, MUST-close violated. |
| 55 | `SMUG-TECL-CONN-CLOSE` | Smuggling | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) | Same as CLTE-CONN-CLOSE with TE before CL header order. **MUST** close connection. |
| 57 | `SMUG-CLTE-DESYNC` | Smuggling | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) | Classic CL.TE desync: CL=6 with TE=chunked body `0\r\n\r\nX`. Poison byte after CL boundary confirms desync. **MUST** close connection. |
| 58 | `SMUG-CLTE-SMUGGLED-GET` | Smuggling | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) | CL.TE desync payload where the trailing bytes form a full `GET /` request. If the server returns multiple HTTP responses on one send, the embedded request was executed. "Regardless, the server **MUST** close the connection after responding to such a request." |
| 59 | `SMUG-TECL-DESYNC` | Smuggling | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) | Reverse TE.CL desync: TE=chunked terminates at `0\r\n\r\n` but CL=30. Extra bytes on wire confirm desync. **MUST** close connection. |
| 60 | `SMUG-CHUNK-SIZE-PLUS` | Smuggling | [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) | Grammar: `chunk-size = 1*HEXDIG`. Leading `+` is not HEXDIG; invalid chunk framing **MUST** be rejected. |
| 61 | `SMUG-CHUNK-SIZE-TRAILING-OWS` | Smuggling | [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) | Grammar: `chunk-size = 1*HEXDIG`. Trailing whitespace in chunk-size is invalid syntax and **MUST** be rejected. |
| 62 | `SMUG-CHUNK-EXT-INVALID-TOKEN` | Smuggling | [RFC 9112 §7.1.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1.1) | Grammar: `chunk-ext-name = token`. `[` is not a valid token character, so the chunk extension is invalid and **MUST** be rejected. |
| 63 | `SMUG-OPTIONS-TE-OBS-FOLD` | Smuggling | [RFC 9112 §5.2](https://www.rfc-editor.org/rfc/rfc9112#section-5.2) | "A server that receives an obs-fold in a request message ... **MUST** either reject the message by sending a 400 (Bad Request) ... or replace each received obs-fold with one or more SP octets." |
| 64 | `SMUG-CHUNK-INVALID-SIZE-DESYNC` | Smuggling | [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) | Sequence test with invalid `+0` chunk-size plus poison byte. Since `chunk-size = 1*HEXDIG`, this framing error **MUST** be rejected to prevent desync. |
| 54 | `COMP-CONNECTION-CLOSE` | Compliance | [RFC 9112 §9.6](https://www.rfc-editor.org/rfc/rfc9112#section-9.6) | "A server that receives a 'close' connection option **MUST** initiate closure of the connection after it sends the final response to the request that contained the 'close' connection option." |
| 55 | `COMP-OPTIONS-STAR` | Compliance | [RFC 9112 §3.2.4](https://www.rfc-editor.org/rfc/rfc9112#section-3.2.4) | The asterisk-form `*` is defined only for OPTIONS. A valid OPTIONS * request **MUST** be accepted. |
| 56 | `COMP-POST-CL-BODY` | Compliance | [RFC 9112 §6.2](https://www.rfc-editor.org/rfc/rfc9112#section-6.2) | "If a valid Content-Length header field is present without Transfer-Encoding, its decimal value defines the expected message body length in octets." Server **MUST** accept a well-formed POST with matching body. |
| 57 | `COMP-POST-CL-ZERO` | Compliance | [RFC 9112 §6.2](https://www.rfc-editor.org/rfc/rfc9112#section-6.2) | Content-Length: 0 is a valid 1*DIGIT value. Server **MUST** accept zero-length body. |
| 58 | `COMP-POST-NO-CL-NO-TE` | Compliance | [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3) | "If this is a request message and none of the above are true, then the message body length is zero (no message body is present)." Server **MUST** treat as zero-length. |
| 59 | `COMP-RANGE-POST` | Compliance | [RFC 9110 §14.2](https://www.rfc-editor.org/rfc/rfc9110#section-14.2) | "A server **MUST** ignore a Range header field received with a request method other than GET." |
| 60 | `COMP-UPGRADE-HTTP10` | Compliance | [RFC 9110 §7.8](https://www.rfc-editor.org/rfc/rfc9110#section-7.8) | "A server **MUST** ignore an Upgrade header field that is received in an HTTP/1.0 request." |
| 59 | `COMP-CHUNKED-BODY` | Compliance | [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) | "A recipient **MUST** be able to parse and decode the chunked transfer coding." |
| 60 | `COMP-CHUNKED-MULTI` | Compliance | [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) | Same — multi-chunk is the standard chunked format. **MUST** accept. |
| 61 | `COMP-CHUNKED-EMPTY` | Compliance | [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) | A zero-length chunked body (just `0\r\n\r\n`) is valid. **MUST** accept. |
| 62 | `COMP-CHUNKED-HEX-UPPERCASE` | Compliance | [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) | `chunk-size = 1*HEXDIG`. HEXDIG includes A-F. **MUST** accept uppercase hex. |
| 63 | `COMP-CHUNKED-TRAILER-VALID` | Compliance | [RFC 9112 §7.1.2](https://www.rfc-editor.org/rfc/rfc9112#section-7.1.2) | "A recipient **MUST** be able to parse and decode the chunked transfer coding." Trailers are part of the chunked format. **MUST** accept. |
| 64 | `COMP-CHUNKED-EXTENSION` | Compliance | [RFC 9112 §7.1.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1.1) | "A recipient **MUST** ignore unrecognized chunk extensions." Server **MUST** accept and ignore. |
| 65 | `COMP-POST-CL-UNDERSEND` | Compliance | [RFC 9112 §6.2](https://www.rfc-editor.org/rfc/rfc9112#section-6.2) | "If the sender closes the connection or the recipient times out before the indicated number of octets are received, the recipient **MUST** consider the message to be incomplete and close the connection." |
| 66 | `COMP-CHUNKED-NO-FINAL` | Compliance | [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) | "The message body length is determined by reading and decoding the chunked data until the transfer coding indicates the data is complete." Without a zero terminator, the transfer is incomplete. Server **MUST** not process as complete. |
| 67 | `COMP-UPGRADE-POST` | Compliance | [RFC 6455 §4.1](https://www.rfc-editor.org/rfc/rfc6455#section-4.1) | "The method of the request **MUST** be GET, and the HTTP version **MUST** be at least 1.1." WebSocket upgrade via POST **MUST** not succeed. |
| 68 | `COMP-UPGRADE-MISSING-CONN` | Compliance | [RFC 9110 §7.8](https://www.rfc-editor.org/rfc/rfc9110#section-7.8) | "A sender of Upgrade **MUST** also send an 'Upgrade' connection option in the Connection header field." Without Connection: Upgrade, the server **MUST NOT** switch protocols. |
| 69 | `COMP-UPGRADE-UNKNOWN` | Compliance | [RFC 9110 §7.8](https://www.rfc-editor.org/rfc/rfc9110#section-7.8) | "A server **MUST NOT** switch to a protocol that was not indicated by the client in the corresponding request's Upgrade header field." Unknown protocol — **MUST NOT** return 101. |
| 70 | `MAL-NUL-IN-URL` | Malformed | [RFC 9112 §2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) | NUL byte is not valid in any protocol element. Grammar violation — **MUST** reject. |
| 71 | `MAL-CONTROL-CHARS-HEADER` | Malformed | [RFC 9110 §5.5](https://www.rfc-editor.org/rfc/rfc9110#section-5.5) | "Field values containing CR, LF, or NUL characters are invalid and dangerous... a recipient **MUST** either reject the message or replace each of those characters with SP." (CTL characters outside safe context.) |
| 72 | `MAL-NON-ASCII-HEADER-NAME` | Malformed | [RFC 9112 §5](https://www.rfc-editor.org/rfc/rfc9112#section-5) | `field-name = token = 1*tchar`. Non-ASCII bytes are not tchar. Grammar violation — **MUST** reject. |
| 73 | `MAL-NON-ASCII-URL` | Malformed | [RFC 9112 §3](https://www.rfc-editor.org/rfc/rfc9112#section-3) | Non-ASCII bytes in the request-target violate the URI grammar. "A recipient **SHOULD NOT** attempt to autocorrect... since the invalid request-line might be deliberately crafted to bypass security filters." |
| 74 | `MAL-CL-OVERFLOW` | Malformed | [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) | "A recipient **MUST** anticipate potentially large decimal numerals and prevent parsing errors due to integer conversion overflows or precision loss due to integer conversion." |
| 75 | `MAL-NUL-IN-HEADER-VALUE` | Malformed | [RFC 9110 §5.5](https://www.rfc-editor.org/rfc/rfc9110#section-5.5) | "Field values containing CR, LF, or NUL characters are invalid and dangerous... a recipient of CR, LF, or NUL within a field value **MUST** either reject the message or replace each of those characters with SP." |
| 76 | `MAL-CHUNK-SIZE-OVERFLOW` | Malformed | [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) | "Recipients **MUST** anticipate potentially large hexadecimal numerals and prevent parsing errors due to integer conversion overflows or precision loss due to integer conversion." |
| 77 | `MAL-CL-EMPTY` | Malformed | [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) | `Content-Length = 1*DIGIT`. Empty value does not match 1*DIGIT (requires at least one). **MUST** reject as invalid CL. |
| 78 | `MAL-URL-OVERLONG-UTF8` | Malformed | [RFC 3629 §3](https://www.rfc-editor.org/rfc/rfc3629#section-3) | "Implementations of the decoding algorithm above **MUST** protect against decoding invalid sequences." Overlong UTF-8 (0xC0 0xAF) is explicitly invalid per RFC 3629. |
| 79 | `NORM-SP-BEFORE-COLON-CL` | Normalization | [RFC 9112 §5.1](https://www.rfc-editor.org/rfc/rfc9112#section-5) | "A server **MUST** reject, with a response status code of 400 (Bad Request), any received request message that contains whitespace between a header field name and colon." |
| 80 | `NORM-TAB-IN-NAME` | Normalization | [RFC 9112 §5](https://www.rfc-editor.org/rfc/rfc9112#section-5) | `field-name = token = 1*tchar`. Tab (0x09) is not a tchar. **MUST** reject — invalid token character. |
| 81 | `COMP-UPGRADE-INVALID-VER` | Compliance | [RFC 6455 §4.4](https://www.rfc-editor.org/rfc/rfc6455#section-4.4) | "If the server doesn't support the requested version, it **MUST** abort the WebSocket handshake." (426 Upgrade Required preferred.) |
| 82 | `COMP-UNKNOWN-TE-501` | Compliance | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) | "A server that receives a request message with a transfer coding it does not understand **SHOULD** respond with 501." Combined with unknown-TE-without-CL making body length indeterminate: **MUST** reject. |
| 83 | `SMUG-TE-TRAILING-SPACE` | Smuggling | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) | "chunked " (with trailing space) is not an exact match for the registered coding "chunked". Combined with CL present: "the server **MUST** close the connection after responding." |
| 84 | `MAL-POST-CL-HUGE-NO-BODY` | Malformed | [RFC 9112 §6.2](https://www.rfc-editor.org/rfc/rfc9112#section-6.2) | "If the sender closes the connection or the recipient times out before the indicated number of octets are received, the recipient **MUST** consider the message to be incomplete and close the connection." |
| 85 | `COMP-HEAD-NO-BODY` | Compliance | [RFC 9110 §9.3.2](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.2) | "The HEAD method is identical to GET except that the server **MUST NOT** send content in the response." |
| 86 | `COMP-405-ALLOW` | Compliance | [RFC 9110 §15.5.6](https://www.rfc-editor.org/rfc/rfc9110#section-15.5.6) | "The origin server **MUST** generate an Allow header field in a 405 response containing a list of the target resource's currently supported methods." |
| 87 | `COMP-DATE-HEADER` | Compliance | [RFC 9110 §6.6.1](https://www.rfc-editor.org/rfc/rfc9110#section-6.6.1) | "An origin server with a clock **MUST** generate a Date header field in all 2xx (Successful), 3xx (Redirection), and 4xx (Client Error) responses." |
| 88 | `COMP-NO-1XX-HTTP10` | Compliance | [RFC 9110 §15.2](https://www.rfc-editor.org/rfc/rfc9110#section-15.2) | "Since HTTP/1.0 did not define any 1xx status codes, a server **MUST NOT** send a 1xx response to an HTTP/1.0 client." |
| 89 | `COMP-NO-CL-IN-204` | Compliance | [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) | "A server **MUST NOT** send a Content-Length header field in any response with a status code of 1xx (Informational) or 204 (No Content)." |
| 90 | `SMUG-CLTE-SMUGGLED-GET-CL-PLUS` | Smuggling | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) | Variant of `SMUG-CLTE-SMUGGLED-GET` with `Content-Length: +N` (malformed CL) and `Transfer-Encoding: chunked`, embedding a full `GET /` in the body. "Regardless, the server **MUST** close the connection after responding to such a request." |
| 91 | `SMUG-CLTE-SMUGGLED-GET-CL-NON-NUMERIC` | Smuggling | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) | Variant of `SMUG-CLTE-SMUGGLED-GET` with `Content-Length: N<alpha>` (non-numeric suffix) and `Transfer-Encoding: chunked`, embedding a full `GET /` in the body. "Regardless, the server **MUST** close the connection after responding to such a request." |
| 92 | `SMUG-CLTE-SMUGGLED-GET-TE-OBS-FOLD` | Smuggling | [RFC 9112 §5.2](https://www.rfc-editor.org/rfc/rfc9112#section-5.2) | Variant of `SMUG-CLTE-SMUGGLED-GET` with obs-folded `Transfer-Encoding:\r\n chunked` plus `Content-Length`, embedding a full `GET /` in the body. "A server that receives an obs-fold in a request message... **MUST** either reject the message by sending a 400 (Bad Request)... or replace each received obs-fold with one or more SP octets prior to interpreting the field value..." |
| 93 | `SMUG-CLTE-SMUGGLED-HEAD` | Smuggling | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) | Embedded-request confirmation variant using a smuggled `HEAD /` request. "Regardless, the server **MUST** close the connection after responding to such a request." |
| 94 | `SMUG-CLTE-SMUGGLED-GET-TE-TRAILING-SPACE` | Smuggling | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) | Variant of `SMUG-CLTE-SMUGGLED-GET` with `Transfer-Encoding: chunked␠` (trailing space) plus `Content-Length`. "Regardless, the server **MUST** close the connection after responding to such a request." |
| 95 | `SMUG-CLTE-SMUGGLED-GET-TE-LEADING-COMMA` | Smuggling | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) | Variant of `SMUG-CLTE-SMUGGLED-GET` with `Transfer-Encoding: , chunked` plus `Content-Length`. "Regardless, the server **MUST** close the connection after responding to such a request." |
| 96 | `SMUG-CLTE-SMUGGLED-GET-TE-CASE-MISMATCH` | Smuggling | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) | Variant of `SMUG-CLTE-SMUGGLED-GET` with `Transfer-Encoding: Chunked` (case mismatch) plus `Content-Length`. "Regardless, the server **MUST** close the connection after responding to such a request." |
| 97 | `SMUG-TE-DUPLICATE-HEADERS-SMUGGLED-GET` | Smuggling | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) | Sequence confirmation variant using duplicate `Transfer-Encoding` header fields (`chunked` + `identity`) plus `Content-Length`, embedding a full `GET /` in the body. "Regardless, the server **MUST** close the connection after responding to such a request." |
| 98 | `SMUG-TECL-SMUGGLED-GET` | Smuggling | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) | TE.CL confirmation using a chunk-size prefix trick: `Content-Length` covers only the chunk-size line, leaving chunk-data that begins with a `GET /` request. "Regardless, the server **MUST** close the connection after responding to such a request." |
| 99 | `SMUG-DUPLICATE-CL-SMUGGLED-GET` | Smuggling | [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) | Sequence confirmation variant of duplicate `Content-Length` using an embedded `GET /` immediately after the shorter body's boundary. "If a message is received without Transfer-Encoding and with an invalid Content-Length header field... the recipient **MUST** treat it as an unrecoverable error." |

---

## SHOULD-Level Requirements (29 tests)

The RFC recommends this behavior. Valid exceptions exist but must be understood and justified.

| # | Test ID | Suite | RFC | RFC Quote |
|---|---------|-------|-----|-----------|
| 1 | `COMP-HTTP10-DEFAULT-CLOSE` | Compliance | [RFC 9112 §9.3](https://www.rfc-editor.org/rfc/rfc9112#section-9.3) | "HTTP implementations **SHOULD** support persistent connections." HTTP/1.0 without keep-alive: server **SHOULD** close after response. |
| 2 | `COMP-LEADING-CRLF` | Compliance | [RFC 9112 §2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) | "A server that is expecting to receive and parse a request-line **SHOULD** ignore at least one empty line (CRLF) received prior to the request-line." |
| 3 | `RFC9112-3-MULTI-SP-REQUEST-LINE` | Compliance | [RFC 9112 §3](https://www.rfc-editor.org/rfc/rfc9112#section-3) | "A recipient **SHOULD NOT** attempt to autocorrect and then process the request without a redirect, since the invalid request-line might be deliberately crafted to bypass security filters." Multiple spaces = invalid request-line grammar. |
| 4 | `RFC9112-3.2-FRAGMENT-IN-TARGET` | Compliance | [RFC 9112 §3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2) | "Recipients of an invalid request-line **SHOULD** respond with either a 400 (Bad Request) error or a 301 (Moved Permanently) redirect." Fragment (#) is not part of origin-form. |
| 5 | `RFC9112-2.3-HTTP09-REQUEST` | Compliance | [RFC 9112 §2.3](https://www.rfc-editor.org/rfc/rfc9112#section-2.3) | HTTP/0.9 has no version token. "Recipients of an invalid request-line **SHOULD** respond with either a 400 (Bad Request) error." |
| 6 | `RFC9112-2.3-INVALID-VERSION` | Compliance | [RFC 9112 §2.3](https://www.rfc-editor.org/rfc/rfc9112#section-2.3) | Invalid HTTP-version format. "Recipients of an invalid request-line **SHOULD** respond with either a 400 (Bad Request) error." (No explicit MUST — the Requirement field says "No MUST".) |
| 7 | `COMP-REQUEST-LINE-TAB` | Compliance | [RFC 9112 §3](https://www.rfc-editor.org/rfc/rfc9112#section-3) | Grammar: `request-line = method SP request-target SP HTTP-version`. SP is specifically 0x20. Tab is not SP. "A recipient **SHOULD NOT** attempt to autocorrect." **SHOULD** reject, **MAY** accept. |
| 8 | `COMP-METHOD-TRACE` | Compliance | [RFC 9110 §9.3.8](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.8) | "A client **MUST NOT** send content in a TRACE request." Servers **SHOULD** disable TRACE in production to prevent cross-site tracing (XST). |
| 9 | `COMP-TRACE-WITH-BODY` | Compliance | [RFC 9110 §9.3.8](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.8) | "A client **MUST NOT** send content in a TRACE request." Server **SHOULD** reject TRACE with body — 400/405 preferred. |
| 10 | `COMP-METHOD-CONNECT` | Compliance | [RFC 9110 §9.3.6](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.6) | "An origin server **MAY** accept a CONNECT request, but most origin servers do not implement CONNECT." Origin server **SHOULD** reject with 400/405/501. |
| 11 | `SMUG-CL-LEADING-ZEROS` | Smuggling | [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) | `Content-Length = 1*DIGIT`. "0005" matches the grammar but creates parser-disagreement risk (octal vs decimal interpretation). **SHOULD** reject. |
| 12 | `SMUG-CL-TRAILING-SPACE` | Smuggling | [RFC 9110 §5.5](https://www.rfc-editor.org/rfc/rfc9110#section-5.5) | OWS trimming is valid per `field-value = *( field-content )`. But trailing space in CL value is unusual. **SHOULD** be trimmed per OWS rules. |
| 13 | `SMUG-CL-EXTRA-LEADING-SP` | Smuggling | [RFC 9110 §5.5](https://www.rfc-editor.org/rfc/rfc9110#section-5.5) | Extra leading whitespace (double space) as OWS. Valid per `OWS = *( SP / HTAB )` but unusual. **SHOULD** trim. |
| 14 | `SMUG-CL-DOUBLE-ZERO` | Smuggling | [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) | "00" matches 1*DIGIT grammar but leading zero creates parser ambiguity. **SHOULD** reject — same class as CL-LEADING-ZEROS. |
| 15 | `SMUG-CL-LEADING-ZEROS-OCTAL` | Smuggling | [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) | "0200" — octal 128 vs decimal 200. Parser disagreement vector. **SHOULD** reject to eliminate ambiguity. |
| 16 | `SMUG-TE-DOUBLE-CHUNKED` | Smuggling | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) | "A sender **MUST NOT** apply the chunked transfer coding more than once." Duplicate chunked with CL — ambiguous. **SHOULD** reject. |
| 17 | `SMUG-TE-CASE-MISMATCH` | Smuggling | [RFC 9110 §7.8](https://www.rfc-editor.org/rfc/rfc9110#section-7.8) | "Recipients **SHOULD** use case-insensitive comparison when matching each protocol-name." "Chunked" (capital C) **SHOULD** be recognized. |
| 18 | `SMUG-TE-TRAILING-COMMA` | Smuggling | [RFC 9110 §5.6.1](https://www.rfc-editor.org/rfc/rfc9110#section-5.6.1) | "A sender **MUST NOT** generate empty list elements." But "A recipient **MUST** parse and ignore a reasonable number of empty list elements." **SHOULD** handle gracefully. |
| 19 | `SMUG-TE-LEADING-COMMA` | Smuggling | [RFC 9110 §5.6.1](https://www.rfc-editor.org/rfc/rfc9110#section-5.6.1) | Same — leading comma creates empty list element. "A recipient **MUST** parse and ignore a reasonable number of empty list elements." **SHOULD** handle. |
| 20 | `MAL-BINARY-GARBAGE` | Malformed | [RFC 9112 §2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) | "The server **SHOULD** respond with a 400 (Bad Request) response and close the connection." |
| 21 | `MAL-LONG-URL` | Malformed | [RFC 9112 §3](https://www.rfc-editor.org/rfc/rfc9112#section-3) | "A server that receives a request-target longer than any URI it wishes to parse **MUST** respond with a 414 (URI Too Long) status code." (MUST for 414 specifically, but SHOULD for having a limit.) |
| 22 | `MAL-LONG-METHOD` | Malformed | [RFC 9112 §3](https://www.rfc-editor.org/rfc/rfc9112#section-3) | "A server that receives a method longer than any that it implements **SHOULD** respond with a 501 (Not Implemented) status code." |
| 23 | `MAL-LONG-HEADER-VALUE` | Malformed | [RFC 9110 §5.4](https://www.rfc-editor.org/rfc/rfc9110#section-5.4) | "A server that receives a request header field line, field value, or set of fields larger than it wishes to process **MUST** respond with an appropriate 4xx (Client Error) status code." (MUST for 4xx, SHOULD for having a limit.) |
| 24 | `MAL-LONG-HEADER-NAME` | Malformed | [RFC 9110 §5.4](https://www.rfc-editor.org/rfc/rfc9110#section-5.4) | Same as above. |
| 25 | `COMP-UNKNOWN-METHOD` | Compliance | [RFC 9110 §9.1](https://www.rfc-editor.org/rfc/rfc9110#section-9.1) | "An origin server that receives a request method that is unrecognized or not implemented **SHOULD** respond with the 501 (Not Implemented) status code." |
| 26 | `COMP-OPTIONS-ALLOW` | Compliance | [RFC 9110 §9.3.7](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.7) | "A server generating a successful response to OPTIONS **SHOULD** send any header that might indicate optional features implemented by the server and applicable to the target resource (e.g., Allow)." |
| 27 | `COMP-CONTENT-TYPE` | Compliance | [RFC 9110 §8.3](https://www.rfc-editor.org/rfc/rfc9110#section-8.3) | "A sender that generates a message containing content **SHOULD** generate a Content-Type header field in the message." |
| 28 | `COMP-LONG-URL-OK` | Compliance | [RFC 9112 §3](https://www.rfc-editor.org/rfc/rfc9112#section-3) | "It is RECOMMENDED that all HTTP senders and recipients support, at a minimum, request-line lengths of 8000 octets." Server **SHOULD** accept ~7900-char path. |
| 29 | `COMP-DUPLICATE-CT` | Compliance | [RFC 9110 §5.3](https://www.rfc-editor.org/rfc/rfc9110#section-5.3) | "A sender **MUST NOT** generate multiple header fields with the same field name." Content-Type is not list-based — duplicate values **SHOULD** be rejected with 400. |

---

## MAY-Level Requirements (10 tests)

The RFC explicitly permits either behavior. Both acceptance and rejection are fully compliant.

| # | Test ID | Suite | RFC | RFC Quote |
|---|---------|-------|-----|-----------|
| 1 | `RFC9112-2.2-BARE-LF-REQUEST-LINE` | Compliance | [RFC 9112 §2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) | "A recipient **MAY** recognize a single LF as a line terminator and ignore any preceding CR." |
| 2 | `RFC9112-2.2-BARE-LF-HEADER` | Compliance | [RFC 9112 §2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) | Same — bare LF in headers. **MAY** accept. |
| 3 | `COMP-EXPECT-UNKNOWN` | Compliance | [RFC 9110 §10.1.1](https://www.rfc-editor.org/rfc/rfc9110#section-10.1.1) | "A server that receives an Expect field value containing a member other than 100-continue **MAY** respond with a 417 (Expectation Failed) status code." |
| 4 | `COMP-GET-WITH-CL-BODY` | Compliance | [RFC 9110 §9.3.1](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.1) | "Content received in a GET request has no generally defined semantics, cannot alter the meaning or target of the request, and might lead some implementations to reject the request." **MAY** reject. |
| 5 | `SMUG-CHUNK-EXT-LF` | Smuggling | [RFC 9112 §2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) | "A recipient **MAY** recognize a single LF as a line terminator." Bare LF in chunk extension — **MAY** accept. |
| 6 | `SMUG-CHUNK-LF-TERM` | Smuggling | [RFC 9112 §2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) | Same — bare LF as chunk data terminator. **MAY** accept. |
| 7 | `SMUG-CHUNK-LF-TRAILER` | Smuggling | [RFC 9112 §2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) | Same — bare LF in chunked trailer termination. **MAY** accept. |
| 8 | `COMP-HTTP10-NO-HOST` | Compliance | [RFC 9112 §3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2) | "A client **MUST** send a Host header field in all HTTP/1.1 request messages." This requirement applies only to HTTP/1.1. HTTP/1.0 without Host is valid. **MAY** accept or reject. |
| 9 | `COMP-HTTP12-VERSION` | Compliance | [RFC 9112 §2.3](https://www.rfc-editor.org/rfc/rfc9112#section-2.3) | HTTP/1.2 has a higher minor version. A server **MAY** accept or return 505. |
| 10 | `MAL-CL-TAB-BEFORE-VALUE` | Malformed | [RFC 9110 §5.6.3](https://www.rfc-editor.org/rfc/rfc9110#section-5.6.3) | `OWS = *( SP / HTAB )`. Tab is valid optional whitespace after the colon. Fully compliant — **MAY** accept. |

---

## "ought to" Level (1 test)

Weaker than SHOULD — recommends but does not normatively require.

| # | Test ID | Suite | RFC | RFC Quote |
|---|---------|-------|-----|-----------|
| 1 | `SMUG-CL-TE-BOTH` | Smuggling | [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3) | "Such a message might indicate an attempt to perform request smuggling... and **ought to** be handled as an error." §6.1: "A server **MAY** reject a request that contains both Content-Length and Transfer-Encoding or process such a request in accordance with the Transfer-Encoding alone." |

---

## Unscored Tests (30 tests)

These tests are informational — they produce warnings but never fail.

| # | Test ID | Suite | RFC | Notes |
|---|---------|-------|-----|-------|
| 1 | `SMUG-TRANSFER_ENCODING` | Smuggling | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) | `Transfer_Encoding` (underscore) is a valid token but not the standard header. Some parsers normalize. |
| 2 | `SMUG-CL-COMMA-SAME` | Smuggling | [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) | "A recipient of a Content-Length header field value consisting of the same decimal value repeated as a comma-separated list **MAY** either reject the message as invalid or replace that invalid field value with a single instance." |
| 3 | `SMUG-CL-COMMA-TRIPLE` | Smuggling | [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) | Same — three comma-separated identical CL values. Extended merge test. |
| 4 | `SMUG-CHUNKED-WITH-PARAMS` | Smuggling | [RFC 9112 §7](https://www.rfc-editor.org/rfc/rfc9112#section-7) | "The chunked coding does not define any parameters. Their presence **SHOULD** be treated as an error." |
| 5 | `SMUG-EXPECT-100-CL` | Smuggling | [RFC 9110 §10.1.1](https://www.rfc-editor.org/rfc/rfc9110#section-10.1.1) | Expect: 100-continue with CL — standard behavior, tested for proxy interaction. |
| 6 | `SMUG-TRAILER-CL` | Smuggling | [RFC 9110 §6.5.1](https://www.rfc-editor.org/rfc/rfc9110#section-6.5.1) | Content-Length in trailers — prohibited trailer field. **MUST NOT** be used for framing. |
| 7 | `SMUG-TRAILER-TE` | Smuggling | [RFC 9110 §6.5.1](https://www.rfc-editor.org/rfc/rfc9110#section-6.5.1) | Transfer-Encoding in trailers — prohibited trailer field. |
| 8 | `SMUG-TRAILER-HOST` | Smuggling | [RFC 9110 §6.5.2](https://www.rfc-editor.org/rfc/rfc9110#section-6.5.2) | Host in trailers — must not be used for routing. |
| 9 | `SMUG-TRAILER-AUTH` | Smuggling | [RFC 9110 §6.5.1](https://www.rfc-editor.org/rfc/rfc9110#section-6.5.1) | Authorization in trailers — prohibited trailer field. |
| 10 | `SMUG-TRAILER-CONTENT-TYPE` | Smuggling | [RFC 9110 §6.5.1](https://www.rfc-editor.org/rfc/rfc9110#section-6.5.1) | Content-Type in trailers — prohibited trailer field. |
| 11 | `SMUG-HEAD-CL-BODY` | Smuggling | [RFC 9110 §9.3.2](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.2) | HEAD with body — server must not leave body on connection. |
| 12 | `SMUG-OPTIONS-CL-BODY` | Smuggling | [RFC 9110 §9.3.7](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.7) | OPTIONS with body — server should consume or reject body. |
| 13 | `SMUG-TE-TAB-BEFORE-VALUE` | Smuggling | [RFC 9110 §5.5](https://www.rfc-editor.org/rfc/rfc9110#section-5.5) | Tab as OWS before TE value — valid per `OWS = *( SP / HTAB )`. |
| 14 | `SMUG-ABSOLUTE-URI-HOST-MISMATCH` | Smuggling | [RFC 9112 §3.2.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2.2) | Absolute-form URI host differs from Host header — routing confusion vector. |
| 15 | `COMP-ABSOLUTE-FORM` | Compliance | [RFC 9112 §3.2.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2.2) | Absolute-form request-target — server **MUST** accept per RFC but many reject. |
| 16 | `COMP-METHOD-CASE` | Compliance | [RFC 9110 §9.1](https://www.rfc-editor.org/rfc/rfc9110#section-9.1) | Methods are case-sensitive. Lowercase "get" is an unknown method. Server **SHOULD** respond 501. |
| 17 | `MAL-RANGE-OVERLAPPING` | Malformed | [RFC 9110 §14.2](https://www.rfc-editor.org/rfc/rfc9110#section-14.2) | "A server that supports range requests **MAY** ignore or reject a Range header field that contains... a ranges-specifier with more than two overlapping ranges." |
| 18 | `MAL-URL-BACKSLASH` | Malformed | N/A | Backslash is not a valid URI character. Some servers normalize to `/`. |
| 19 | `NORM-CASE-TE` | Normalization | N/A | All-uppercase TRANSFER-ENCODING — tests header name case normalization. |
| 20 | `COMP-TRACE-SENSITIVE` | Compliance | [RFC 9110 §9.3.8](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.8) | "A server **SHOULD** exclude any request header fields that are likely to contain sensitive data." TRACE with Authorization header — checks if secret is echoed. |
| 21 | `COMP-ACCEPT-NONSENSE` | Compliance | [RFC 9110 §12.5.1](https://www.rfc-editor.org/rfc/rfc9110#section-12.5.1) | Unrecognized Accept value — server may return 406 or serve default representation. Both behaviors valid. |
| 22 | `COMP-DATE-FORMAT` | Compliance | [RFC 9110 §5.6.7](https://www.rfc-editor.org/rfc/rfc9110#section-5.6.7) | "A sender **MUST** generate timestamps in the IMF-fixdate format." Checks Date header format. |
| 23 | `COMP-RANGE-INVALID` | Compliance | [RFC 9110 §14.2](https://www.rfc-editor.org/rfc/rfc9110#section-14.2) | "A server **MAY** ignore the Range header field." Invalid Range syntax — 2xx or 416 both acceptable. |
| 24 | `COMP-POST-UNSUPPORTED-CT` | Compliance | [RFC 9110 §15.5.16](https://www.rfc-editor.org/rfc/rfc9110#section-15.5.16) | POST with unknown Content-Type — 415 or 2xx both acceptable. |
| 25 | `SMUG-PIPELINE-SAFE` | Smuggling | [RFC 9112 §9.3](https://www.rfc-editor.org/rfc/rfc9112#section-9.3) | Baseline: two clean pipelined GETs. Validates sequence test infrastructure against the target. |
| 26 | `SMUG-CL0-BODY-POISON` | Smuggling | [RFC 9112 §6.2](https://www.rfc-editor.org/rfc/rfc9112#section-6.2) | `Content-Length: 0` plus trailing bytes, then follow-up GET on same socket. Sequence telemetry for `0.CL`-style poisoning behavior. |
| 27 | `SMUG-GET-CL-BODY-DESYNC` | Smuggling | [RFC 9110 §9.3.1](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.1) | "Content received in a GET request ... might lead some implementations to reject the request and close the connection because of its potential as a request smuggling attack." Adds follow-up desync check. |
| 28 | `SMUG-OPTIONS-CL-BODY-DESYNC` | Smuggling | [RFC 9110 §9.3.7](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.7) | OPTIONS with body plus follow-up GET to detect unread-body poisoning on persistent connections. |
| 29 | `SMUG-EXPECT-100-CL-DESYNC` | Smuggling | [RFC 9110 §10.1.1](https://www.rfc-editor.org/rfc/rfc9110#section-10.1.1) | Expect/continue flow with immediate body plus follow-up GET; highlights whether connection framing remains synchronized. |
| 30 | `SMUG-GET-CL-PREFIX-DESYNC` | Smuggling | [RFC 9110 §9.3.1](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.1) | GET with a body containing an incomplete request prefix (missing the blank line). The follow-up write completes it and then sends a normal GET. If multiple responses are observed on step 2, the prefix bytes were likely left unread and executed. |

---

## N/A — Best-Practice / Defensive Tests (11 tests)

These tests don't map to a single RFC 2119 keyword but enforce defensive best practices.

| # | Test ID | Suite | RFC | RFC Quote / Rationale |
|---|---------|-------|-----|----------------------|
| 1 | `COMP-BASELINE` | Compliance | N/A | Sanity check — valid GET must return 2xx. |
| 2 | `MAL-MANY-HEADERS` | Malformed | [RFC 6585 §5](https://www.rfc-editor.org/rfc/rfc6585#section-5) | "The 431 status code indicates that the server is unwilling to process the request because its header fields are too large." Server should enforce header count limits. |
| 3 | `MAL-INCOMPLETE-REQUEST` | Malformed | [RFC 9112 §2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) | Grammar not matched — server **SHOULD** respond 400 and close. Timeout also acceptable. |
| 4 | `MAL-EMPTY-REQUEST` | Malformed | [RFC 9112 §2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) | Zero bytes — no grammar match possible. 400, close, or timeout acceptable. |
| 5 | `MAL-WHITESPACE-ONLY-LINE` | Malformed | [RFC 9112 §2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) | Whitespace-only request line — not an empty line (CRLF) and not a valid request-line. |
| 6 | `MAL-H2-PREFACE` | Malformed | [RFC 9113 §3.4](https://www.rfc-editor.org/rfc/rfc9113#section-3.4) | HTTP/2 preface on HTTP/1.1 port — protocol confusion. 400/505/close/timeout acceptable. |
| 7 | `MAL-URL-PERCENT-NULL` | Malformed | [RFC 9110 §5.5](https://www.rfc-editor.org/rfc/rfc9110#section-5.5) | Percent-encoded NUL (%00) — null byte injection risk. 400 = pass, 2xx/404 = warn. |
| 8 | `MAL-URL-PERCENT-CRLF` | Malformed | [RFC 9110 §5.5](https://www.rfc-editor.org/rfc/rfc9110#section-5.5) | Percent-encoded CRLF (%0d%0a) — header injection if decoded during parsing. 400 = pass, 2xx/404 = warn. |
| 9 | `MAL-CHUNK-EXT-64K` | Malformed | [RFC 9112 §7.1.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1.1) | "A server **ought to** limit the total length of chunk extensions." (CVE-2023-39326 class.) |
| 10 | `NORM-UNDERSCORE-CL` | Normalization | N/A | `Content_Length` — valid token but dangerous if normalized to `Content-Length`. Drop or reject = pass. |
| 11 | `NORM-UNDERSCORE-TE` | Normalization | N/A | `Transfer_Encoding` — same class of normalization attack. Drop or reject = pass. |

---

## Requirement Level by Suite

### Compliance Suite (76 tests)

| Level | Tests |
|-------|-------|
| MUST | 47 |
| SHOULD | 15 |
| MAY | 6 |
| Unscored | 7 |
| N/A | 1 |

### Smuggling Suite (87 tests)

| Level | Tests |
|-------|-------|
| MUST | 54 |
| SHOULD | 9 |
| MAY | 3 |
| "ought to" | 1 |
| Unscored | 20 |

### Malformed Input Suite (26 tests)

| Level | Tests |
|-------|-------|
| MUST | 10 |
| SHOULD | 5 |
| MAY | 1 |
| Unscored | 2 |
| N/A | 8 |

### Normalization Suite (5 tests)

| Level | Tests |
|-------|-------|
| MUST | 2 |
| Unscored | 1 |
| N/A | 2 |

---

## RFC Section Cross-Reference

| RFC Section | Tests | Topic |
|-------------|-------|-------|
| RFC 9112 §2.2 | 14 | Line endings, bare CR/LF, message parsing |
| RFC 9112 §2.3 | 6 | HTTP version |
| RFC 9112 §3 | 9 | Request line, method, request-target |
| RFC 9112 §3.2 | 11 | Host header, request-target forms |
| RFC 9112 §5 | 7 | Header field syntax, sp-before-colon |
| RFC 9112 §5.2 | 4 | Obsolete line folding |
| RFC 9112 §6.1 | 29 | Transfer-Encoding, CL+TE ambiguity |
| RFC 9112 §6.2 | 5 | Content-Length body framing |
| RFC 9112 §6.3 | 5 | Message body length determination |
| RFC 9112 §7.1 | 18 | Chunked transfer coding format |
| RFC 9112 §7.1.1 | 5 | Chunk extensions |
| RFC 9112 §7.1.2 | 1 | Chunked trailer section |
| RFC 9112 §9.3-9.6 | 3 | Connection management |
| RFC 9110 §5.3 | 1 | Header field duplication |
| RFC 9110 §5.4-5.6 | 8 | Field limits, values, lists, tokens |
| RFC 9110 §6.6.1 | 1 | Date header |
| RFC 9110 §7.2 | 1 | Host header semantics |
| RFC 9110 §7.8 | 5 | Upgrade |
| RFC 9110 §8.3 | 1 | Content-Type |
| RFC 9110 §8.6 | 15 | Content-Length semantics |
| RFC 9110 §9.1-9.3 | 13 | Methods (GET, HEAD, CONNECT, OPTIONS, TRACE) |
| RFC 9110 §10.1.1 | 3 | Expect header |
| RFC 9110 §6.5 | 5 | Trailer field restrictions |
| RFC 9110 §12.5.1 | 1 | Content negotiation (Accept) |
| RFC 9110 §14.2 | 3 | Range requests |
| RFC 9110 §15.2 | 1 | 1xx status codes |
| RFC 9110 §15.5.6 | 1 | 405 Method Not Allowed |
| RFC 9110 §15.5.16 | 1 | 415 Unsupported Media Type |
| RFC 6455 | 2 | WebSocket handshake |
| RFC 6585 | 3 | 431 status code |
| RFC 3629 | 1 | UTF-8 encoding |
| RFC 9113 | 1 | HTTP/2 preface |
| N/A | 7 | Best practice / defensive |
