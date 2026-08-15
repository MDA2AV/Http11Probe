---
title: "uWebSockets.js"
description: "uWebSockets.js (JavaScript) tested against RFC 9110/9112 for HTTP/1.1 compliance, request smuggling resistance, and malformed input handling."
toc: true
breadcrumbs: false
---

**Language:** JavaScript · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/UWebSocketsServer)

## Dockerfile

```dockerfile
# trixie, not the default bookworm: uWS ships prebuilt binaries needing glibc >= 2.38
FROM node:22-trixie-slim
WORKDIR /app
COPY src/Servers/UWebSocketsServer/package.json src/Servers/UWebSocketsServer/package-lock.json ./
RUN npm ci --omit=dev --ignore-scripts
COPY src/Servers/UWebSocketsServer/server.js .
USER node
ENTRYPOINT ["node", "server.js", "8080"]
```

## Source — `package.json`

uWebSockets.js is not published to the npm registry, so it is pinned to a release tarball. Release tags carry prebuilt `.node` binaries, so there is no build step and no lifecycle scripts to run. A generated `package-lock.json` sits alongside this file, pinning the tarball by sha512 integrity hash so `npm ci` gets identical bytes on every build.

```json
{
  "name": "uwebsockets-server",
  "private": true,
  "dependencies": {
    "uWebSockets.js": "https://github.com/uNetworking/uWebSockets.js/archive/refs/tags/v20.69.0.tar.gz"
  }
}
```

## Source — `server.js`

```javascript
const uWS = require('uWebSockets.js');

const port = Number.parseInt(process.argv[2] || '8080', 10);

/* uWS invalidates `req` the moment the handler returns, so everything needed
 * later has to be read out synchronously. Only the body echo is async here. */

function readBody(res, onDone) {
    const chunks = [];
    res.onAborted(() => { res.aborted = true; });
    res.onData((ab, isLast) => {
        /* The ArrayBuffer is only valid inside this callback — slice(0) copies it. */
        chunks.push(Buffer.from(ab.slice(0)));
        if (isLast) onDone(Buffer.concat(chunks));
    });
}

const app = uWS.App();

app.any('/cookie', (res, req) => {
    let body = '';
    const raw = req.getHeader('cookie');
    for (const pair of raw.split(';')) {
        const trimmed = pair.trimStart();
        const eq = trimmed.indexOf('=');
        if (eq > 0) body += trimmed.substring(0, eq) + '=' + trimmed.substring(eq + 1) + '\n';
    }
    res.writeHeader('Content-Type', 'text/plain');
    res.end(body);
});

app.any('/echo', (res, req) => {
    let body = '';
    req.forEach((name, value) => { body += name + ': ' + value + '\n'; });
    res.writeHeader('Content-Type', 'text/plain');
    res.end(body);
});

/* Wildcards must be registered last. */
app.any('/*', (res, req) => {
    if (req.getMethod() === 'post') {
        readBody(res, (body) => {
            if (res.aborted) return;
            /* Cork when responding from an async callback. */
            res.cork(() => {
                res.writeHeader('Content-Type', 'text/plain');
                res.end(body);
            });
        });
        return;
    }
    res.writeHeader('Content-Type', 'text/plain');
    res.end('OK');
});

app.listen('0.0.0.0', port, (token) => {
    if (!token) {
        console.error('Failed to listen on port ' + port);
        process.exit(1);
    }
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

<script src="/probe/data.js"></script>
<script src="/probe/render.js"></script>
<script>
(function() {
  if (!window.PROBE_DATA) {
    document.getElementById('server-summary').innerHTML = '<p><em>No probe data available yet. Run the Probe workflow on <code>main</code> to generate results.</em></p>';
    return;
  }
  ProbeRender.renderServerPage('uWebSockets.js');
})();
</script>
