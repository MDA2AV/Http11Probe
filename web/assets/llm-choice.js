/* Synthetic "LLM's Choice" column.
   One opinionated, doctrine-driven pick per test, derived from the whole
   fleet's results. Doctrine: reject ambiguity, accept the well-formed, be
   strict only where a byte can move a message boundary or reroute a request.
   Appends a server to window.PROBE_DATA *before* grid.js reads it, so it needs
   no change to the generated data.js and stays in sync with every re-probe. */
(function () {
  const DATA = window.PROBE_DATA;
  if (!DATA || !DATA.servers || !DATA.servers.length) return;
  if (DATA.servers.some(s => s.name === "LLM's Choice")) return; // idempotent
  const base = DATA.servers[0].results;

  // fleet split per test id: accept = 2xx, reject = 4xx/5xx or connection close
  const fleet = {};
  DATA.servers.forEach(s => s.results.forEach(r => {
    const f = fleet[r.id] || (fleet[r.id] = { acc: 0, rej: 0, other: 0, n: 0 });
    f.n++;
    const sc = r.statusCode;
    if (sc >= 200 && sc < 300) f.acc++;
    else if (sc >= 400 || r.connectionState === "ClosedByServer") f.rej++;
    else f.other++;
  }));

  const R = "reject", A = "accept";
  // Hand-authored calls on the tests that actually carry the argument.
  // [action, rationale, forcedStatus?]
  const OV = {
    "COMP-DUPLICATE-HOST-SAME": [R, "Two Host headers, even identical, is ambiguous routing input. Reject; a parser that silently picks one teaches the next hop to pick the other."],
    "RFC9110-5.4-DUPLICATE-HOST": [R, "Conflicting Host headers are a routing/smuggling primitive. There is no safe way to choose between them, so choose neither."],
    "COMP-HOST-WITH-PATH": [R, "A Host header carrying a path is malformed authority. Reject rather than normalise it away."],
    "COMP-HOST-WITH-USERINFO": [R, "user@host in a Host header is not a valid routing authority. Reject."],
    "SMUG-MULTIPLE-HOST-COMMA": [R, "Comma-joined Host values are two hosts wearing one header. Reject."],
    "RFC9112-7.1-MISSING-HOST": [R, "HTTP/1.1 requires exactly one Host. Missing Host is a 400, full stop."],
    "RFC9112-5.1-OBS-FOLD": [R, "Obs-fold is deprecated and a header-injection vector. Reject; never unfold-and-continue."],
    "RFC9112-2.2-BARE-LF-REQUEST-LINE": [R, "A bare LF as a line terminator is exactly how framing desyncs start. Require CRLF; reject."],
    "SMUG-CL-TE-BOTH": [R, "Content-Length and Transfer-Encoding together is THE smuggling primitive. The RFC allows 'TE wins', but that only holds if every hop agrees, and the fleet shows they don't. Reject and close."],
    "SMUG-DUPLICATE-CL": [R, "Two Content-Length values is an unresolvable message boundary. Reject."],
    "SMUG-TE-EMPTY-VALUE": [R, "An empty Transfer-Encoding is malformed framing. Reject rather than guess a coding."],
    "SMUG-TE-DOUBLE-CHUNKED": [R, "Transfer-Encoding: chunked, chunked is obfuscation aimed at a laxer downstream parser. Reject."],
    "SMUG-CHUNK-EXT-CTRL": [R, "A NUL/control byte in a chunk extension is not something you forward. Reject the framing."],
    "MAL-NON-ASCII-URL": [R, "Raw non-ASCII bytes in the target are not a valid URI. Reject; don't transcode into a guess."],
    "MAL-URL-OVERLONG-UTF8": [R, "Overlong UTF-8 for '/' is a canonicalisation attack. Reject before any path logic sees it."],
    "COMP-ASTERISK-WITH-GET": [R, "Asterisk-form is only valid for OPTIONS. With GET it is malformed; reject."],
    "COMP-METHOD-CONNECT": [R, "An origin server is not a tunnel. CONNECT gets 405, not a body.", 405],
    "COMP-BASELINE": [A, "A plain, well-formed request. Accepting is the whole point: strictness is about ambiguity, not hostility."],
    "COMP-POST-CL-BODY": [A, "Well-formed POST with a matching Content-Length. Accept and echo; the framing is unambiguous."],
    "COMP-CONNECTION-CLOSE": [A, "Well-formed request asking to close. Honour it: 200 then close the connection."],
  };

  function pickReject(exp) {
    exp = exp || "";
    if (/\b400\b/.test(exp)) return { statusCode: 400 };
    for (const c of [405, 501, 431, 413, 414, 417, 505])
      if (new RegExp("\\b" + c + "\\b").test(exp)) return { statusCode: c };
    if (/close/i.test(exp)) return { connectionState: "ClosedByServer" };
    return { statusCode: 400 };
  }

  function classify(r) {
    if (OV[r.id]) return { action: OV[r.id][0], why: OV[r.id][1], code: OV[r.id][2] };
    const exp = r.expected || "", cat = r.category, desc = (r.description || "").toLowerCase();
    const canReject = /\b(400|401|403|405|413|414|417|431|501|505)\b|close/i.test(exp);
    const canAccept = /2xx|\b2\d\d\b|echo/i.test(exp);
    const looksBad = cat === "Smuggling" || cat === "MalformedInput" || /reject|invalid|malformed|must not/i.test(desc);
    let action;
    if (looksBad && canReject) action = R;
    else if (canAccept) action = A;
    else action = canReject ? R : A;
    const why = action === R
      ? "Ambiguous or malformed input on a framing/routing-relevant field. Doctrine: reject, don't resolve."
      : "Well-formed and unambiguous; nothing here can move a message boundary or reroute the request, so accepting is safe and correct.";
    return { action, why };
  }

  const results = base.map(r => {
    const c = classify(r);
    const st = c.action === R
      ? (c.code ? { statusCode: c.code } : pickReject(r.expected))
      : { statusCode: 200 };
    const f = fleet[r.id] || { acc: 0, rej: 0, other: 0, n: 0 };
    const head = c.action === R
      ? (st.statusCode
          ? `HTTP/1.1 ${st.statusCode} Bad Request\r\nConnection: close\r\nContent-Length: 0`
          : `HTTP/1.1 400 Bad Request\r\nConnection: close`)
      : `HTTP/1.1 200 OK\r\nContent-Length: 2\r\n\r\nOK`;
    const resp = head +
      `\n\n── LLM's Choice · ${c.action.toUpperCase()} ──\n${c.why}\n\n` +
      `Fleet on this test: ${f.acc} accept · ${f.rej} reject` + (f.other ? ` · ${f.other} other` : "") +
      ` (of ${f.n}). Given the overall results, the safe pick is the one that can't desync against the rest of the fleet.`;
    return {
      id: r.id, category: r.category, rfcLevel: r.rfcLevel, description: r.description,
      expected: r.expected, rfc: r.rfc, scored: r.scored !== false,
      verdict: "Pass",
      statusCode: st.statusCode || null,
      connectionState: st.connectionState || "Open",
      got: st.statusCode ? String(st.statusCode) : "close",
      rawRequest: r.rawRequest || "(request payload identical to the fleet run)",
      rawResponse: resp,
      reason: c.why,
    };
  });

  DATA.servers.push({ name: "LLM's Choice", language: "doctrine", results });
})();
