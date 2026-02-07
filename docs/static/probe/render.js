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
    var html = '<div style="display:flex;gap:8px;flex-wrap:wrap;align-items:center;">';
    sorted.forEach(function (sv) {
      var s = sv.summary;
      var scored = s.scored || s.total;
      var pct = Math.round((s.passed / scored) * 100);
      var bg = pct >= 85 ? '#1a7f37' : pct >= 60 ? '#9a6700' : pct >= 20 ? '#cf222e' : '#82071e';
      html += '<div style="background:' + bg + ';color:#fff;border-radius:6px;padding:6px 12px;text-align:center;line-height:1.3;">';
      html += '<div style="font-weight:700;font-size:12px;">' + sv.name + '</div>';
      html += '<div style="font-size:16px;font-weight:800;">' + s.passed + '/' + scored + '</div>';
      html += '</div>';
    });
    html += '</div>';
    if (data.commit) {
      html += '<p style="margin-top:8px;font-size:0.85em;color:#656d76;">Commit: <code>' + data.commit.id.substring(0, 7) + '</code> &mdash; ' + (data.commit.message || '') + '</p>';
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
