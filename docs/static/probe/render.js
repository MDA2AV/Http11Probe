// Shared probe rendering utilities
window.ProbeRender = (function () {
  var PASS_BG = '#1a7f37';
  var WARN_BG = '#9a6700';
  var FAIL_BG = '#cf222e';
  var SKIP_BG = '#656d76';
  var EXPECT_BG = '#444c56';
  var pillCss = 'text-align:center;padding:2px 4px;font-size:11px;font-weight:600;color:#fff;border-radius:3px;min-width:28px;display:inline-block;line-height:18px;';

  // ── Scrollbar styling (injected once) ──────────────────────────
  var scrollStyleInjected = false;
  function injectScrollStyle() {
    if (scrollStyleInjected) return;
    scrollStyleInjected = true;
    var isDark = document.documentElement.classList.contains('dark');
    var trackBg = isDark ? '#2a2f38' : '#e5e7eb';
    var thumbBg = isDark ? '#4b5563' : '#94a3b8';
    var thumbHover = isDark ? '#6b7280' : '#64748b';
    var css = '.probe-scroll{overflow-x:auto}'
      + '.probe-scroll::-webkit-scrollbar{height:8px}'
      + '.probe-scroll::-webkit-scrollbar-track{background:' + trackBg + ';border-radius:4px}'
      + '.probe-scroll::-webkit-scrollbar-thumb{background:' + thumbBg + ';border-radius:4px}'
      + '.probe-scroll::-webkit-scrollbar-thumb:hover{background:' + thumbHover + '}'
      + '.probe-scroll{scrollbar-width:thin;scrollbar-color:' + thumbBg + ' ' + trackBg + '}';
    var style = document.createElement('style');
    style.textContent = css;
    document.head.appendChild(style);
  }

  // ── Test ID → doc page URL mapping ─────────────────────────────
  var TEST_URLS = {
    'COMP-ABSOLUTE-FORM': '/Http11Probe/docs/request-line/absolute-form/',
    'COMP-ASTERISK-WITH-GET': '/Http11Probe/docs/request-line/asterisk-with-get/',
    'COMP-BASELINE': '',
    'COMP-CONNECT-EMPTY-PORT': '/Http11Probe/docs/request-line/connect-empty-port/',
    'COMP-DUPLICATE-HOST-SAME': '/Http11Probe/docs/host-header/duplicate-host-same/',
    'COMP-HOST-WITH-PATH': '/Http11Probe/docs/host-header/host-with-path/',
    'COMP-HOST-WITH-USERINFO': '/Http11Probe/docs/host-header/host-with-userinfo/',
    'COMP-LEADING-CRLF': '/Http11Probe/docs/line-endings/leading-crlf/',
    'COMP-METHOD-CASE': '/Http11Probe/docs/request-line/method-case/',
    'COMP-OPTIONS-STAR': '/Http11Probe/docs/request-line/options-star/',
    'COMP-UNKNOWN-TE-501': '/Http11Probe/docs/request-line/unknown-te-501/',
    'COMP-WHITESPACE-BEFORE-HEADERS': '/Http11Probe/docs/headers/whitespace-before-headers/',
    'MAL-BINARY-GARBAGE': '/Http11Probe/docs/malformed-input/binary-garbage/',
    'MAL-CHUNK-EXTENSION-LONG': '/Http11Probe/docs/malformed-input/chunk-extension-long/',
    'MAL-CHUNK-SIZE-OVERFLOW': '/Http11Probe/docs/malformed-input/chunk-size-overflow/',
    'MAL-CL-OVERFLOW': '/Http11Probe/docs/malformed-input/cl-overflow/',
    'MAL-CONTROL-CHARS-HEADER': '/Http11Probe/docs/malformed-input/control-chars-header/',
    'MAL-EMPTY-REQUEST': '/Http11Probe/docs/malformed-input/empty-request/',
    'MAL-H2-PREFACE': '/Http11Probe/docs/malformed-input/h2-preface/',
    'MAL-INCOMPLETE-REQUEST': '/Http11Probe/docs/malformed-input/incomplete-request/',
    'MAL-LONG-HEADER-NAME': '/Http11Probe/docs/malformed-input/long-header-name/',
    'MAL-LONG-HEADER-VALUE': '/Http11Probe/docs/malformed-input/long-header-value/',
    'MAL-LONG-METHOD': '/Http11Probe/docs/malformed-input/long-method/',
    'MAL-LONG-URL': '/Http11Probe/docs/malformed-input/long-url/',
    'MAL-MANY-HEADERS': '/Http11Probe/docs/malformed-input/many-headers/',
    'MAL-NON-ASCII-HEADER-NAME': '/Http11Probe/docs/malformed-input/non-ascii-header-name/',
    'MAL-NON-ASCII-URL': '/Http11Probe/docs/malformed-input/non-ascii-url/',
    'MAL-NUL-IN-HEADER-VALUE': '/Http11Probe/docs/malformed-input/nul-in-header-value/',
    'MAL-NUL-IN-URL': '/Http11Probe/docs/malformed-input/nul-in-url/',
    'MAL-WHITESPACE-ONLY-LINE': '/Http11Probe/docs/malformed-input/whitespace-only-line/',
    'RFC9110-5.4-DUPLICATE-HOST': '/Http11Probe/docs/host-header/duplicate-host/',
    'RFC9110-5.6.2-SP-BEFORE-COLON': '/Http11Probe/docs/headers/sp-before-colon/',
    'RFC9110-8.6-DUPLICATE-CL': '/Http11Probe/docs/smuggling/duplicate-cl/',
    'RFC9112-2.2-BARE-LF-HEADER': '/Http11Probe/docs/line-endings/bare-lf-header/',
    'RFC9112-2.2-BARE-LF-REQUEST-LINE': '/Http11Probe/docs/line-endings/bare-lf-request-line/',
    'RFC9112-2.3-HTTP09-REQUEST': '/Http11Probe/docs/request-line/http09-request/',
    'RFC9112-2.3-INVALID-VERSION': '/Http11Probe/docs/request-line/invalid-version/',
    'RFC9112-3-CR-ONLY-LINE-ENDING': '/Http11Probe/docs/line-endings/cr-only-line-ending/',
    'RFC9112-3-MISSING-TARGET': '/Http11Probe/docs/request-line/missing-target/',
    'RFC9112-3-MULTI-SP-REQUEST-LINE': '/Http11Probe/docs/request-line/multi-sp-request-line/',
    'RFC9112-3.2-FRAGMENT-IN-TARGET': '/Http11Probe/docs/request-line/fragment-in-target/',
    'RFC9112-5-EMPTY-HEADER-NAME': '/Http11Probe/docs/headers/empty-header-name/',
    'RFC9112-5-HEADER-NO-COLON': '/Http11Probe/docs/headers/header-no-colon/',
    'RFC9112-5-INVALID-HEADER-NAME': '/Http11Probe/docs/headers/invalid-header-name/',
    'RFC9112-5.1-OBS-FOLD': '/Http11Probe/docs/headers/obs-fold/',
    'RFC9112-6.1-CL-LEADING-ZEROS': '/Http11Probe/docs/smuggling/cl-leading-zeros/',
    'RFC9112-6.1-CL-NEGATIVE': '/Http11Probe/docs/smuggling/cl-negative/',
    'RFC9112-6.1-CL-NON-NUMERIC': '/Http11Probe/docs/content-length/cl-non-numeric/',
    'RFC9112-6.1-CL-PLUS-SIGN': '/Http11Probe/docs/content-length/cl-plus-sign/',
    'RFC9112-6.1-CL-TE-BOTH': '/Http11Probe/docs/smuggling/cl-te-both/',
    'RFC9112-7.1-MISSING-HOST': '/Http11Probe/docs/host-header/missing-host/',
    'SMUG-BARE-CR-HEADER-VALUE': '/Http11Probe/docs/smuggling/bare-cr-header-value/',
    'SMUG-CHUNK-BARE-SEMICOLON': '/Http11Probe/docs/smuggling/chunk-bare-semicolon/',
    'SMUG-CHUNK-HEX-PREFIX': '/Http11Probe/docs/smuggling/chunk-hex-prefix/',
    'SMUG-CHUNK-LEADING-SP': '/Http11Probe/docs/smuggling/chunk-leading-sp/',
    'SMUG-CHUNK-MISSING-TRAILING-CRLF': '/Http11Probe/docs/smuggling/chunk-missing-trailing-crlf/',
    'SMUG-CHUNK-UNDERSCORE': '/Http11Probe/docs/smuggling/chunk-underscore/',
    'SMUG-CHUNKED-WITH-PARAMS': '/Http11Probe/docs/smuggling/chunked-with-params/',
    'SMUG-CL-COMMA-DIFFERENT': '/Http11Probe/docs/smuggling/cl-comma-different/',
    'SMUG-CL-COMMA-SAME': '/Http11Probe/docs/smuggling/cl-comma-same/',
    'SMUG-CL-EXTRA-LEADING-SP': '/Http11Probe/docs/smuggling/cl-extra-leading-sp/',
    'SMUG-CL-HEX-PREFIX': '/Http11Probe/docs/smuggling/cl-hex-prefix/',
    'SMUG-CL-INTERNAL-SPACE': '/Http11Probe/docs/smuggling/cl-internal-space/',
    'SMUG-CL-OCTAL': '/Http11Probe/docs/smuggling/cl-octal/',
    'SMUG-CL-TRAILING-SPACE': '/Http11Probe/docs/smuggling/cl-trailing-space/',
    'SMUG-CLTE-PIPELINE': '/Http11Probe/docs/smuggling/clte-pipeline/',
    'SMUG-EXPECT-100-CL': '/Http11Probe/docs/smuggling/expect-100-cl/',
    'SMUG-HEADER-INJECTION': '/Http11Probe/docs/smuggling/header-injection/',
    'SMUG-TE-CASE-MISMATCH': '/Http11Probe/docs/smuggling/te-case-mismatch/',
    'SMUG-TE-DOUBLE-CHUNKED': '/Http11Probe/docs/smuggling/te-double-chunked/',
    'SMUG-TE-DUPLICATE-HEADERS': '/Http11Probe/docs/smuggling/te-duplicate-headers/',
    'SMUG-TE-EMPTY-VALUE': '/Http11Probe/docs/smuggling/te-empty-value/',
    'SMUG-TE-HTTP10': '/Http11Probe/docs/smuggling/te-http10/',
    'SMUG-TE-LEADING-COMMA': '/Http11Probe/docs/smuggling/te-leading-comma/',
    'SMUG-TE-NOT-FINAL-CHUNKED': '/Http11Probe/docs/smuggling/te-not-final-chunked/',
    'SMUG-TE-SP-BEFORE-COLON': '/Http11Probe/docs/smuggling/te-sp-before-colon/',
    'SMUG-TE-TRAILING-SPACE': '/Http11Probe/docs/smuggling/te-trailing-space/',
    'SMUG-TE-XCHUNKED': '/Http11Probe/docs/smuggling/te-xchunked/',
    'SMUG-TECL-PIPELINE': '/Http11Probe/docs/smuggling/tecl-pipeline/',
    'SMUG-TRANSFER_ENCODING': '/Http11Probe/docs/smuggling/transfer-encoding-underscore/'
  };

  function testUrl(tid) {
    return TEST_URLS[tid] || '';
  }

  function pill(bg, label) {
    return '<span style="' + pillCss + 'background:' + bg + ';">' + label + '</span>';
  }

  function verdictBg(v) {
    return v === 'Pass' ? PASS_BG : v === 'Warn' ? WARN_BG : FAIL_BG;
  }

  function buildLookups(servers) {
    var names = servers.map(function (sv) { return sv.name; }).sort();
    var lookup = {};
    servers.forEach(function (sv) {
      var m = {};
      sv.results.forEach(function (r) { m[r.id] = r; });
      lookup[sv.name] = m;
    });
    var testIds = servers[0].results.map(function (r) { return r.id; });
    return { names: names, lookup: lookup, testIds: testIds };
  }

  function renderSummary(targetId, data) {
    var el = document.getElementById(targetId);
    if (!el) return;
    var servers = data.servers;
    if (!servers || servers.length === 0) {
      el.innerHTML = '<p><em>No server results found.</em></p>';
      return;
    }
    var sorted = servers.slice().sort(function (a, b) {
      var sa = a.summary, sb = b.summary;
      var pa = sa.passed / (sa.scored || sa.total || 1);
      var pb = sb.passed / (sb.scored || sb.total || 1);
      return pb - pa || a.name.localeCompare(b.name);
    });
    var maxScored = sorted[0] ? (sorted[0].summary.scored || sorted[0].summary.total) : 1;
    var topPassed = sorted[0] ? sorted[0].summary.passed : 1;

    var html = '<div style="display:flex;flex-direction:column;gap:6px;max-width:700px;">';
    sorted.forEach(function (sv, i) {
      var s = sv.summary;
      var scored = s.scored || s.total;
      var pct = s.passed / scored;
      var barPct = (s.passed / topPassed) * 100;
      var displayPct = Math.round(pct * 100);
      var bg = pct >= 0.85 ? '#1a7f37' : pct >= 0.6 ? '#9a6700' : pct >= 0.2 ? '#cf222e' : '#82071e';
      var rank = i + 1;

      html += '<div style="display:flex;align-items:center;gap:10px;">';
      html += '<div style="min-width:24px;text-align:right;font-size:13px;font-weight:600;color:#656d76;">' + rank + '</div>';
      html += '<div style="min-width:110px;font-size:13px;font-weight:600;white-space:nowrap;">' + sv.name + '</div>';
      var trackBg = document.documentElement.classList.contains('dark') ? '#2a2f38' : '#f0f0f0';
      html += '<div style="flex:1;height:22px;background:' + trackBg + ';border-radius:3px;overflow:hidden;position:relative;">';
      html += '<div style="height:100%;width:' + barPct + '%;background:' + bg + ';border-radius:3px;transition:width 0.3s;"></div>';
      html += '</div>';
      html += '<div style="min-width:72px;text-align:right;font-size:13px;font-weight:700;">' + s.passed + '/' + scored + '</div>';
      html += '<div style="min-width:40px;text-align:right;font-size:12px;color:#656d76;">' + displayPct + '%</div>';
      html += '</div>';
    });
    html += '</div>';
    if (data.commit) {
      html += '<p style="margin-top:12px;font-size:0.85em;color:#656d76;">Commit: <code>' + data.commit.id.substring(0, 7) + '</code> &mdash; ' + (data.commit.message || '') + '</p>';
    }
    el.innerHTML = html;
  }

  function renderTable(targetId, categoryKey, ctx) {
    injectScrollStyle();
    var el = document.getElementById(targetId);
    if (!el) return;
    var names = ctx.names, lookup = ctx.lookup, testIds = ctx.testIds;

    var catTests = testIds.filter(function (tid) {
      return lookup[names[0]][tid] && lookup[names[0]][tid].category === categoryKey;
    });
    if (catTests.length === 0) {
      el.innerHTML = '<p><em>No tests in this category.</em></p>';
      return;
    }

    var scoredTests = catTests.filter(function (tid) { return lookup[names[0]][tid].scored !== false; });
    var unscoredTests = catTests.filter(function (tid) { return lookup[names[0]][tid].scored === false; });
    var orderedTests = scoredTests.concat(unscoredTests);

    var shortLabels = orderedTests.map(function (tid) {
      return tid.replace(/^(RFC\d+-[\d.]+-|COMP-|SMUG-|MAL-)/, '');
    });

    var t = '<div class="probe-scroll"><table style="border-collapse:collapse;font-size:12px;white-space:nowrap;">';

    // Column header row (diagonal labels)
    t += '<thead><tr>';
    t += '<th style="padding:4px 8px;text-align:left;vertical-align:bottom;min-width:100px;"></th>';
    orderedTests.forEach(function (tid, i) {
      var first = lookup[names[0]][tid];
      var isUnscored = first.scored === false;
      var opacity = isUnscored ? 'opacity:0.55;' : '';
      var url = testUrl(tid);
      t += '<th style="padding:0;height:110px;width:30px;vertical-align:bottom;' + opacity + '">';
      t += '<div style="width:30px;height:110px;position:relative;">';
      if (url) {
        t += '<a href="' + url + '" style="font-size:10px;font-weight:500;color:inherit;text-decoration:none;position:absolute;bottom:6px;left:50%;transform-origin:bottom left;transform:rotate(-55deg);white-space:nowrap;" title="' + first.description + '">' + shortLabels[i];
      } else {
        t += '<span style="font-size:10px;font-weight:500;color:inherit;position:absolute;bottom:6px;left:50%;transform-origin:bottom left;transform:rotate(-55deg);white-space:nowrap;" title="' + first.description + '">' + shortLabels[i];
      }
      if (isUnscored) t += '*';
      t += url ? '</a>' : '</span>';
      t += '</div></th>';
    });
    t += '</tr></thead><tbody>';

    // Expected row
    t += '<tr style="background:#f6f8fa;">';
    t += '<td style="padding:4px 8px;font-weight:700;font-size:11px;color:#656d76;">Expected</td>';
    orderedTests.forEach(function (tid) {
      var first = lookup[names[0]][tid];
      var isUnscored = first.scored === false;
      var opacity = isUnscored ? 'opacity:0.55;' : '';
      t += '<td style="text-align:center;padding:2px 3px;' + opacity + '">' + pill(EXPECT_BG, first.expected.replace(/ or close/g, '/\u2715').replace(/\//g, '/\u200B')) + '</td>';
    });
    t += '</tr>';

    // Server rows
    names.forEach(function (n) {
      t += '<tr>';
      t += '<td style="padding:4px 8px;font-weight:600;font-size:12px;">' + n + '</td>';
      orderedTests.forEach(function (tid) {
        var r = lookup[n] && lookup[n][tid];
        var isUnscored = lookup[names[0]][tid].scored === false;
        var opacity = isUnscored ? 'opacity:0.55;' : '';
        if (!r) {
          t += '<td style="text-align:center;padding:2px 3px;' + opacity + '">' + pill(SKIP_BG, '\u2014') + '</td>';
          return;
        }
        t += '<td style="text-align:center;padding:2px 3px;' + opacity + '">' + pill(verdictBg(r.verdict), r.got) + '</td>';
      });
      t += '</tr>';
    });

    t += '</tbody></table></div>';
    if (unscoredTests.length > 0) {
      t += '<p style="font-size:0.8em;color:#656d76;margin-top:4px;">* Not scored &mdash; RFC-compliant behavior, shown for reference.</p>';
    }
    el.innerHTML = t;
  }

  // ── Language filter ────────────────────────────────────────────
  function renderLanguageFilter(targetId, data, onChange) {
    var el = document.getElementById(targetId);
    if (!el || !data.servers || data.servers.length === 0) return;

    var langs = {};
    data.servers.forEach(function (sv) {
      if (sv.language) langs[sv.language] = true;
    });
    var langList = Object.keys(langs).sort();
    if (langList.length === 0) return;

    var isDark = document.documentElement.classList.contains('dark');
    var baseBg = isDark ? '#21262d' : '#f6f8fa';
    var baseFg = isDark ? '#c9d1d9' : '#24292f';
    var baseBorder = isDark ? '#30363d' : '#d0d7de';
    var activeBg = isDark ? '#1f6feb' : '#0969da';

    var btnStyle = 'display:inline-block;padding:4px 12px;font-size:12px;font-weight:600;'
      + 'border-radius:20px;cursor:pointer;border:1px solid ' + baseBorder + ';'
      + 'margin-right:6px;margin-bottom:6px;transition:all 0.15s;';

    var html = '<div style="margin-bottom:12px;">';
    html += '<button class="probe-lang-btn" data-lang="" style="' + btnStyle
      + 'background:' + activeBg + ';color:#fff;border-color:' + activeBg + ';">All</button>';
    langList.forEach(function (lang) {
      html += '<button class="probe-lang-btn" data-lang="' + lang + '" style="' + btnStyle
        + 'background:' + baseBg + ';color:' + baseFg + ';">' + lang + '</button>';
    });
    html += '</div>';
    el.innerHTML = html;

    var buttons = el.querySelectorAll('.probe-lang-btn');
    buttons.forEach(function (btn) {
      btn.addEventListener('click', function () {
        var lang = btn.getAttribute('data-lang');
        buttons.forEach(function (b) {
          if (b === btn) {
            b.style.background = activeBg;
            b.style.color = '#fff';
            b.style.borderColor = activeBg;
          } else {
            b.style.background = baseBg;
            b.style.color = baseFg;
            b.style.borderColor = baseBorder;
          }
        });
        if (!lang) {
          onChange(data);
        } else {
          var filtered = {
            commit: data.commit,
            servers: data.servers.filter(function (sv) { return sv.language === lang; })
          };
          onChange(filtered);
        }
      });
    });
  }

  return {
    pill: pill,
    verdictBg: verdictBg,
    buildLookups: buildLookups,
    renderSummary: renderSummary,
    renderTable: renderTable,
    renderLanguageFilter: renderLanguageFilter,
    EXPECT_BG: EXPECT_BG
  };
})();
