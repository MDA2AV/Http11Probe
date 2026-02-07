// Shared probe rendering utilities
window.ProbeRender = (function () {
  var PASS_BG = '#1a7f37';
  var WARN_BG = '#9a6700';
  var FAIL_BG = '#cf222e';
  var SKIP_BG = '#656d76';
  var EXPECT_BG = '#444c56';
  var pillCss = 'text-align:center;padding:2px 4px;font-size:11px;font-weight:600;color:#fff;border-radius:3px;min-width:28px;display:inline-block;line-height:18px;';

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
      html += '<div style="flex:1;height:22px;background:#f0f0f0;border-radius:3px;overflow:hidden;position:relative;">';
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

    var t = '<div style="overflow-x:auto;"><table style="border-collapse:collapse;font-size:12px;white-space:nowrap;">';

    // Column header row (diagonal labels)
    t += '<thead><tr>';
    t += '<th style="padding:4px 8px;text-align:left;vertical-align:bottom;min-width:100px;"></th>';
    orderedTests.forEach(function (tid, i) {
      var first = lookup[names[0]][tid];
      var isUnscored = first.scored === false;
      var opacity = isUnscored ? 'opacity:0.55;' : '';
      t += '<th style="padding:0;height:110px;width:30px;vertical-align:bottom;' + opacity + '">';
      t += '<div style="width:30px;height:110px;position:relative;">';
      t += '<a href="/Http11Probe/glossary/#test-' + tid + '" style="font-size:10px;font-weight:500;color:inherit;text-decoration:none;position:absolute;bottom:6px;left:50%;transform-origin:bottom left;transform:rotate(-55deg);white-space:nowrap;" title="' + first.description + '">' + shortLabels[i];
      if (isUnscored) t += '*';
      t += '</a></div></th>';
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

  return {
    pill: pill,
    verdictBg: verdictBg,
    buildLookups: buildLookups,
    renderSummary: renderSummary,
    renderTable: renderTable,
    EXPECT_BG: EXPECT_BG
  };
})();
