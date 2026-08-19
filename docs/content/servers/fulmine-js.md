---
title: "Fulmine.js"
description: "Fulmine.js (JavaScript) tested against RFC 9110/9112 for HTTP/1.1 compliance, request smuggling resistance, and malformed input handling."
toc: true
breadcrumbs: false
---

**Language:** JavaScript · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/FulmineServer)

A drop-in replacement for Express 5 that runs on `uWebSockets.js` instead of the Node.js HTTP module.
The package is installed under the name `express`, so this row and the
[Express](/servers/express.html) row probe the same application with only the framework behind it
swapped. The parser is the Node.js binding of the same C++ core the
[uWebSockets](/servers/uwebsockets-cpp.html) row builds, but a released one rather than `master`, so
where the two rows differ the binding is behind.

## Dockerfile

```dockerfile
FROM node:22-trixie-slim
# fulmine.js depends on uWebSockets.js, which npm installs from its git tag rather than from the
# registry, and whose prebuilt binary needs glibc 2.38, which is why this is trixie and not the
# bookworm -slim every other server here uses. git fetches it over https because npm asks for the
# repository over ssh by default and this image carries no key.
RUN apt-get update \
    && apt-get install -y --no-install-recommends git ca-certificates \
    && rm -rf /var/lib/apt/lists/* \
    && git config --global url."https://github.com/".insteadOf ssh://git@github.com/
WORKDIR /app
COPY src/Servers/FulmineServer/package.json .
# the version is pinned rather than ranged, since this repo does not keep lock files, and nothing
# here needs an install script to run
RUN npm install --omit=dev --ignore-scripts
# ExpressServer's application, not a copy of it. Fulmine.js is a drop-in replacement for Express 5,
# so the package is installed under the name "express" and the same file runs unchanged: what is
# probed here is the same application as the Express row, with only the framework behind it swapped.
COPY src/Servers/ExpressServer/server.js .
USER node
ENTRYPOINT ["node", "server.js", "8080"]
```

## Source

There is no `server.js` here. The image copies `src/Servers/ExpressServer/server.js` in and runs it
unchanged, which is the point of the row: the source is the one on the
[Express](/servers/express.html) page. All this directory carries is the dependency.

**`src/Servers/FulmineServer/package.json`**

```json
{
  "name": "fulmine-server",
  "private": true,
  "dependencies": {
    "express": "npm:fulmine.js@5.13.3"
  }
}
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
  ProbeRender.renderServerPage('Fulmine.js');
})();
</script>
