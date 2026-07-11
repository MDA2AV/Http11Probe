/* Synthetic "LLM's Choice" column — ideal behaviour for a PROXY, read off the
   whole fleet's results.
   Doctrine: a proxy forwards to a backend, so anywhere the ecosystem disagrees
   on framing/routing, forwarding is a desync/smuggling bet. Therefore reject
   (400 + close) every ambiguous or malformed request, AND refuse legal-but-risky
   features a proxy can't guarantee a backend re-frames identically (chunk
   extensions, trailers, obs-fold, odd Content-Length spellings, header-name
   normalisation) — even when the suite would prefer 2xx. Forward only the clean,
   unambiguous requests. Accept the proxy request-forms an origin would reject
   (absolute-form). Bound the buffers (reject unbounded targets).
   Verdicts are NOT asserted — each cell inherits the verdict a real server was
   given for the same response, so the deliberate strict calls show as honest
   Warn/Fail. Appended before grid.js reads window.PROBE_DATA; self-updating. */
(function () {
  const DATA = window.PROBE_DATA;
  if (!DATA || !DATA.servers || !DATA.servers.length) return;
  if (DATA.servers.some(s => s.name === "LLM's Choice")) return;
  const base = DATA.servers[0].results;

  // fleet split + verdict-by-status, per test
  const fleet = {}, vByStatus = {};
  DATA.servers.forEach(s => s.results.forEach(r => {
    const f = fleet[r.id] || (fleet[r.id] = { acc: 0, rej: 0, other: 0, n: 0 });
    f.n++;
    const sc = r.statusCode;
    if (sc >= 200 && sc < 300) f.acc++;
    else if (sc >= 400 || r.connectionState === "ClosedByServer") f.rej++;
    else f.other++;
    const key = String(sc || r.connectionState);
    const vb = vByStatus[r.id] || (vByStatus[r.id] = {});
    vb[key] = vb[key] || {};
    vb[key][r.verdict] = (vb[key][r.verdict] || 0) + 1;
  }));
  const fleetVerdict = (id, key) => {
    const vb = vByStatus[id] && vByStatus[id][key];
    return vb ? Object.keys(vb).sort((a, b) => vb[b] - vb[a])[0] : null;
  };

  const REJECT = "reject", FWD = "forward";
  // [action, rationale, forcedStatus?]
  const OV = {
    // ---- framing ambiguity: the smuggling surface a proxy must not forward ----
    "SMUG-CL-TE-BOTH": [REJECT, "Content-Length and Transfer-Encoding give two different message boundaries. A proxy that forwards this pairs with a backend that may pick the other one — that IS request smuggling. Reject and close."],
    "SMUG-DUPLICATE-CL": [REJECT, "Two Content-Lengths: proxy and backend can choose differently. Unresolvable — reject."],
    "SMUG-TE-EMPTY-VALUE": [REJECT, "Empty Transfer-Encoding is malformed framing a downstream may read as chunked. Reject."],
    "SMUG-TE-DOUBLE-CHUNKED": [REJECT, "`chunked, chunked` is obfuscation aimed at a laxer backend. A proxy refuses it."],
    "SMUG-CHUNK-EXT-CTRL": [REJECT, "Control byte in a chunk extension is not forwardable data. Reject the framing."],
    // ---- Host / routing: a proxy routes on Host, so ambiguity is a mis-route ----
    "COMP-DUPLICATE-HOST-SAME": [REJECT, "Two Host headers is ambiguous routing input; a proxy that picks one may route differently than the next hop. Reject."],
    "RFC9110-5.4-DUPLICATE-HOST": [REJECT, "Conflicting Host headers are a routing/smuggling primitive for a proxy. Reject."],
    "COMP-HOST-WITH-PATH": [REJECT, "Host carrying a path is malformed authority — a proxy can't route it safely. Reject."],
    "COMP-HOST-WITH-USERINFO": [REJECT, "userinfo@host is not a routable authority. Reject."],
    "SMUG-MULTIPLE-HOST-COMMA": [REJECT, "Comma-joined Host values are two hosts in one header. Reject."],
    "RFC9112-7.1-MISSING-HOST": [REJECT, "No Host means no route. A proxy rejects with 400."],
    // ---- line framing ----
    "RFC9112-5.1-OBS-FOLD": [REJECT, "Obs-fold must never be unfolded-and-forwarded — that rewrites headers for the backend. Reject."],
    // ---- odd Content-Length spellings: legal grammar, but the exact parser-disagreement vector a chain exploits ----
    "SMUG-CL-LEADING-ZEROS": [REJECT, "Leading-zero Content-Length is legal 1*DIGIT, but it's a parser-disagreement vector — the backend might read it differently. A proxy normalises to one canonical length or rejects; here, reject."],
    "SMUG-CL-LEADING-ZEROS-OCTAL": [REJECT, "`0200` reads as 128 (octal) or 200 (decimal) depending on the parser — precisely the proxy/backend split that smuggles. Reject."],
    "SMUG-CL-DOUBLE-ZERO": [REJECT, "`00` invites leading-zero ambiguity between hops. Reject."],
    "SMUG-CL-TRAILING-SPACE": [REJECT, "OWS around Content-Length is trimmed inconsistently across parsers. A proxy refuses rather than forward a length two hops may read differently."],
    "SMUG-CL-EXTRA-LEADING-SP": [REJECT, "Extra OWS before the length — same inconsistent-trimming desync risk. Reject."],
    // ---- legal-but-risky features a proxy can't guarantee the backend re-frames identically (DELIBERATE non-passes) ----
    "COMP-CHUNKED-EXTENSION": [REJECT, "Chunk extensions are legal but effectively unused and are dropped/re-framed differently across hops. A hardened proxy refuses them rather than forward desync surface — knowingly giving up the 2xx the suite prefers."],
    "COMP-CHUNKED-TRAILER-VALID": [REJECT, "Trailer fields are a known smuggling vector and are handled inconsistently downstream. A proxy strips or refuses them. Deliberate non-pass — the security trade."],
    "NORM-UNDERSCORE-CL": [REJECT, "`Content_Length` must NOT be normalised to `Content-Length` — that's how a hidden length is smuggled past a filter. A proxy rejects the underscore header, not silently rewrites it."],
    "NORM-UNDERSCORE-TE": [REJECT, "`Transfer_Encoding` underscore variant is a filter-bypass smuggling trick. Reject, don't normalise."],
    // ---- bounded buffers: refuse the unbounded even when it's valid (DELIBERATE non-pass) ----
    "COMP-LONG-URL-OK": [REJECT, "A proxy has fixed buffers and a small trusted routing surface; it caps the request-target and answers 414, even though this URL is technically valid. The robustness trade a black box never has to make.", 414],
    // ---- proxy-specific ACCEPT that an origin server rejects ----
    "COMP-ABSOLUTE-FORM": [FWD, "`GET http://host/path` is exactly the request-target a client sends TO a proxy. A proxy accepts absolute-form — the one place it's *more* lenient than an origin server."],
    // ---- clean baselines: forward ----
    "COMP-BASELINE": [FWD, "Clean, unambiguous request. Forward."],
    "COMP-POST-CL-BODY": [FWD, "Well-formed POST, matching Content-Length. Forward and echo."],
    "COMP-CONNECTION-CLOSE": [FWD, "Well-formed request asking to close. Forward, then close."],
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
    const cat = r.category, exp = (r.expected || ""), desc = (r.description || "").toLowerCase();
    if (cat === "Smuggling" || cat === "MalformedInput" || cat === "Normalization")
      return { action: REJECT, why: "Ambiguous or malformed framing/routing input. A proxy that forwards it is betting the backend frames it the same way — so it rejects rather than resolve." };
    const prefersReject = /^\s*(4\d\d|5\d\d|400|501|close)/.test(exp) || /must be rejected|must reject|not valid|invalid|malformed/.test(desc);
    if (prefersReject) return { action: REJECT, why: "Disallowed or malformed request; a strict proxy rejects it rather than pass it to a backend." };
    return { action: FWD, why: "Clean and unambiguous — a proxy forwards this." };
  }

  const results = base.map(r => {
    const c = classify(r);
    const st = c.action === REJECT
      ? (c.code ? { statusCode: c.code } : pickReject(r.expected))
      : { statusCode: 200 };
    const key = String(st.statusCode || st.connectionState);
    const verdict = fleetVerdict(r.id, key) ||
      (c.action === FWD ? (/2xx|200/.test(r.expected || "") ? "Pass" : "Warn")
       : (new RegExp("\\b" + (st.statusCode || "") + "\\b").test(r.expected || "") || /close/i.test(r.expected || "") ? "Pass"
          : (/2xx|echo/.test(r.expected || "") && !/4\d\d|close/.test(r.expected || "")) ? "Fail" : "Warn"));
    const f = fleet[r.id] || { acc: 0, rej: 0, other: 0, n: 0 };
    const head = c.action === REJECT
      ? (st.statusCode ? `HTTP/1.1 ${st.statusCode} Bad Request\r\nConnection: close\r\nContent-Length: 0`
                       : `HTTP/1.1 400 Bad Request\r\nConnection: close`)
      : `HTTP/1.1 200 OK\r\nContent-Length: 2\r\n\r\nOK`;
    const note = verdict === "Pass" ? ""
      : c.action === REJECT
        ? `\n(Verdict ${verdict}: the suite prefers acceptance — this is the deliberate proxy-strictness trade, not a bug. A smuggling-proof proxy gives up this feature on purpose.)`
        : `\n(Verdict ${verdict}: forwarded as-is; the response detail the suite wants here is the backend's to supply, not the proxy's.)`;
    const resp = head +
      `\n\n── LLM's Choice · ideal proxy · ${c.action.toUpperCase()} ──\n${c.why}\n\n` +
      `Fleet on this test: ${f.acc} accept · ${f.rej} reject` + (f.other ? ` · ${f.other} other` : "") +
      ` (of ${f.n}). Where the fleet is split, a proxy that forwards is the hop that desyncs.` + note;
    return {
      id: r.id, category: r.category, rfcLevel: r.rfcLevel, description: r.description,
      expected: r.expected, rfc: r.rfc, scored: r.scored !== false,
      verdict,
      statusCode: st.statusCode || null,
      connectionState: st.connectionState || "Open",
      got: st.statusCode ? String(st.statusCode) : "close",
      rawRequest: r.rawRequest || "(request payload identical to the fleet run)",
      rawResponse: resp,
      reason: c.why,
    };
  });

  DATA.servers.push({ name: "LLM's Choice", language: "ideal proxy", results });
})();
