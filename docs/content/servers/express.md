---
title: "Express"
toc: true
breadcrumbs: false
---

**Language:** JavaScript · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/ExpressServer)

## Dockerfile

```dockerfile
FROM node:22-slim
WORKDIR /app
COPY src/Servers/ExpressServer/package.json .
RUN npm install --omit=dev
COPY src/Servers/ExpressServer/server.js .
ENTRYPOINT ["node", "server.js", "8080"]
```

## Source

```javascript
const express = require("express");

const app = express();
const port = parseInt(process.argv[2] || "9003", 10);

app.get("/", (_req, res) => {
  res.send("OK");
});

app.post("/", (req, res) => {
  const chunks = [];
  req.on("data", (chunk) => chunks.push(chunk));
  req.on("end", () => res.send(Buffer.concat(chunks)));
});

app.all('/cookie', (req, res) => {
  let body = '';
  const raw = req.headers.cookie || '';
  for (const pair of raw.split(';')) {
    const trimmed = pair.trimStart();
    const eq = trimmed.indexOf('=');
    if (eq > 0) body += trimmed.substring(0, eq) + '=' + trimmed.substring(eq + 1) + '\n';
  }
  res.set('Content-Type', 'text/plain').send(body);
});

app.all('/echo', (req, res) => {
  let body = '';
  for (const [name, value] of Object.entries(req.headers)) {
    if (Array.isArray(value)) value.forEach(v => body += name + ': ' + v + '\n');
    else body += name + ': ' + value + '\n';
  }
  res.set('Content-Type', 'text/plain').send(body);
});

app.listen(port, "127.0.0.1", () => {
  console.log(`Express listening on 127.0.0.1:${port}`);
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
  ProbeRender.renderServerPage('Express');
})();
</script>
