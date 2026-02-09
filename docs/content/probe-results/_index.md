---
title: Probe Results
layout: wide
toc: false
---

{{< callout type="warning" >}}
**Glyph11** is an HTTP/1.1 parsing library currently in development and is included here only as a reference implementation. Its results should not be compared directly with production-grade servers.
{{< /callout >}}

HTTP/1.1 compliance comparison across frameworks. Each test sends a specific malformed or ambiguous request and checks the server's response against the **exact** expected status code. Updated on each manual probe run on `main`.

## Summary

<div id="lang-filter" style="margin-bottom:6px;"></div>
<div id="cat-filter" style="margin-bottom:16px;"></div>
<div id="probe-summary"><p><em>Loading probe data...</em></p></div>

**Pass** — the server gave the correct response. For most tests this means rejecting a malformed request with `400` or closing the connection. For body handling tests it means successfully reading the request body and returning `2xx`.

**Warn** — the server's response is technically valid per the RFC, but a stricter alternative exists. For example, accepting a `GET` request with a body is allowed, but rejecting it is safer because GET-with-body is a known smuggling vector. Warnings appear when the RFC uses "MAY" or "SHOULD" language rather than "MUST", giving the server a choice — the lenient option is compliant but the strict option is more secure.

**Fail** — the server gave the wrong response. It either accepted a request it should have rejected, or rejected one it should have accepted.

**Unscored** — tests marked with `*` in the detail tables. These cover RFC language that uses "MAY" or permits multiple valid behaviors, so there is no single correct answer to score against. They are still run and displayed for visibility, but do not count toward the pass/fail score.

{{< callout type="info" >}}
These results are from CI runs (`ubuntu-latest`). Click on the **Compliance**, **Smuggling**, or **Malformed Input** tabs above for detailed results per category.
{{< /callout >}}

<script src="/Http11Probe/probe/data.js"></script>
<script src="/Http11Probe/probe/render.js"></script>
<script>
(function () {
  if (!window.PROBE_DATA) {
    document.getElementById('probe-summary').innerHTML = '<p><em>No probe data available yet. Run the Probe workflow manually on <code>main</code> to generate results.</em></p>';
    return;
  }
  var langFiltered = window.PROBE_DATA;
  var catFilter = null;

  function rerender() {
    var data = langFiltered;
    if (catFilter) {
      data = ProbeRender.filterByCategory(data, catFilter);
    }
    ProbeRender.renderSummary('probe-summary', data);
  }

  ProbeRender.renderSummary('probe-summary', window.PROBE_DATA);
  ProbeRender.renderLanguageFilter('lang-filter', window.PROBE_DATA, function (filtered) {
    langFiltered = filtered;
    rerender();
  });
  ProbeRender.renderCategoryFilter('cat-filter', function (categories) {
    catFilter = categories;
    rerender();
  });
})();
</script>
