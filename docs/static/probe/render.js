// Shared probe rendering utilities
window.ProbeRender = (function () {
  var PASS_BG = '#1a7f37';
  var WARN_BG = '#9a6700';
  var FAIL_BG = '#cf222e';
  var SKIP_BG = '#656d76';
  var EXPECT_BG = '#444c56';
  var pillCss = 'text-align:center;padding:3px 6px;font-size:11px;font-weight:600;color:#fff;border-radius:4px;min-width:32px;display:inline-block;line-height:18px;cursor:default;';

  function escapeAttr(s) {
    if (!s) return '';
    return s.replace(/&/g, '&amp;').replace(/"/g, '&quot;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
  }

  // Servers temporarily hidden from results (undergoing major changes)
  var BLACKLISTED_SERVERS = [];
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
      + '.probe-scroll{overflow-x:auto;-webkit-overflow-scrolling:touch;scrollbar-width:thin;scrollbar-color:#94a3b8 #e5e7eb}'
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
      + '.probe-table thead{border-bottom:2px solid #d0d7de}'
      + '.probe-table tbody tr{border-bottom:1px solid #e1e4e8}'
      + '.probe-table th+th,.probe-table td+td{border-left:1px solid #e1e4e8}'
      + '.probe-server-row{cursor:pointer;transition:background 0.15s}'
      + '.probe-server-row:nth-child(even){background:#f8f9fb}'
      + '.probe-server-row:nth-child(even) .probe-sticky-col{background:#f8f9fb}'
      + '.probe-server-row:hover{background:#eef1f5}'
      + '.probe-server-row.probe-row-active{background:#c8ddf0 !important}'
      + '.probe-table thead a{color:#0969da !important;text-decoration:underline !important;text-underline-offset:2px}'
      // Table rows — dark
      + 'html.dark .probe-table thead{border-bottom-color:#30363d}'
      + 'html.dark .probe-table tbody tr{border-bottom-color:#262c36}'
      + 'html.dark .probe-table th+th,html.dark .probe-table td+td{border-left-color:#262c36}'
      + 'html.dark .probe-server-row:nth-child(even){background:#1e242c}'
      + 'html.dark .probe-server-row:nth-child(even) .probe-sticky-col{background:#1e242c}'
      + 'html.dark .probe-server-row:hover{background:#161b22}'
      + 'html.dark .probe-server-row.probe-row-active{background:#2a3a50 !important}'
      + 'html.dark .probe-table thead a{color:#58a6ff !important}'
      // Tooltip (hover)
      + '.probe-tooltip{position:fixed;z-index:10001;background:#1c1c1c;color:#e0e0e0;font-family:monospace;font-size:11px;'
      + 'white-space:pre;padding:8px 10px;border-radius:6px;max-width:500px;max-height:60vh;overflow:auto;'
      + 'box-shadow:0 4px 16px rgba(0,0,0,0.3);line-height:1.4}'
      + '.probe-tooltip .probe-note{color:#f0c674;font-family:sans-serif;font-weight:600;font-size:11px;margin-bottom:6px;white-space:normal}'
      + '.probe-tooltip .probe-label{color:#81a2be;font-family:sans-serif;font-weight:700;font-size:10px;text-transform:uppercase;letter-spacing:0.5px;margin-bottom:2px}'
      + '.probe-tooltip .probe-label:not(:first-child){margin-top:8px;padding-top:8px;border-top:1px solid #333}'
      // Modal (click)
      + '.probe-modal-overlay{position:fixed;inset:0;z-index:10000;background:rgba(0,0,0,0.5);display:flex;align-items:center;justify-content:center}'
      + '.probe-modal{background:#1c1c1c;color:#e0e0e0;font-family:monospace;font-size:12px;white-space:pre;'
      + 'padding:16px 20px;border-radius:8px;max-width:90vw;max-height:85vh;overflow:auto;'
      + 'box-shadow:0 8px 32px rgba(0,0,0,0.5);line-height:1.5;position:relative;min-width:300px}'
      + '.probe-modal .probe-note{color:#f0c674;font-family:sans-serif;font-weight:600;font-size:13px;margin-bottom:10px;white-space:normal}'
      + '.probe-modal .probe-label{color:#81a2be;font-family:sans-serif;font-weight:700;font-size:11px;text-transform:uppercase;letter-spacing:0.5px;margin-bottom:4px}'
      + '.probe-modal .probe-label:not(:first-child){margin-top:12px;padding-top:12px;border-top:1px solid #333}'
      + '.probe-modal-close{position:sticky;top:0;float:right;background:none;border:none;color:#808080;font-size:20px;'
      + 'cursor:pointer;padding:0 4px;line-height:1;font-family:sans-serif}'
      + '.probe-modal-close:hover{color:#fff}'
      // Sticky first column — light
      + '.probe-table .probe-sticky-col{position:sticky;left:0;z-index:2;background:#fff;box-shadow:2px 0 4px rgba(0,0,0,0.06)}'
      + '.probe-table thead .probe-sticky-col{z-index:3}'
      + 'tr[data-expected-row]{background:#f0f3f6;border-bottom:2px solid #d0d7de !important}'
      + 'tr[data-expected-row] .probe-sticky-col{background:#f0f3f6}'
      + 'tr[data-rfc-level-row]{background:#f6f8fa}'
      + 'tr[data-rfc-level-row] .probe-sticky-col{background:#f6f8fa}'
      + 'html.dark tr[data-rfc-level-row]{background:#1e242c}'
      + 'html.dark tr[data-rfc-level-row] .probe-sticky-col{background:#1e242c}'
      + 'tr[data-method-row]{background:#f6f8fa}'
      + 'tr[data-method-row] .probe-sticky-col{background:#f6f8fa}'
      + 'html.dark tr[data-method-row]{background:#1e242c}'
      + 'html.dark tr[data-method-row] .probe-sticky-col{background:#1e242c}'
      + '.probe-server-row:hover .probe-sticky-col{background:#eef1f5}'
      + '.probe-server-row.probe-row-active .probe-sticky-col{background:#c8ddf0}'
      // Sticky first column — dark
      + 'html.dark .probe-table .probe-sticky-col{background:#1c2128;box-shadow:2px 0 4px rgba(0,0,0,0.2)}'
      + 'html.dark tr[data-expected-row]{background:#21262d;border-bottom-color:#30363d !important}'
      + 'html.dark tr[data-expected-row] .probe-sticky-col{background:#21262d}'
      + 'html.dark .probe-server-row:hover .probe-sticky-col{background:#161b22}'
      + 'html.dark .probe-server-row.probe-row-active .probe-sticky-col{background:#2a3a50}'
      // Collapsible groups
      + '.probe-group-header{cursor:pointer;user-select:none;display:flex;align-items:center;gap:8px;padding:8px 0;border-bottom:1px solid #e1e4e8;color:#24292f}'
      + 'html.dark .probe-group-header{border-bottom-color:#30363d;color:#c9d1d9}'
      + '.probe-group-chevron{display:inline-block;transition:transform 0.2s;font-size:0.8em}'
      + '.probe-group-chevron.collapsed{transform:rotate(-90deg)}'
      + '.probe-group-body{overflow:hidden;transition:max-height 0.3s ease,opacity 0.3s ease;max-height:10000px;opacity:1}'
      + '.probe-group-body.collapsed{max-height:0;opacity:0}'
      + '.probe-toggle-all{display:inline-block;padding:5px 14px;font-size:12px;font-weight:600;border-radius:6px;cursor:pointer;'
      + 'border:1px solid #d0d7de;background:#f6f8fa;color:#24292f;margin-bottom:8px;transition:all 0.15s}'
      + '.probe-toggle-all:hover{background:#eef1f5}'
      + 'html.dark .probe-toggle-all{border-color:#30363d;background:#21262d;color:#c9d1d9}'
      + 'html.dark .probe-toggle-all:hover{background:#30363d}'
      // Scored/unscored separator
      + '.probe-unscored-sep{border-left:2px solid #d0d7de}'
      + 'html.dark .probe-unscored-sep{border-left-color:#30363d}'
      // Language suffix dark contrast
      + 'html.dark .probe-lang-suffix{color:#8b949e}'
      // Touch targets
      + '@media(pointer:coarse){.probe-table td{padding:4px 5px}[data-tooltip]{position:relative}[data-tooltip]::after{content:"";position:absolute;top:-6px;left:-6px;right:-6px;bottom:-6px}}'
      // Mobile modal (bottom sheet)
      + '@media(max-width:640px){.probe-modal-overlay{align-items:flex-end}.probe-modal{width:100vw;border-radius:12px 12px 0 0;max-height:92vh}.probe-modal-close{font-size:24px;padding:4px 8px}}'
      // Touch-friendly buttons
      + '@media(pointer:coarse){.probe-lang-btn,.probe-cat-btn,.probe-toggle-all{padding:8px 16px;font-size:13px;min-height:36px}}'
      // Stronger sticky shadow on mobile
      + '@media(max-width:640px){.probe-table .probe-sticky-col{box-shadow:3px 0 6px rgba(0,0,0,0.1)}html.dark .probe-table .probe-sticky-col{box-shadow:3px 0 6px rgba(0,0,0,0.3)}}'
      // Filter input
      + '.probe-filter-wrap{margin-bottom:8px;display:flex;align-items:center;gap:8px}'
      + '.probe-filter-input{width:100%;max-width:340px;padding:6px 10px;font-size:13px;border:1px solid #d0d7de;border-radius:6px;background:#fff;color:#24292f;outline:none;transition:border-color 0.15s,box-shadow 0.15s}'
      + '.probe-filter-input:focus{border-color:#0969da;box-shadow:0 0 0 3px rgba(9,105,218,0.15)}'
      + '.probe-filter-input::placeholder{color:#8b949e}'
      + 'html.dark .probe-filter-input{background:#161b22;color:#c9d1d9;border-color:#30363d}'
      + 'html.dark .probe-filter-input:focus{border-color:#1f6feb;box-shadow:0 0 0 3px rgba(31,111,235,0.2)}'
      + '.probe-filter-count{font-size:12px;color:#656d76;white-space:nowrap}';
    var style = document.createElement('style');
    style.textContent = css;
    document.head.appendChild(style);

    // Truncation detection for raw request/response (8192-byte cap)
    function isTruncated(raw) {
      if (!raw || raw.indexOf('[Truncated') !== -1) return false;
      return raw.length > 500 && raw.charAt(raw.length - 1) !== '\n';
    }

    // Tooltip hover handler (delegated)
    var tip = null;
    var tipTrigger = null;
    function dismissTip() { if (tip) { tip.remove(); tip = null; tipTrigger = null; } }
    document.addEventListener('mouseover', function (e) {
      // If mouse moved into the tooltip itself, keep it open
      if (tip && tip.contains(e.target)) return;
      var target = e.target.closest('[data-tooltip]');
      if (!target) { dismissTip(); return; }
      if (target === tipTrigger) return; // already showing for this element
      dismissTip();
      var text = target.getAttribute('data-tooltip');
      if (!text) return;
      tipTrigger = target;
      tip = document.createElement('div');
      tip.className = 'probe-tooltip';
      var note = target.getAttribute('data-note');
      var dblFlush = target.getAttribute('data-double-flush');
      var req = target.getAttribute('data-request');
      var truncated = isTruncated(req) || isTruncated(text);
      var html = '';
      if (dblFlush) html += '<div style="color:#d4880f;font-family:sans-serif;font-weight:600;font-size:10px;margin-bottom:6px;white-space:normal;">Potential double flush</div>';
      if (truncated) html += '<div style="color:#f0c674;font-family:sans-serif;font-weight:600;font-size:10px;margin-bottom:6px;white-space:normal;">[Truncated \u2014 payload exceeds display limit]</div>';
      if (note) html += '<div class="probe-note">' + escapeAttr(note) + '</div>';
      if (req) html += '<div class="probe-label">Request</div>' + escapeAttr(req);
      if (text) html += '<div class="probe-label">Response</div>' + escapeAttr(text);
      tip.innerHTML = html;
      // Dismiss when mouse leaves the tooltip
      tip.addEventListener('mouseleave', function (ev) {
        if (tipTrigger && tipTrigger.contains(ev.relatedTarget)) return;
        dismissTip();
      });
      document.body.appendChild(tip);
      var rect = target.getBoundingClientRect();
      var tipRect = tip.getBoundingClientRect();
      var left = rect.left + rect.width / 2 - tipRect.width / 2;
      if (left < 4) left = 4;
      if (left + tipRect.width > window.innerWidth - 4) left = window.innerWidth - 4 - tipRect.width;
      var top = rect.top - tipRect.height - 6;
      if (top < 4) top = rect.bottom + 6;
      tip.style.left = left + 'px';
      tip.style.top = top + 'px';
    });
    document.addEventListener('mouseout', function (e) {
      var target = e.target.closest('[data-tooltip]');
      if (!target || target !== tipTrigger) return;
      // Don't dismiss if mouse moved into the tooltip
      if (tip && (e.relatedTarget === tip || tip.contains(e.relatedTarget))) return;
      if (target && tip) { dismissTip(); }
    });

    // Modal click handler (delegated)
    document.addEventListener('click', function (e) {
      var target = e.target.closest('[data-tooltip]');
      if (!target) return;
      var text = target.getAttribute('data-tooltip');
      var req = target.getAttribute('data-request');
      if (!text && !req) return;
      // Dismiss hover tooltip
      dismissTip();

      var note = target.getAttribute('data-note');
      var dblFlush = target.getAttribute('data-double-flush');
      var truncated = isTruncated(req) || isTruncated(text);
      var html = '<button class="probe-modal-close" title="Close">&times;</button>';
      if (dblFlush) html += '<div style="color:#d4880f;font-family:sans-serif;font-weight:600;font-size:12px;margin-bottom:8px;white-space:normal;">Potential double flush \u2014 response body arrived in a separate write from the headers</div>';
      if (truncated) html += '<div style="color:#f0c674;font-family:sans-serif;font-weight:600;font-size:12px;margin-bottom:8px;white-space:normal;">[Truncated \u2014 payload exceeds display limit]</div>';
      if (note) html += '<div class="probe-note">' + escapeAttr(note) + '</div>';
      if (req) html += '<div class="probe-label">Request</div>' + escapeAttr(req);
      if (text) html += '<div class="probe-label">Response</div>' + escapeAttr(text);

      var overlay = document.createElement('div');
      overlay.className = 'probe-modal-overlay';
      var modal = document.createElement('div');
      modal.className = 'probe-modal';
      modal.innerHTML = html;
      overlay.appendChild(modal);
      document.body.appendChild(overlay);

      // Close on X button
      modal.querySelector('.probe-modal-close').addEventListener('click', function () {
        overlay.remove();
      });
      // Close on overlay click (outside modal)
      overlay.addEventListener('click', function (ev) {
        if (ev.target === overlay) overlay.remove();
      });
      // Close on Escape
      function onKey(ev) {
        if (ev.key === 'Escape') { overlay.remove(); document.removeEventListener('keydown', onKey); }
      }
      document.addEventListener('keydown', onKey);
    });
  }

  // ── Test ID → doc page URL mapping ─────────────────────────────
  var TEST_URLS = {
    'COMP-405-ALLOW': '/Http11Probe/docs/request-line/405-allow/',
    'COMP-ABSOLUTE-FORM': '/Http11Probe/docs/request-line/absolute-form/',
    'COMP-ASTERISK-WITH-GET': '/Http11Probe/docs/request-line/asterisk-with-get/',
    'COMP-BASELINE': '/Http11Probe/docs/baseline/',
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
    'COMP-OPTIONS-ALLOW': '/Http11Probe/docs/request-line/options-allow/',
    'COMP-OPTIONS-STAR': '/Http11Probe/docs/request-line/options-star/',
    'COMP-POST-CL-BODY': '/Http11Probe/docs/body/post-cl-body/',
    'COMP-POST-CL-UNDERSEND': '/Http11Probe/docs/body/post-cl-undersend/',
    'COMP-POST-CL-ZERO': '/Http11Probe/docs/body/post-cl-zero/',
    'COMP-POST-NO-CL-NO-TE': '/Http11Probe/docs/body/post-no-cl-no-te/',
    'COMP-UNKNOWN-METHOD': '/Http11Probe/docs/request-line/unknown-method/',
    'COMP-UNKNOWN-TE-501': '/Http11Probe/docs/request-line/unknown-te-501/',
    'COMP-UPGRADE-INVALID-VER': '/Http11Probe/docs/upgrade/upgrade-invalid-ver/',
    'COMP-UPGRADE-MISSING-CONN': '/Http11Probe/docs/upgrade/upgrade-missing-conn/',
    'COMP-UPGRADE-POST': '/Http11Probe/docs/upgrade/upgrade-post/',
    'COMP-UPGRADE-UNKNOWN': '/Http11Probe/docs/upgrade/upgrade-unknown/',
    'COMP-WHITESPACE-BEFORE-HEADERS': '/Http11Probe/docs/headers/whitespace-before-headers/',
    'MAL-BINARY-GARBAGE': '/Http11Probe/docs/malformed-input/binary-garbage/',
    'MAL-CHUNK-EXT-64K': '/Http11Probe/docs/malformed-input/chunk-extension-long/',
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
    'SMUG-CHUNK-EXT-CR': '/Http11Probe/docs/smuggling/chunk-ext-cr/',
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
    'SMUG-CL-COMMA-TRIPLE': '/Http11Probe/docs/smuggling/cl-comma-triple/',
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
    'SMUG-TE-FORMFEED': '/Http11Probe/docs/smuggling/te-formfeed/',
    'SMUG-TE-HTTP10': '/Http11Probe/docs/smuggling/te-http10/',
    'SMUG-TE-LEADING-COMMA': '/Http11Probe/docs/smuggling/te-leading-comma/',
    'SMUG-TE-NOT-FINAL-CHUNKED': '/Http11Probe/docs/smuggling/te-not-final-chunked/',
    'SMUG-TE-NULL': '/Http11Probe/docs/smuggling/te-null/',
    'SMUG-TE-SP-BEFORE-COLON': '/Http11Probe/docs/smuggling/te-sp-before-colon/',
    'SMUG-TE-TRAILING-SPACE': '/Http11Probe/docs/smuggling/te-trailing-space/',
    'SMUG-TE-VTAB': '/Http11Probe/docs/smuggling/te-vtab/',
    'SMUG-TE-IDENTITY': '/Http11Probe/docs/smuggling/te-identity/',
    'SMUG-TE-XCHUNKED': '/Http11Probe/docs/smuggling/te-xchunked/',
    'SMUG-TECL-PIPELINE': '/Http11Probe/docs/smuggling/tecl-pipeline/',
    'SMUG-TRAILER-AUTH': '/Http11Probe/docs/smuggling/trailer-auth/',
    'SMUG-TRAILER-CL': '/Http11Probe/docs/smuggling/trailer-cl/',
    'SMUG-TRAILER-HOST': '/Http11Probe/docs/smuggling/trailer-host/',
    'SMUG-TRAILER-TE': '/Http11Probe/docs/smuggling/trailer-te/',
    'SMUG-TRANSFER_ENCODING': '/Http11Probe/docs/smuggling/transfer-encoding-underscore/',
    'COMP-CHUNKED-HEX-UPPERCASE': '/Http11Probe/docs/body/chunked-hex-uppercase/',
    'COMP-CHUNKED-TRAILER-VALID': '/Http11Probe/docs/body/chunked-trailer-valid/',
    'COMP-CONNECTION-CLOSE': '/Http11Probe/docs/headers/connection-close/',
    'COMP-CONTENT-TYPE': '/Http11Probe/docs/headers/content-type-presence/',
    'COMP-DATE-HEADER': '/Http11Probe/docs/headers/date-header/',
    'COMP-HEAD-NO-BODY': '/Http11Probe/docs/request-line/head-no-body/',
    'COMP-HOST-EMPTY-VALUE': '/Http11Probe/docs/host-header/host-empty-value/',
    'COMP-HTTP10-DEFAULT-CLOSE': '/Http11Probe/docs/headers/http10-default-close/',
    'COMP-HTTP10-NO-HOST': '/Http11Probe/docs/host-header/http10-no-host/',
    'COMP-NO-1XX-HTTP10': '/Http11Probe/docs/headers/no-1xx-http10/',
    'COMP-NO-CL-IN-204': '/Http11Probe/docs/content-length/no-cl-in-204/',
    'COMP-HTTP12-VERSION': '/Http11Probe/docs/request-line/http12-version/',
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
    'SMUG-TRAILER-CONTENT-TYPE': '/Http11Probe/docs/smuggling/trailer-content-type/',
    'NORM-UNDERSCORE-CL': '/Http11Probe/docs/normalization/underscore-cl/',
    'NORM-SP-BEFORE-COLON-CL': '/Http11Probe/docs/normalization/sp-before-colon-cl/',
    'NORM-TAB-IN-NAME': '/Http11Probe/docs/normalization/tab-in-name/',
    'NORM-CASE-TE': '/Http11Probe/docs/normalization/case-te/',
    'NORM-UNDERSCORE-TE': '/Http11Probe/docs/normalization/underscore-te/'
  };

  function testUrl(tid) {
    return TEST_URLS[tid] || '';
  }

  // ── Server name → config page URL mapping ────────────────────
  var SERVER_URLS = {
    'Actix': '/Http11Probe/servers/actix/',
    'Apache': '/Http11Probe/servers/apache/',
    'Bun': '/Http11Probe/servers/bun/',
    'Caddy': '/Http11Probe/servers/caddy/',
    'Deno': '/Http11Probe/servers/deno/',
    'EmbedIO': '/Http11Probe/servers/embedio/',
    'Envoy': '/Http11Probe/servers/envoy/',
    'Express': '/Http11Probe/servers/express/',
    'FastHTTP': '/Http11Probe/servers/fasthttp/',
    'Flask': '/Http11Probe/servers/flask/',
    'GenHTTP': '/Http11Probe/servers/genhttp/',
    'Gin': '/Http11Probe/servers/gin/',
    'Glyph11': '/Http11Probe/servers/glyph/',
    'Gunicorn': '/Http11Probe/servers/gunicorn/',
    'H2O': '/Http11Probe/servers/h2o/',
    'HAProxy': '/Http11Probe/servers/haproxy/',
    'Hyper': '/Http11Probe/servers/hyper/',
    'Jetty': '/Http11Probe/servers/jetty/',
    'Kestrel': '/Http11Probe/servers/aspnet-minimal/',
    'Lighttpd': '/Http11Probe/servers/lighttpd/',
    'NetCoreServer': '/Http11Probe/servers/netcoreserver/',
    'Nginx': '/Http11Probe/servers/nginx/',
    'Node': '/Http11Probe/servers/node/',
    'Ntex': '/Http11Probe/servers/ntex/',
    'PHP': '/Http11Probe/servers/php/',
    'Pingora': '/Http11Probe/servers/pingora/',
    'Puma': '/Http11Probe/servers/puma/',
    'Quarkus': '/Http11Probe/servers/quarkus/',
    'ServiceStack': '/Http11Probe/servers/servicestack/',
    'SimpleW': '/Http11Probe/servers/simplew/',
    'Sisk': '/Http11Probe/servers/sisk/',
    'Spring Boot': '/Http11Probe/servers/spring-boot/',
    'Tomcat': '/Http11Probe/servers/tomcat/',
    'Traefik': '/Http11Probe/servers/traefik/',
    'Uvicorn': '/Http11Probe/servers/uvicorn/',
    'Watson': '/Http11Probe/servers/watson/'
  };
  function serverUrl(name) { return SERVER_URLS[name] || ''; }

  function pill(bg, label, tooltipRaw, tooltipNote, tooltipReq, doubleFlush) {
    var extra = '';
    var hasData = tooltipRaw || tooltipReq;
    if (hasData) extra += ' data-tooltip="' + escapeAttr(tooltipRaw || '') + '"';
    if (tooltipNote) extra += ' data-note="' + escapeAttr(tooltipNote) + '"';
    if (tooltipReq) extra += ' data-request="' + escapeAttr(tooltipReq) + '"';
    if (doubleFlush) extra += ' data-double-flush="1"';
    var cursor = hasData ? 'cursor:pointer;' : 'cursor:default;';
    var border = doubleFlush ? 'border:2px solid #d4880f;' : '';
    return '<span style="' + pillCss + cursor + 'background:' + bg + ';' + border + '"' + extra + '>' + label + '</span>';
  }

  function verdictBg(v) {
    return v === 'Pass' ? PASS_BG : v === 'Warn' ? WARN_BG : FAIL_BG;
  }

  var KNOWN_METHODS = { GET:1, POST:1, HEAD:1, PUT:1, DELETE:1, PATCH:1, OPTIONS:1, TRACE:1, CONNECT:1 };

  function methodFromRequest(rawReq) {
    if (!rawReq) return '?';
    // Strip leading CRLF (e.g. COMP-LEADING-CRLF sends \r\n\r\nGET ...)
    var trimmed = rawReq.replace(/^[\r\n]+/, '');
    if (!trimmed) return '?';
    // Find first space or tab (tab used in COMP-REQUEST-LINE-TAB)
    var sp = trimmed.search(/[\t ]/);
    if (sp <= 0 || sp > 10) return '?';
    var method = trimmed.substring(0, sp).toUpperCase();
    // Only return known HTTP methods; PRI, FOOBAR, etc. → '?'
    return KNOWN_METHODS[method] ? method : '?';
  }

  var METHOD_COLORS = {
    GET:     { fg: '#0969da', border: '#0969da', bg: 'rgba(9,105,218,0.08)' },
    POST:    { fg: '#1a7f37', border: '#1a7f37', bg: 'rgba(26,127,55,0.08)' },
    HEAD:    { fg: '#8250df', border: '#8250df', bg: 'rgba(130,80,223,0.08)' },
    OPTIONS: { fg: '#9a6700', border: '#9a6700', bg: 'rgba(154,103,0,0.08)' },
    DELETE:  { fg: '#cf222e', border: '#cf222e', bg: 'rgba(207,34,46,0.08)' },
    TRACE:   { fg: '#656d76', border: '#656d76', bg: 'rgba(101,109,118,0.08)' },
    CONNECT: { fg: '#656d76', border: '#656d76', bg: 'rgba(101,109,118,0.08)' },
    PUT:     { fg: '#bf8700', border: '#bf8700', bg: 'rgba(191,135,0,0.08)' },
    PATCH:   { fg: '#0550ae', border: '#0550ae', bg: 'rgba(5,80,174,0.08)' }
  };
  var METHOD_DEFAULT = { fg: '#656d76', border: '#656d76', bg: 'rgba(101,109,118,0.08)' };

  function methodTag(method) {
    var c = METHOD_COLORS[method] || METHOD_DEFAULT;
    return '<span style="display:inline-block;padding:2px 6px;font-size:10px;font-weight:700;'
      + 'font-family:ui-monospace,SFMono-Regular,monospace;letter-spacing:0.3px;'
      + 'color:' + c.fg + ';border:1px solid ' + c.border + ';background:' + c.bg + ';'
      + 'border-radius:3px;line-height:16px;cursor:default;">' + method + '</span>';
  }

  var RFC_LEVEL_COLORS = {
    Must:    { fg: '#cf222e', bg: 'rgba(207,34,46,0.12)' },
    Should:  { fg: '#9a6700', bg: 'rgba(154,103,0,0.12)' },
    OughtTo: { fg: '#b08800', bg: 'rgba(176,136,0,0.12)' },
    May:     { fg: '#0969da', bg: 'rgba(9,105,218,0.12)' }
  };
  var RFC_LEVEL_DEFAULT = { fg: '#656d76', bg: 'rgba(101,109,118,0.10)' };

  function rfcLevelLabel(level) {
    if (level === 'Must') return 'MUST';
    if (level === 'Should') return 'SHOULD';
    if (level === 'OughtTo') return 'OUGHT TO';
    if (level === 'May') return 'MAY';
    return 'N/A';
  }

  function rfcLevelTag(level) {
    var c = RFC_LEVEL_COLORS[level] || RFC_LEVEL_DEFAULT;
    var label = rfcLevelLabel(level);
    return '<span style="display:inline-block;padding:2px 7px;font-size:10px;font-weight:700;'
      + 'letter-spacing:0.4px;color:' + c.fg + ';background:' + c.bg + ';'
      + 'border-radius:10px;line-height:16px;cursor:default;">' + label + '</span>';
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
      var sUrl = serverUrl(sv.name);
      var nameLabel = sUrl
        ? '<a href="' + sUrl + '" style="color:inherit;text-decoration:none;" onmouseover="this.style.textDecoration=\'underline\'" onmouseout="this.style.textDecoration=\'none\'">' + sv.name + '</a>'
        : sv.name;
      if (sv.language) nameLabel += ' <span class="probe-lang-suffix" style="font-weight:400;color:#656d76;font-size:11px;">(' + sv.language + ')</span>';
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

  var CAT_LABELS = { Compliance: 'Compliance', Smuggling: 'Smuggling', MalformedInput: 'Malformed Input', Normalization: 'Normalization' };

  function renderTable(targetId, categoryKey, ctx, testIdFilter, tableLabel) {
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
      return tid.replace(/^(RFC\d+-[\d.]+-|COMP-|SMUG-|MAL-|NORM-)/, '');
    });

    var unscoredStart = scoredTests.length;
    var t = '<div class="probe-scroll"><table class="probe-table" style="border-collapse:collapse;font-size:13px;white-space:nowrap;">';

    // Column header row (horizontal labels)
    t += '<thead><tr>';
    t += '<th class="probe-sticky-col" style="padding:6px 10px;text-align:left;vertical-align:bottom;min-width:100px;"></th>';
    orderedTests.forEach(function (tid, i) {
      var first = lookup[names[0]][tid];
      var isUnscored = first.scored === false;
      var opacity = isUnscored ? 'opacity:0.55;' : '';
      var sepCls = i === unscoredStart ? ' probe-unscored-sep' : '';
      var url = testUrl(tid);
      t += '<th data-test-label="' + escapeAttr(shortLabels[i]) + '" class="' + sepCls + '" style="padding:6px 8px;vertical-align:bottom;white-space:nowrap;' + opacity + '">';
      if (url) {
        t += '<a href="' + url + '" style="font-size:10.5px;font-weight:600;letter-spacing:0.3px;color:inherit;text-decoration:none;" title="' + escapeAttr(first.description) + '">' + shortLabels[i];
      } else {
        t += '<span style="font-size:10.5px;font-weight:600;letter-spacing:0.3px;" title="' + escapeAttr(first.description) + '">' + shortLabels[i];
      }
      if (isUnscored) t += '*';
      t += url ? '</a>' : '</span>';
      t += '</th>';
    });
    t += '</tr></thead><tbody>';

    // RFC Level row
    t += '<tr data-rfc-level-row>';
    t += '<td class="probe-sticky-col" style="padding:6px 10px;font-weight:700;font-size:12px;color:#656d76;">RFC Level</td>';
    orderedTests.forEach(function (tid, i) {
      var first = lookup[names[0]][tid];
      var level = first.rfcLevel || 'Must';
      var sepCls = i === unscoredStart ? ' probe-unscored-sep' : '';
      t += '<td data-test-label="' + escapeAttr(shortLabels[i]) + '" class="' + sepCls + '" style="text-align:center;padding:3px 4px;">' + rfcLevelTag(level) + '</td>';
    });
    t += '</tr>';

    // Method row
    t += '<tr data-method-row>';
    t += '<td class="probe-sticky-col" style="padding:6px 10px;font-weight:700;font-size:12px;color:#656d76;">Method</td>';
    orderedTests.forEach(function (tid, i) {
      var sepCls = i === unscoredStart ? ' probe-unscored-sep' : '';
      // Find rawRequest from any server that has this test
      var method = '?';
      for (var ni = 0; ni < names.length; ni++) {
        var r = lookup[names[ni]][tid];
        if (r && r.rawRequest) { method = methodFromRequest(r.rawRequest); break; }
      }
      t += '<td data-test-label="' + escapeAttr(shortLabels[i]) + '" class="' + sepCls + '" style="text-align:center;padding:3px 4px;">' + methodTag(method) + '</td>';
    });
    t += '</tr>';

    // Expected row
    t += '<tr data-expected-row>';
    t += '<td class="probe-sticky-col" style="padding:6px 10px;font-weight:700;font-size:12px;color:#656d76;">Expected</td>';
    orderedTests.forEach(function (tid, i) {
      var first = lookup[names[0]][tid];
      var isUnscored = first.scored === false;
      var opacity = isUnscored ? 'opacity:0.55;' : '';
      var sepCls = i === unscoredStart ? ' probe-unscored-sep' : '';
      t += '<td data-test-label="' + escapeAttr(shortLabels[i]) + '" class="' + sepCls + '" style="text-align:center;padding:3px 4px;' + opacity + '">' + pill(EXPECT_BG, first.expected.replace(/ or close/g, '/\u2715').replace(/\//g, '/\u200B')) + '</td>';
    });
    t += '</tr>';

    // Server rows
    var serverLangs = {};
    if (ctx.servers) ctx.servers.forEach(function (sv) { serverLangs[sv.name] = sv.language; });
    names.forEach(function (n) {
      var lang = serverLangs[n];
      t += '<tr class="probe-server-row" data-server="' + escapeAttr(n) + '" data-language="' + escapeAttr(lang || '') + '">';
      var langSuffix = lang ? ' <span class="probe-lang-suffix" style="font-weight:400;color:#656d76;font-size:10px;">(' + lang + ')</span>' : '';
      var srvUrl = serverUrl(n);
      var srvName = srvUrl
        ? '<a href="' + srvUrl + '" style="color:inherit;text-decoration:none;" onmouseover="this.style.textDecoration=\'underline\'" onmouseout="this.style.textDecoration=\'none\'">' + n + '</a>'
        : n;
      t += '<td class="probe-sticky-col" style="padding:6px 10px;font-weight:600;font-size:13px;white-space:nowrap;">' + srvName + langSuffix + '</td>';
      orderedTests.forEach(function (tid, i) {
        var r = lookup[n] && lookup[n][tid];
        var isUnscored = lookup[names[0]][tid].scored === false;
        var opacity = isUnscored ? 'opacity:0.55;' : '';
        var sepCls = i === unscoredStart ? ' probe-unscored-sep' : '';
        if (!r) {
          t += '<td data-test-label="' + escapeAttr(shortLabels[i]) + '" class="' + sepCls + '" style="text-align:center;padding:3px 4px;' + opacity + '">' + pill(SKIP_BG, '\u2014') + '</td>';
          return;
        }
        t += '<td data-test-label="' + escapeAttr(shortLabels[i]) + '" class="' + sepCls + '" style="text-align:center;padding:3px 4px;' + opacity + '">' + pill(verdictBg(r.verdict), r.got, r.rawResponse, r.behavioralNote, r.rawRequest, r.doubleFlush) + '</td>';
      });
      t += '</tr>';
    });

    t += '</tbody></table></div>';
    if (unscoredTests.length > 0) {
      t += '<p style="font-size:0.8em;color:#656d76;margin-top:4px;">* Not scored &mdash; RFC-compliant behavior, shown for reference.</p>';
    }
    el.innerHTML = t;

    // Overflow scroll hint + right-edge fade
    var scrollEl = el.querySelector('.probe-scroll');
    if (scrollEl && orderedTests.length > 3) {
      var isDark = document.documentElement.classList.contains('dark');

      // Hint label
      var hint = document.createElement('div');
      hint.style.cssText = 'text-align:right;font-size:14px;font-weight:600;color:#656d76;margin-bottom:6px;';
      hint.innerHTML = '\u2B95 Scroll to see all tests';
      scrollEl.parentNode.insertBefore(hint, scrollEl);

      // Wrap scroll container so fade can sit on top
      var wrapper = document.createElement('div');
      wrapper.style.cssText = 'position:relative;';
      scrollEl.parentNode.insertBefore(wrapper, scrollEl);
      wrapper.appendChild(scrollEl);

      // Fade overlay (sibling of scroll, not inside it)
      var fadeEl = document.createElement('div');
      fadeEl.style.cssText = 'position:absolute;top:0;right:0;bottom:0;width:120px;pointer-events:none;'
        + 'background:linear-gradient(to right,transparent,' + (isDark ? 'rgba(28,33,40,0.95)' : 'rgba(255,255,255,0.92)') + ');'
        + 'transition:opacity 0.3s;';
      wrapper.appendChild(fadeEl);

      scrollEl.addEventListener('scroll', function () {
        var atEnd = scrollEl.scrollLeft + scrollEl.clientWidth >= scrollEl.scrollWidth - 1;
        fadeEl.style.opacity = atEnd ? '0' : '1';
      });

    }

    // Row click → detail popup
    var rows = el.querySelectorAll('.probe-server-row');
    rows.forEach(function (row) {
      row.addEventListener('click', function (e) {
        if (e.target.closest('a') || e.target.closest('[data-tooltip]')) return;

        var svName = row.getAttribute('data-server');
        if (!svName) return;

        // Build vertical detail table for this server
        var sUrl = serverUrl(svName);
        var titleHtml = sUrl
          ? '<a href="' + sUrl + '" style="color:#58a6ff;text-decoration:underline;text-underline-offset:2px;">' + escapeAttr(svName) + '</a>'
          : escapeAttr(svName);

        var pass = 0, warn = 0, fail = 0;
        orderedTests.forEach(function (tid) {
          var r = lookup[svName] && lookup[svName][tid];
          if (!r || r.scored === false) return;
          if (r.verdict === 'Pass') pass++;
          else if (r.verdict === 'Warn') warn++;
          else fail++;
        });

        var displayLabel = tableLabel || CAT_LABELS[categoryKey] || categoryKey;
        var h = '<button class="probe-modal-close" title="Close">&times;</button>';
        h += '<div style="font-size:11px;font-weight:600;color:#81a2be;text-transform:uppercase;letter-spacing:0.5px;margin-bottom:2px;">' + escapeAttr(displayLabel) + '</div>';
        h += '<div style="font-size:15px;font-weight:700;margin-bottom:4px;">' + titleHtml + '</div>';
        h += '<div style="display:flex;gap:12px;margin-bottom:12px;font-size:12px;font-weight:600;">';
        h += '<span style="color:' + PASS_BG + ';">' + pass + ' Pass</span>';
        if (warn > 0) h += '<span style="color:' + WARN_BG + ';">' + warn + ' Warn</span>';
        if (fail > 0) h += '<span style="color:' + FAIL_BG + ';">' + fail + ' Fail</span>';
        h += '</div>';

        h += '<table style="border-collapse:collapse;width:100%;font-size:12px;">';
        h += '<thead><tr style="border-bottom:1px solid #333;">';
        h += '<th style="padding:4px 8px;text-align:left;color:#81a2be;">Test</th>';
        h += '<th style="padding:4px 8px;text-align:center;color:#81a2be;">Method</th>';
        h += '<th style="padding:4px 8px;text-align:center;color:#81a2be;">RFC</th>';
        h += '<th style="padding:4px 8px;text-align:center;color:#81a2be;">Expected</th>';
        h += '<th style="padding:4px 8px;text-align:center;color:#81a2be;">Got</th>';
        h += '<th style="padding:4px 8px;text-align:left;color:#81a2be;">Description</th>';
        h += '</tr></thead><tbody>';

        orderedTests.forEach(function (tid) {
          var first = lookup[names[0]][tid];
          var r = lookup[svName] && lookup[svName][tid];
          var isUnscored = first.scored === false;
          var opacity = isUnscored ? 'opacity:0.55;' : '';
          var shortLabel = tid.replace(/^(RFC\d+-[\d.]+-|COMP-|SMUG-|MAL-|NORM-)/, '');
          var url = testUrl(tid);
          var testLink = url
            ? '<a href="' + url + '" style="color:#58a6ff;text-decoration:underline;text-underline-offset:2px;">' + shortLabel + '</a>'
            : shortLabel;
          if (isUnscored) testLink += '*';

          var gotCell;
          if (!r) {
            gotCell = pill(SKIP_BG, '\u2014');
          } else {
            gotCell = pill(verdictBg(r.verdict), r.got, r.rawResponse, r.behavioralNote, r.rawRequest, r.doubleFlush);
          }

          var method = r ? methodFromRequest(r.rawRequest) : methodFromRequest(first.rawRequest);
          var level = first.rfcLevel || 'Must';

          h += '<tr style="border-bottom:1px solid #2a2f38;' + opacity + '">';
          h += '<td style="padding:4px 8px;font-weight:600;white-space:nowrap;">' + testLink + '</td>';
          h += '<td style="text-align:center;padding:2px 4px;">' + methodTag(method) + '</td>';
          h += '<td style="text-align:center;padding:2px 4px;">' + rfcLevelTag(level) + '</td>';
          h += '<td style="text-align:center;padding:2px 4px;">' + pill(EXPECT_BG, first.expected.replace(/ or close/g, '/\u2715').replace(/\//g, '/\u200B')) + '</td>';
          h += '<td style="text-align:center;padding:2px 4px;">' + gotCell + '</td>';
          h += '<td style="padding:4px 8px;color:#999;white-space:normal;max-width:300px;">' + (first.description || '') + '</td>';
          h += '</tr>';
        });
        h += '</tbody></table>';

        var overlay = document.createElement('div');
        overlay.className = 'probe-modal-overlay';
        var modal = document.createElement('div');
        modal.className = 'probe-modal';
        modal.style.maxWidth = '800px';
        modal.style.whiteSpace = 'normal';
        modal.innerHTML = h;
        overlay.appendChild(modal);
        document.body.appendChild(overlay);

        modal.querySelector('.probe-modal-close').addEventListener('click', function () { overlay.remove(); });
        overlay.addEventListener('click', function (ev) { if (ev.target === overlay) overlay.remove(); });
        function onKey(ev) {
          if (ev.key === 'Escape') { overlay.remove(); document.removeEventListener('keydown', onKey); }
        }
        document.addEventListener('keydown', onKey);
      });
    });
  }

  // ── Collapsible-group wiring helper ────────────────────────────
  function wireCollapsible(el, targetId) {
    var headers = el.querySelectorAll('.probe-group-header');
    headers.forEach(function (hdr) {
      hdr.addEventListener('click', function () {
        var groupId = hdr.getAttribute('data-group');
        var body = document.getElementById(groupId);
        var chevron = hdr.querySelector('.probe-group-chevron');
        if (body) body.classList.toggle('collapsed');
        if (chevron) chevron.classList.toggle('collapsed');
        updateToggleAllLabel(el, targetId);
      });
    });
    var toggleBtn = el.querySelector('.probe-toggle-all');
    if (toggleBtn) {
      toggleBtn.addEventListener('click', function () {
        var bodies = el.querySelectorAll('.probe-group-body');
        var chevrons = el.querySelectorAll('.probe-group-chevron');
        var allCollapsed = Array.prototype.every.call(bodies, function (b) { return b.classList.contains('collapsed'); });
        bodies.forEach(function (b) {
          if (allCollapsed) b.classList.remove('collapsed'); else b.classList.add('collapsed');
        });
        chevrons.forEach(function (c) {
          if (allCollapsed) c.classList.remove('collapsed'); else c.classList.add('collapsed');
        });
        updateToggleAllLabel(el, targetId);
      });
    }
  }

  function updateToggleAllLabel(container, targetId) {
    var btn = container.querySelector('.probe-toggle-all[data-target="' + targetId + '"]');
    if (!btn) return;
    var bodies = container.querySelectorAll('.probe-group-body');
    var allCollapsed = Array.prototype.every.call(bodies, function (b) { return b.classList.contains('collapsed'); });
    btn.textContent = allCollapsed ? 'Expand All' : 'Collapse All';
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

    var html = '<div class="probe-filter-wrap"><input class="probe-filter-input" type="text" placeholder="Filter by server or test name (comma-separated)\u2026"><span class="probe-filter-count"></span></div>';
    html += '<button class="probe-toggle-all" data-target="' + targetId + '">Collapse All</button>';
    allGroups.forEach(function (g) {
      var divId = targetId + '-' + g.key;
      html += '<h3 class="probe-group-header" data-group="' + divId + '">'
        + '<span class="probe-group-chevron">\u25BC</span>' + g.label + '</h3>';
      html += '<div class="probe-group-body" id="' + divId + '"></div>';
    });
    el.innerHTML = html;
    var catLabel = CAT_LABELS[categoryKey] || categoryKey;
    allGroups.forEach(function (g) {
      var divId = targetId + '-' + g.key;
      renderTable(divId, categoryKey, ctx, g.testIds, catLabel + ' \u2014 ' + g.label);
    });
    wireCollapsible(el, targetId);

    // Filter handler
    var filterInput = el.querySelector('.probe-filter-input');
    var filterCount = el.querySelector('.probe-filter-count');
    if (filterInput) {
      function matchesAny(text, keywords) {
        for (var k = 0; k < keywords.length; k++) {
          if (text.indexOf(keywords[k]) !== -1) return true;
        }
        return false;
      }

      filterInput.addEventListener('input', function () {
        var raw = filterInput.value.toLowerCase();
        var keywords = raw.split(',').map(function (s) { return s.trim(); }).filter(Boolean);
        var rows = el.querySelectorAll('.probe-server-row');
        var allCols = el.querySelectorAll('[data-test-label]');
        var thCols = el.querySelectorAll('thead [data-test-label]');

        if (keywords.length === 0) {
          rows.forEach(function (r) { r.style.display = ''; });
          allCols.forEach(function (c) { c.style.display = ''; });
          if (filterCount) filterCount.textContent = '';
          return;
        }

        // Count matching servers
        var serverMatches = 0;
        rows.forEach(function (r) {
          var name = (r.getAttribute('data-server') || '').toLowerCase();
          var lang = (r.getAttribute('data-language') || '').toLowerCase();
          if (matchesAny(name, keywords) || matchesAny(lang, keywords)) serverMatches++;
        });

        // Count matching test columns
        var colMatchSet = {};
        thCols.forEach(function (th) {
          var label = th.getAttribute('data-test-label').toLowerCase();
          if (matchesAny(label, keywords)) colMatchSet[th.getAttribute('data-test-label')] = true;
        });
        var colMatches = Object.keys(colMatchSet).length;

        // Apply row filter (skip if no server matches — show all)
        rows.forEach(function (r) {
          var name = (r.getAttribute('data-server') || '').toLowerCase();
          var lang = (r.getAttribute('data-language') || '').toLowerCase();
          r.style.display = (serverMatches === 0 || matchesAny(name, keywords) || matchesAny(lang, keywords)) ? '' : 'none';
        });

        // Apply column filter (skip if no column matches — show all)
        allCols.forEach(function (c) {
          var label = c.getAttribute('data-test-label');
          c.style.display = (colMatches === 0 || colMatchSet[label]) ? '' : 'none';
        });

        // Update count label
        if (filterCount) {
          var parts = [];
          if (serverMatches > 0) parts.push(serverMatches + ' server' + (serverMatches !== 1 ? 's' : ''));
          if (colMatches > 0) parts.push(colMatches + ' test' + (colMatches !== 1 ? 's' : ''));
          filterCount.textContent = parts.length > 0 ? parts.join(', ') : 'No matches';
        }
      });
    }
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
      { label: 'Malformed Input', categories: ['MalformedInput'] },
      { label: 'Normalization', categories: ['Normalization'] }
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

  // ── Method filter ─────────────────────────────────────────────
  function filterByMethod(data, method) {
    return {
      commit: data.commit,
      servers: data.servers.map(function (sv) {
        var filtered = sv.results.filter(function (r) {
          return methodFromRequest(r.rawRequest) === method;
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

  function renderMethodFilter(targetId, data, onChange) {
    var el = document.getElementById(targetId);
    if (!el) return;
    var allServers = filterBlacklisted(data.servers || []);
    if (allServers.length === 0) return;

    // Collect unique methods from the first server's results
    var methodSet = {};
    allServers[0].results.forEach(function (r) {
      var m = methodFromRequest(r.rawRequest);
      if (m !== '?') methodSet[m] = true;
    });
    var methods = Object.keys(methodSet).sort();
    if (methods.length === 0) return;

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
    html += '<span style="' + labelStyle + '">Method:</span>';
    html += '<button class="probe-method-btn" data-method="" style="' + btnStyle
      + 'background:' + activeBg + ';color:#fff;border-color:' + activeBg + ';">All</button>';
    methods.forEach(function (m) {
      html += '<button class="probe-method-btn" data-method="' + m + '" style="' + btnStyle
        + 'background:' + baseBg + ';color:' + baseFg + ';">' + m + '</button>';
    });
    html += '</div>';
    el.innerHTML = html;

    var buttons = el.querySelectorAll('.probe-method-btn');
    buttons.forEach(function (btn) {
      btn.addEventListener('click', function () {
        var method = btn.getAttribute('data-method');
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
        onChange(method || null);
      });
    });
  }

  // ── RFC Level filter ────────────────────────────────────────────
  function filterByRfcLevel(data, level) {
    return {
      commit: data.commit,
      servers: data.servers.map(function (sv) {
        var filtered = sv.results.filter(function (r) {
          return (r.rfcLevel || 'Must') === level;
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

  function renderRfcLevelFilter(targetId, data, onChange) {
    var el = document.getElementById(targetId);
    if (!el) return;

    var levels = [
      { key: 'Must', label: 'MUST' },
      { key: 'Should', label: 'SHOULD' },
      { key: 'May', label: 'MAY' },
      { key: 'OughtTo', label: 'OUGHT TO' },
      { key: 'NotApplicable', label: 'N/A' }
    ];

    // Only show levels that exist in the data
    var allServers = filterBlacklisted(data.servers || []);
    if (allServers.length === 0) return;
    var presentLevels = {};
    allServers[0].results.forEach(function (r) {
      presentLevels[r.rfcLevel || 'Must'] = true;
    });
    var visibleLevels = levels.filter(function (l) { return presentLevels[l.key]; });
    if (visibleLevels.length === 0) return;

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
    html += '<span style="' + labelStyle + '">RFC Level:</span>';
    html += '<button class="probe-rfc-btn" data-level="" style="' + btnStyle
      + 'background:' + activeBg + ';color:#fff;border-color:' + activeBg + ';">All</button>';
    visibleLevels.forEach(function (l) {
      html += '<button class="probe-rfc-btn" data-level="' + l.key + '" style="' + btnStyle
        + 'background:' + baseBg + ';color:' + baseFg + ';">' + l.label + '</button>';
    });
    html += '</div>';
    el.innerHTML = html;

    var buttons = el.querySelectorAll('.probe-rfc-btn');
    buttons.forEach(function (btn) {
      btn.addEventListener('click', function () {
        var level = btn.getAttribute('data-level');
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
        onChange(level || null);
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
    filterByMethod: filterByMethod,
    renderMethodFilter: renderMethodFilter,
    filterByRfcLevel: filterByRfcLevel,
    renderRfcLevelFilter: renderRfcLevelFilter,
    EXPECT_BG: EXPECT_BG
  };
})();
