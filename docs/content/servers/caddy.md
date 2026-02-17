---
title: "Caddy"
toc: true
breadcrumbs: false
---

**Language:** Go · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/CaddyServer)

## Dockerfile

```dockerfile
FROM caddy:2
COPY src/Servers/CaddyServer/Caddyfile /etc/caddy/Caddyfile
COPY src/Servers/CaddyServer/echo.html /srv/echo.html
COPY src/Servers/CaddyServer/cookie.html /srv/cookie.html
```

## Source

**`Caddyfile`**

```text
:8080 {
    request_body {
        max_size 1MB
    }

    @post_root {
        method POST
        path /
    }
    handle @post_root {
        respond "{http.request.body}" 200
    }

    handle /echo {
        root * /srv
        templates {
            mime text/plain
        }
        rewrite * /echo.html
        file_server
    }

    handle /cookie {
        root * /srv
        templates {
            mime text/plain
        }
        rewrite * /cookie.html
        file_server
    }

    respond "OK" 200
}
```

**`echo.html`**

```html
{{range $key, $vals := .Req.Header}}{{range $vals}}{{$key}}: {{.}}
{{end}}{{end}}
```

**`cookie.html`**

```html
{{range .Req.Header.Cookie}}{{range splitList ";" .}}{{trim .}}
{{end}}{{end}}
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
  ProbeRender.renderServerPage('Caddy');
})();
</script>
