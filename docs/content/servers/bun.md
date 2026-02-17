---
title: "Bun"
toc: true
breadcrumbs: false
---

**Language:** TypeScript · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/BunServer)

## Dockerfile

```dockerfile
FROM oven/bun:1-slim
WORKDIR /app
COPY src/Servers/BunServer/server.ts .
ENTRYPOINT ["bun", "run", "server.ts", "8080"]
```

## Source

```typescript
const port = parseInt(Bun.argv[2] || "8080", 10);

Bun.serve({
  port,
  hostname: "0.0.0.0",
  async fetch(req) {
    const url = new URL(req.url);
    if (url.pathname === "/echo") {
      let body = "";
      for (const [name, value] of req.headers) {
        body += name + ": " + value + "\n";
      }
      return new Response(body, { headers: { "Content-Type": "text/plain" } });
    }
    if (url.pathname === "/cookie") {
      let body = "";
      const raw = req.headers.get("cookie") || "";
      for (const pair of raw.split(";")) {
        const trimmed = pair.trimStart();
        const eq = trimmed.indexOf("=");
        if (eq > 0) body += trimmed.substring(0, eq) + "=" + trimmed.substring(eq + 1) + "\n";
      }
      return new Response(body, { headers: { "Content-Type": "text/plain" } });
    }
    if (req.method === "POST") {
      const body = await req.text();
      return new Response(body);
    }
    return new Response("OK");
  },
});

console.log(`Bun listening on 127.0.0.1:${port}`);
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
  ProbeRender.renderServerPage('Bun');
})();
</script>
