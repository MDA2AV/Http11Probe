---
title: "Deno"
toc: true
breadcrumbs: false
---

**Language:** TypeScript · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/DenoServer)

## Dockerfile

```dockerfile
FROM denoland/deno:latest
COPY src/Servers/DenoServer/server.ts /app/server.ts
WORKDIR /app
RUN deno cache server.ts
EXPOSE 8080
CMD ["deno", "run", "--allow-net", "server.ts"]
```

## Source

```typescript
Deno.serve({ port: 8080, hostname: "0.0.0.0" }, async (req) => {
  const url = new URL(req.url);
  if (url.pathname === "/echo") {
    let body = "";
    for (const [name, value] of req.headers) {
      body += name + ": " + value + "\n";
    }
    return new Response(body, { headers: { "content-type": "text/plain" } });
  }
  if (url.pathname === "/cookie") {
    let body = "";
    const raw = req.headers.get("cookie") || "";
    for (const pair of raw.split(";")) {
      const trimmed = pair.trimStart();
      const eq = trimmed.indexOf("=");
      if (eq > 0) body += trimmed.substring(0, eq) + "=" + trimmed.substring(eq + 1) + "\n";
    }
    return new Response(body, { headers: { "content-type": "text/plain" } });
  }
  if (req.method === "POST") {
    const body = await req.text();
    return new Response(body, { headers: { "content-type": "text/plain" } });
  }
  return new Response("OK", { headers: { "content-type": "text/plain" } });
});
```

## Test Results

<div id="server-summary"><p><em>Loading results...</em></p></div>

### Compliance

<div id="results-compliance"></div>

### Smuggling

<div id="results-smuggling"></div>

### Malformed Input

<div id="results-malformedinput"></div>

### Caching

<div id="results-capabilities"></div>

### Cookies

<div id="results-cookies"></div>

<script src="/Http11Probe/probe/data.js"></script>
<script src="/Http11Probe/probe/render.js"></script>
<script>
(function() {
  if (!window.PROBE_DATA) {
    document.getElementById('server-summary').innerHTML = '<p><em>No probe data available yet. Run the Probe workflow on <code>main</code> to generate results.</em></p>';
    return;
  }
  ProbeRender.renderServerPage('Deno');
})();
</script>
