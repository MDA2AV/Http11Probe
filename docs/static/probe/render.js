// Shared probe rendering utilities
window.ProbeRender = (function () {
  var PASS_BG = '#1a7f37';
  var WARN_BG = '#9a6700';
  var FAIL_BG = '#cf222e';
  var SKIP_BG = '#656d76';
  var EXPECT_BG = '#444c56';
  var pillCss = 'text-align:center;padding:2px 4px;font-size:11px;font-weight:600;color:#fff;border-radius:3px;min-width:28px;display:inline-block;line-height:18px;';

  // Servers temporarily hidden from results (undergoing major changes)
  var BLACKLISTED_SERVERS = ['GenHTTP'];
  function filterBlacklisted(servers) {
    return servers.filter(function (sv) {
      return BLACKLISTED_SERVERS.indexOf(sv.name) === -1;
    });
  }

  // ── Scrollbar styling (injected once) ──────────────────────────
  var scrollStyleInjected = false;
  function injectScrollStyle() {
    if (scrollStyleInjected) return;
    scrollStyleInjected = true;
    var css = ''
      // Scrollbar — light
      + '.probe-scroll{overflow-x:auto;scrollbar-width:thin;scrollbar-color:#94a3b8 #e5e7eb}'
      + '.probe-scroll::-webkit-scrollbar{height:8px}'
      + '.probe-scroll::-webkit-scrollbar-track{background:#e5e7eb;border-radius:4px}'
      + '.probe-scroll::-webkit-scrollbar-thumb{background:#94a3b8;border-radius:4px}'
      + '.probe-scroll::-webkit-scrollbar-thumb:hover{background:#64748b}'
      // Scrollbar — dark
      + 'html.dark .probe-scroll{scrollbar-color:#4b5563 #2a2f38}'
      + 'html.dark .probe-scroll::-webkit-scrollbar-track{background:#2a2f38}'
      + 'html.dark .probe-scroll::-webkit-scrollbar-thumb{background:#4b5563}'
      + 'html.dark .probe-scroll::-webkit-scrollbar-thumb:hover{background:#6b7280}'
      // Table rows — light
      + '.probe-table thead{border-bottom:2px solid #afb8c1}'
      + '.probe-table tbody tr{border-bottom:1px solid #afb8c1}'
      + '.probe-server-row{cursor:pointer;transition:background 0.15s}'
      + '.probe-server-row:hover{background:#eef1f5}'
      + '.probe-server-row.probe-row-active{background:#c8ddf0 !important}'
      + '.probe-table thead a{color:#0969da !important;text-decoration:underline !important;text-underline-offset:2px}'
      // Table rows — dark
      + 'html.dark .probe-table thead{border-bottom-color:#30363d}'
      + 'html.dark .probe-table tbody tr{border-bottom-color:#30363d}'
      + 'html.dark .probe-server-row:hover{background:#161b22}'
      + 'html.dark .probe-server-row.probe-row-active{background:#2a3a50 !important}'
      + 'html.dark .probe-table thead a{color:#58a6ff !important}';
    var style = document.createElement('style');
    style.textContent = css;
    document.head.appendChild(style);
  }

  // ── Test ID → doc page URL mapping ─────────────────────────────
  var TEST_URLS = {
    'COMP-ABSOLUTE-FORM': '/Http11Probe/docs/request-line/absolute-form/',
    'COMP-ASTERISK-WITH-GET': '/Http11Probe/docs/request-line/asterisk-with-get/',
    'COMP-BASELINE': '',
    'COMP-CHUNKED-BODY': '/Http11Probe/docs/body/chunked-body/',
    'COMP-CHUNKED-EMPTY': '/Http11Probe/docs/body/chunked-empty/',
    'COMP-CHUNKED-EXTENSION': '/Http11Probe/docs/body/chunked-extension/',
    'COMP-CHUNKED-MULTI': '/Http11Probe/docs/body/chunked-multi/',
    'COMP-CHUNKED-NO-FINAL': '/Http11Probe/docs/body/chunked-no-final/',
    'COMP-DUPLICATE-HOST-SAME': '/Http11Probe/docs/host-header/duplicate-host-same/',
    'COMP-EXPECT-UNKNOWN': '/Http11Probe/docs/headers/expect-unknown/',
    'COMP-GET-WITH-CL-BODY': '/Http11Probe/docs/body/get-with-cl-body/',
    'COMP-HOST-WITH-PATH': '/Http11Probe/docs/host-header/host-with-path/',
    'COMP-HOST-WITH-USERINFO': '/Http11Probe/docs/host-header/host-with-userinfo/',
    'COMP-LEADING-CRLF': '/Http11Probe/docs/line-endings/leading-crlf/',
    'COMP-METHOD-CASE': '/Http11Probe/docs/request-line/method-case/',
    'COMP-METHOD-CONNECT': '/Http11Probe/docs/request-line/method-connect/',
    'COMP-METHOD-TRACE': '/Http11Probe/docs/request-line/method-trace/',
    'COMP-OPTIONS-STAR': '/Http11Probe/docs/request-line/options-star/',
    'COMP-POST-CL-BODY': '/Http11Probe/docs/body/post-cl-body/',
    'COMP-POST-CL-UNDERSEND': '/Http11Probe/docs/body/post-cl-undersend/',
    'COMP-POST-CL-ZERO': '/Http11Probe/docs/body/post-cl-zero/',
    'COMP-POST-NO-CL-NO-TE': '/Http11Probe/docs/body/post-no-cl-no-te/',
    'COMP-UNKNOWN-TE-501': '/Http11Probe/docs/request-line/unknown-te-501/',
    'COMP-UPGRADE-INVALID-VER': '/Http11Probe/docs/upgrade/upgrade-invalid-ver/',
    'COMP-UPGRADE-MISSING-CONN': '/Http11Probe/docs/upgrade/upgrade-missing-conn/',
    'COMP-UPGRADE-POST': '/Http11Probe/docs/upgrade/upgrade-post/',
    'COMP-UPGRADE-UNKNOWN': '/Http11Probe/docs/upgrade/upgrade-unknown/',
    'COMP-WHITESPACE-BEFORE-HEADERS': '/Http11Probe/docs/headers/whitespace-before-headers/',
    'MAL-BINARY-GARBAGE': '/Http11Probe/docs/malformed-input/binary-garbage/',
    'MAL-CHUNK-EXTENSION-LONG': '/Http11Probe/docs/malformed-input/chunk-extension-long/',
    'MAL-CHUNK-SIZE-OVERFLOW': '/Http11Probe/docs/malformed-input/chunk-size-overflow/',
    'MAL-CL-EMPTY': '/Http11Probe/docs/malformed-input/cl-empty/',
    'MAL-CL-OVERFLOW': '/Http11Probe/docs/malformed-input/cl-overflow/',
    'MAL-CL-TAB-BEFORE-VALUE': '/Http11Probe/docs/malformed-input/cl-tab-before-value/',
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
    'SMUG-DUPLICATE-CL': '/Http11Probe/docs/smuggling/duplicate-cl/',
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
    'SMUG-CL-LEADING-ZEROS': '/Http11Probe/docs/smuggling/cl-leading-zeros/',
    'SMUG-CL-NEGATIVE': '/Http11Probe/docs/smuggling/cl-negative/',
    'RFC9112-6.1-CL-NON-NUMERIC': '/Http11Probe/docs/content-length/cl-non-numeric/',
    'RFC9112-6.1-CL-PLUS-SIGN': '/Http11Probe/docs/content-length/cl-plus-sign/',
    'SMUG-CL-TE-BOTH': '/Http11Probe/docs/smuggling/cl-te-both/',
    'RFC9112-7.1-MISSING-HOST': '/Http11Probe/docs/host-header/missing-host/',
    'SMUG-BARE-CR-HEADER-VALUE': '/Http11Probe/docs/smuggling/bare-cr-header-value/',
    'SMUG-CHUNK-BARE-SEMICOLON': '/Http11Probe/docs/smuggling/chunk-bare-semicolon/',
    'SMUG-CHUNK-EXT-CTRL': '/Http11Probe/docs/smuggling/chunk-ext-ctrl/',
    'SMUG-CHUNK-EXT-LF': '/Http11Probe/docs/smuggling/chunk-ext-lf/',
    'SMUG-CHUNK-HEX-PREFIX': '/Http11Probe/docs/smuggling/chunk-hex-prefix/',
    'SMUG-CHUNK-LEADING-SP': '/Http11Probe/docs/smuggling/chunk-leading-sp/',
    'SMUG-CHUNK-LF-TERM': '/Http11Probe/docs/smuggling/chunk-lf-term/',
    'SMUG-CHUNK-LF-TRAILER': '/Http11Probe/docs/smuggling/chunk-lf-trailer/',
    'SMUG-CHUNK-MISSING-TRAILING-CRLF': '/Http11Probe/docs/smuggling/chunk-missing-trailing-crlf/',
    'SMUG-CHUNK-NEGATIVE': '/Http11Probe/docs/smuggling/chunk-negative/',
    'SMUG-CHUNK-SPILL': '/Http11Probe/docs/smuggling/chunk-spill/',
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
    'SMUG-HEAD-CL-BODY': '/Http11Probe/docs/smuggling/head-cl-body/',
    'SMUG-OPTIONS-CL-BODY': '/Http11Probe/docs/smuggling/options-cl-body/',
    'SMUG-TE-CASE-MISMATCH': '/Http11Probe/docs/smuggling/te-case-mismatch/',
    'SMUG-TE-DOUBLE-CHUNKED': '/Http11Probe/docs/smuggling/te-double-chunked/',
    'SMUG-TE-DUPLICATE-HEADERS': '/Http11Probe/docs/smuggling/te-duplicate-headers/',
    'SMUG-TE-EMPTY-VALUE': '/Http11Probe/docs/smuggling/te-empty-value/',
    'SMUG-TE-HTTP10': '/Http11Probe/docs/smuggling/te-http10/',
    'SMUG-TE-LEADING-COMMA': '/Http11Probe/docs/smuggling/te-leading-comma/',
    'SMUG-TE-NOT-FINAL-CHUNKED': '/Http11Probe/docs/smuggling/te-not-final-chunked/',
    'SMUG-TE-SP-BEFORE-COLON': '/Http11Probe/docs/smuggling/te-sp-before-colon/',
    'SMUG-TE-TRAILING-SPACE': '/Http11Probe/docs/smuggling/te-trailing-space/',
    'SMUG-TE-IDENTITY': '/Http11Probe/docs/smuggling/te-identity/',
    'SMUG-TE-XCHUNKED': '/Http11Probe/docs/smuggling/te-xchunked/',
    'SMUG-TECL-PIPELINE': '/Http11Probe/docs/smuggling/tecl-pipeline/',
    'SMUG-TRAILER-CL': '/Http11Probe/docs/smuggling/trailer-cl/',
    'SMUG-TRAILER-HOST': '/Http11Probe/docs/smuggling/trailer-host/',
    'SMUG-TRAILER-TE': '/Http11Probe/docs/smuggling/trailer-te/',
    'SMUG-TRANSFER_ENCODING': '/Http11Probe/docs/smuggling/transfer-encoding-underscore/',
    'COMP-CHUNKED-HEX-UPPERCASE': '/Http11Probe/docs/body/chunked-hex-uppercase/',
    'COMP-CHUNKED-TRAILER-VALID': '/Http11Probe/docs/body/chunked-trailer-valid/',
    'COMP-CONNECTION-CLOSE': '/Http11Probe/docs/headers/connection-close/',
    'COMP-HOST-EMPTY-VALUE': '/Http11Probe/docs/host-header/host-empty-value/',
    'COMP-HTTP10-DEFAULT-CLOSE': '/Http11Probe/docs/headers/http10-default-close/',
    'COMP-HTTP10-NO-HOST': '/Http11Probe/docs/host-header/http10-no-host/',
    'COMP-HTTP12-VERSION': '/Http11Probe/docs/request-line/http12-version/',
    'COMP-METHOD-TRACE': '/Http11Probe/docs/request-line/method-trace/',
    'COMP-REQUEST-LINE-TAB': '/Http11Probe/docs/request-line/request-line-tab/',
    'COMP-TRACE-WITH-BODY': '/Http11Probe/docs/request-line/trace-with-body/',
    'COMP-VERSION-LEADING-ZEROS': '/Http11Probe/docs/request-line/version-leading-zeros/',
    'COMP-VERSION-MISSING-MINOR': '/Http11Probe/docs/request-line/version-missing-minor/',
    'COMP-VERSION-WHITESPACE': '/Http11Probe/docs/request-line/version-whitespace/',
    'MAL-POST-CL-HUGE-NO-BODY': '/Http11Probe/docs/malformed-input/post-cl-huge-no-body/',
    'MAL-RANGE-OVERLAPPING': '/Http11Probe/docs/malformed-input/range-overlapping/',
    'MAL-URL-BACKSLASH': '/Http11Probe/docs/malformed-input/url-backslash/',
    'MAL-URL-OVERLONG-UTF8': '/Http11Probe/docs/malformed-input/url-overlong-utf8/',
    'MAL-URL-PERCENT-CRLF': '/Http11Probe/docs/malformed-input/url-percent-crlf/',
    'MAL-URL-PERCENT-NULL': '/Http11Probe/docs/malformed-input/url-percent-null/',
    'SMUG-ABSOLUTE-URI-HOST-MISMATCH': '/Http11Probe/docs/smuggling/absolute-uri-host-mismatch/',
    'SMUG-CHUNK-BARE-CR-TERM': '/Http11Probe/docs/smuggling/chunk-bare-cr-term/',
    'SMUG-CL-DOUBLE-ZERO': '/Http11Probe/docs/smuggling/cl-double-zero/',
    'SMUG-CL-LEADING-ZEROS-OCTAL': '/Http11Probe/docs/smuggling/cl-leading-zeros-octal/',
    'SMUG-CL-NEGATIVE-ZERO': '/Http11Probe/docs/smuggling/cl-negative-zero/',
    'SMUG-CL-UNDERSCORE': '/Http11Probe/docs/smuggling/cl-underscore/',
    'SMUG-MULTIPLE-HOST-COMMA': '/Http11Probe/docs/smuggling/multiple-host-comma/',
    'SMUG-TE-OBS-FOLD': '/Http11Probe/docs/smuggling/te-obs-fold/',
    'SMUG-TE-TAB-BEFORE-VALUE': '/Http11Probe/docs/smuggling/te-tab-before-value/',
    'SMUG-TE-TRAILING-COMMA': '/Http11Probe/docs/smuggling/te-trailing-comma/',
    'SMUG-TRAILER-CONTENT-TYPE': '/Http11Probe/docs/smuggling/trailer-content-type/'
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
    servers = filterBlacklisted(servers);
    var names = servers.map(function (sv) { return sv.name; }).sort();
    var lookup = {};
    servers.forEach(function (sv) {
      var m = {};
      sv.results.forEach(function (r) { m[r.id] = r; });
      lookup[sv.name] = m;
    });
    var testIds = servers[0].results.map(function (r) { return r.id; });
    return { names: names, lookup: lookup, testIds: testIds, servers: servers };
  }

  function renderSummary(targetId, data) {
    var el = document.getElementById(targetId);
    if (!el) return;
    var servers = filterBlacklisted(data.servers || []);
    if (servers.length === 0) {
      el.innerHTML = '<p><em>No server results found.</em></p>';
      return;
    }
    function scoredCounts(sv) {
      var p = 0, w = 0, f = 0;
      if (sv.results) {
        sv.results.forEach(function (r) {
          if (r.scored === false) return;
          if (r.verdict === 'Pass') p++;
          else if (r.verdict === 'Warn') w++;
          else if (r.verdict === 'Fail') f++;
        });
      } else {
        p = sv.summary.passed || 0;
        w = sv.summary.warnings || 0;
      }
      return p + w;
    }
    var sorted = servers.slice().sort(function (a, b) {
      return scoredCounts(b) - scoredCounts(a) || a.name.localeCompare(b.name);
    });

    var html = '<div style="display:flex;flex-direction:column;gap:6px;max-width:780px;">';
    sorted.forEach(function (sv, i) {
      var s = sv.summary;
      var total = s.total || 1;

      // Compute counts from results — unscored tests are a separate bucket
      var unscored = 0, scoredPass = 0, scoredWarn = 0, scoredFail = 0;
      if (sv.results) {
        sv.results.forEach(function (r) {
          if (r.scored === false) { unscored++; return; }
          if (r.verdict === 'Pass') scoredPass++;
          else if (r.verdict === 'Warn') scoredWarn++;
          else scoredFail++;
        });
      } else {
        scoredPass = s.passed || 0;
        scoredFail = s.failed || 0;
        scoredWarn = s.warnings || 0;
      }

      var passPct = (scoredPass / total) * 100;
      var warnPct = (scoredWarn / total) * 100;
      var failPct = (scoredFail / total) * 100;
      var unscoredPct = (unscored / total) * 100;
      var rank = i + 1;

      html += '<div style="display:flex;align-items:center;gap:10px;">';
      html += '<div style="min-width:24px;text-align:right;font-size:13px;font-weight:600;color:#656d76;">' + rank + '</div>';
      var nameLabel = sv.name;
      if (sv.language) nameLabel += ' <span style="font-weight:400;color:#656d76;font-size:11px;">(' + sv.language + ')</span>';
      html += '<div style="min-width:150px;font-size:13px;font-weight:600;white-space:nowrap;">' + nameLabel + '</div>';
      var trackBg = document.documentElement.classList.contains('dark') ? '#2a2f38' : '#f0f0f0';
      html += '<div style="flex:1;height:22px;background:' + trackBg + ';border-radius:3px;overflow:hidden;display:flex;">';
      html += '<div style="height:100%;width:' + passPct + '%;background:' + PASS_BG + ';transition:width 0.3s;"></div>';
      if (scoredWarn > 0) {
        html += '<div style="height:100%;width:' + warnPct + '%;background:' + WARN_BG + ';transition:width 0.3s;"></div>';
      }
      if (scoredFail > 0) {
        html += '<div style="height:100%;width:' + failPct + '%;background:' + FAIL_BG + ';transition:width 0.3s;"></div>';
      }
      if (unscored > 0) {
        html += '<div style="height:100%;width:' + unscoredPct + '%;background:' + SKIP_BG + ';transition:width 0.3s;"></div>';
      }
      html += '</div>';
      // Score: pass + warn [fail] [unscored] / total
      html += '<div style="min-width:200px;text-align:right;font-size:13px;">';
      html += '<span style="font-weight:700;color:' + PASS_BG + ';">' + scoredPass + '</span>';
      if (scoredWarn > 0) {
        html += ' + <span style="font-weight:700;color:' + WARN_BG + ';">' + scoredWarn + '</span>';
      }
      if (scoredFail > 0) {
        html += ' <span style="color:' + FAIL_BG + ';">' + scoredFail + ' fail</span>';
      }
      if (unscored > 0) {
        html += ' <span style="color:' + SKIP_BG + ';">' + unscored + ' unscored</span>';
      }
      html += ' <span style="color:#656d76;font-size:12px;">/ ' + total + '</span>';
      html += '</div>';
      html += '</div>';
    });
    html += '</div>';

    // Legend
    var totalTests = sorted[0] ? sorted[0].summary.total : 0;
    html += '<div style="display:flex;align-items:center;gap:16px;margin-top:10px;font-size:12px;color:#656d76;">';
    html += '<span>' + totalTests + ' tests</span>';
    html += '<span style="display:inline-flex;align-items:center;gap:4px;"><span style="display:inline-block;width:10px;height:10px;border-radius:2px;background:' + PASS_BG + ';"></span> Pass</span>';
    html += '<span style="display:inline-flex;align-items:center;gap:4px;"><span style="display:inline-block;width:10px;height:10px;border-radius:2px;background:' + WARN_BG + ';"></span> Warn</span>';
    html += '<span style="display:inline-flex;align-items:center;gap:4px;"><span style="display:inline-block;width:10px;height:10px;border-radius:2px;background:' + FAIL_BG + ';"></span> Fail</span>';
    html += '</div>';

    if (data.commit) {
      html += '<p style="margin-top:8px;font-size:0.85em;color:#656d76;">Commit: <code>' + data.commit.id.substring(0, 7) + '</code> &mdash; ' + (data.commit.message || '') + '</p>';
    }
    el.innerHTML = html;
  }

  function renderTable(targetId, categoryKey, ctx, testIdFilter) {
    injectScrollStyle();
    var el = document.getElementById(targetId);
    if (!el) return;
    var names = ctx.names, lookup = ctx.lookup, testIds = ctx.testIds;

    var catTests = testIds.filter(function (tid) {
      if (!(lookup[names[0]][tid] && lookup[names[0]][tid].category === categoryKey)) return false;
      if (testIdFilter) return testIdFilter.indexOf(tid) !== -1;
      return true;
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

    var t = '<div class="probe-scroll"><table class="probe-table" style="border-collapse:collapse;font-size:12px;white-space:nowrap;">';

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
    var serverLangs = {};
    if (ctx.servers) ctx.servers.forEach(function (sv) { serverLangs[sv.name] = sv.language; });
    names.forEach(function (n) {
      t += '<tr class="probe-server-row">';
      var lang = serverLangs[n];
      var langSuffix = lang ? ' <span style="font-weight:400;color:#656d76;font-size:10px;">(' + lang + ')</span>' : '';
      t += '<td style="padding:4px 8px;font-weight:600;font-size:12px;white-space:nowrap;">' + n + langSuffix + '</td>';
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

    // Row click-to-highlight (one at a time)
    var rows = el.querySelectorAll('.probe-server-row');
    rows.forEach(function (row) {
      row.addEventListener('click', function () {
        var wasActive = row.classList.contains('probe-row-active');
        rows.forEach(function (r) { r.classList.remove('probe-row-active'); });
        if (!wasActive) row.classList.add('probe-row-active');
      });
    });
  }

  // ── Sub-table renderer ─────────────────────────────────────────
  function renderSubTables(targetId, categoryKey, ctx, groups) {
    injectScrollStyle();
    var el = document.getElementById(targetId);
    if (!el) return;

    // Find tests in this category that aren't in any explicit group
    var grouped = {};
    groups.forEach(function (g) {
      g.testIds.forEach(function (tid) { grouped[tid] = true; });
    });
    var allCatTests = ctx.testIds.filter(function (tid) {
      return ctx.lookup[ctx.names[0]][tid] && ctx.lookup[ctx.names[0]][tid].category === categoryKey;
    });
    var ungrouped = allCatTests.filter(function (tid) { return !grouped[tid]; });

    var allGroups = groups.slice();
    if (ungrouped.length > 0) {
      allGroups.push({ key: 'other', label: 'Other', testIds: ungrouped });
    }

    var html = '';
    allGroups.forEach(function (g) {
      var divId = targetId + '-' + g.key;
      html += '<h3 style="margin-top:1.5em;margin-bottom:0.3em;">' + g.label + '</h3>';
      html += '<div id="' + divId + '"></div>';
    });
    el.innerHTML = html;
    allGroups.forEach(function (g) {
      var divId = targetId + '-' + g.key;
      renderTable(divId, categoryKey, ctx, g.testIds);
    });
  }

  // ── Language filter ────────────────────────────────────────────
  function renderLanguageFilter(targetId, data, onChange) {
    var el = document.getElementById(targetId);
    var allServers = filterBlacklisted(data.servers || []);
    if (!el || allServers.length === 0) return;

    var langs = {};
    allServers.forEach(function (sv) {
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

    var labelStyle = 'font-size:12px;font-weight:700;color:#656d76;margin-right:10px;white-space:nowrap;';
    var html = '<div style="display:flex;align-items:center;flex-wrap:wrap;margin-bottom:4px;">';
    html += '<span style="' + labelStyle + '">Language:</span>';
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
          onChange({ commit: data.commit, servers: allServers });
        } else {
          var filtered = {
            commit: data.commit,
            servers: allServers.filter(function (sv) { return sv.language === lang; })
          };
          onChange(filtered);
        }
      });
    });
  }

  // ── Category filter ──────────────────────────────────────────
  function filterByCategory(data, categories) {
    return {
      commit: data.commit,
      servers: data.servers.map(function (sv) {
        var filtered = sv.results.filter(function (r) {
          return categories.indexOf(r.category) !== -1;
        });
        var scored = filtered.filter(function (r) { return r.scored !== false; });
        return {
          name: sv.name,
          language: sv.language,
          results: filtered,
          summary: {
            total: filtered.length,
            scored: scored.length,
            passed: scored.filter(function (r) { return r.verdict === 'Pass'; }).length,
            failed: scored.filter(function (r) { return r.verdict !== 'Pass' && r.verdict !== 'Warn'; }).length,
            warnings: scored.filter(function (r) { return r.verdict === 'Warn'; }).length,
            unscored: filtered.filter(function (r) { return r.scored === false; }).length
          }
        };
      })
    };
  }

  function renderCategoryFilter(targetId, onChange) {
    var el = document.getElementById(targetId);
    if (!el) return;

    var isDark = document.documentElement.classList.contains('dark');
    var baseBg = isDark ? '#21262d' : '#f6f8fa';
    var baseFg = isDark ? '#c9d1d9' : '#24292f';
    var baseBorder = isDark ? '#30363d' : '#d0d7de';
    var activeBg = isDark ? '#1f6feb' : '#0969da';

    var btnStyle = 'display:inline-block;padding:4px 12px;font-size:12px;font-weight:600;'
      + 'border-radius:20px;cursor:pointer;border:1px solid ' + baseBorder + ';'
      + 'margin-right:6px;margin-bottom:6px;transition:all 0.15s;';

    var filters = [
      { label: 'All', categories: null },
      { label: 'Compliance', categories: ['Compliance'] },
      { label: 'Smuggling', categories: ['Smuggling'] },
      { label: 'Malformed Input', categories: ['MalformedInput'] }
    ];

    var labelStyle = 'font-size:12px;font-weight:700;color:#656d76;margin-right:10px;white-space:nowrap;';
    var html = '<div style="display:flex;align-items:center;flex-wrap:wrap;margin-bottom:4px;">';
    html += '<span style="' + labelStyle + '">Category:</span>';
    filters.forEach(function (f, i) {
      var isActive = i === 0;
      html += '<button class="probe-cat-btn" data-idx="' + i + '" style="' + btnStyle
        + 'background:' + (isActive ? activeBg : baseBg) + ';color:' + (isActive ? '#fff' : baseFg)
        + ';border-color:' + (isActive ? activeBg : baseBorder) + ';">' + f.label + '</button>';
    });
    html += '</div>';
    el.innerHTML = html;

    var buttons = el.querySelectorAll('.probe-cat-btn');
    buttons.forEach(function (btn) {
      btn.addEventListener('click', function () {
        var idx = parseInt(btn.getAttribute('data-idx'));
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
        onChange(filters[idx].categories);
      });
    });
  }

  return {
    pill: pill,
    verdictBg: verdictBg,
    buildLookups: buildLookups,
    renderSummary: renderSummary,
    renderTable: renderTable,
    renderSubTables: renderSubTables,
    renderLanguageFilter: renderLanguageFilter,
    filterByCategory: filterByCategory,
    renderCategoryFilter: renderCategoryFilter,
    EXPECT_BG: EXPECT_BG
  };
})();
