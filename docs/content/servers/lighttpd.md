---
title: "Lighttpd"
toc: true
breadcrumbs: false
---

**Language:** C · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/LighttpdServer)

## Dockerfile

```dockerfile
FROM alpine:3.20
RUN apk add --no-cache lighttpd
COPY src/Servers/LighttpdServer/lighttpd.conf /etc/lighttpd/lighttpd.conf
COPY src/Servers/LighttpdServer/index.cgi /var/www/index.cgi
COPY src/Servers/LighttpdServer/echo.cgi /var/www/echo.cgi
COPY src/Servers/LighttpdServer/cookie.cgi /var/www/cookie.cgi
RUN chmod +x /var/www/index.cgi /var/www/echo.cgi /var/www/cookie.cgi
EXPOSE 8080
CMD ["lighttpd", "-D", "-f", "/etc/lighttpd/lighttpd.conf"]
```

## Source

**`lighttpd.conf`**

```text
server.document-root = "/var/www"
server.port = 8080
index-file.names = ("index.cgi")
server.modules += ("mod_cgi", "mod_alias")
cgi.assign = (".cgi" => "")
server.error-handler = "/index.cgi"
alias.url = ("/echo" => "/var/www/echo.cgi", "/cookie" => "/var/www/cookie.cgi")
```

**`index.cgi`**

```bash
#!/bin/sh
printf 'Content-Type: text/plain\r\n\r\n'
if [ "$REQUEST_METHOD" = "POST" ] && [ "${CONTENT_LENGTH:-0}" -gt 0 ] 2>/dev/null; then
    head -c "$CONTENT_LENGTH"
else
    printf 'OK'
fi
```

**`echo.cgi`**

```bash
#!/bin/sh
printf 'Content-Type: text/plain\r\n\r\n'
env | grep '^HTTP_' | while IFS='=' read -r key value; do
    name=$(echo "$key" | sed 's/^HTTP_//;s/_/-/g')
    printf '%s: %s\n' "$name" "$value"
done
if [ -n "$CONTENT_TYPE" ]; then
    printf 'Content-Type: %s\n' "$CONTENT_TYPE"
fi
if [ -n "$CONTENT_LENGTH" ]; then
    printf 'Content-Length: %s\n' "$CONTENT_LENGTH"
fi
```

**`cookie.cgi`**

```bash
#!/bin/sh
printf 'Content-Type: text/plain\r\n\r\n'
if [ -n "$HTTP_COOKIE" ]; then
    echo "$HTTP_COOKIE" | tr ';' '\n' | while read -r pair; do
        trimmed=$(echo "$pair" | sed 's/^ *//')
        printf '%s\n' "$trimmed"
    done
fi
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
  ProbeRender.renderServerPage('Lighttpd');
})();
</script>
