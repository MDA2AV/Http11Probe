---
title: "Apache"
toc: true
breadcrumbs: false
---

**Language:** C · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/ApacheServer)

## Dockerfile

```dockerfile
FROM httpd:2.4

COPY src/Servers/ApacheServer/httpd-probe.conf /usr/local/apache2/conf/httpd.conf
RUN echo "OK" > /usr/local/apache2/htdocs/index.html
COPY src/Servers/ApacheServer/echo.cgi /usr/local/apache2/cgi-bin/echo.cgi
COPY src/Servers/ApacheServer/cookie.cgi /usr/local/apache2/cgi-bin/cookie.cgi
RUN chmod +x /usr/local/apache2/cgi-bin/echo.cgi /usr/local/apache2/cgi-bin/cookie.cgi
```

## Source

**`httpd-probe.conf`**

```apache
ServerRoot "/usr/local/apache2"
Listen 8080

LoadModule mpm_event_module modules/mod_mpm_event.so
LoadModule dir_module modules/mod_dir.so
LoadModule unixd_module modules/mod_unixd.so
LoadModule authz_core_module modules/mod_authz_core.so
LoadModule cgi_module modules/mod_cgi.so
LoadModule alias_module modules/mod_alias.so

ErrorLog /proc/self/fd/2
LogLevel warn

DocumentRoot "/usr/local/apache2/htdocs"

<Directory "/usr/local/apache2/htdocs">
    Require all granted
</Directory>

ScriptAlias /echo /usr/local/apache2/cgi-bin/echo.cgi
ScriptAlias /cookie /usr/local/apache2/cgi-bin/cookie.cgi

<Directory "/usr/local/apache2/cgi-bin">
    Require all granted
</Directory>
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
  ProbeRender.renderServerPage('Apache');
})();
</script>
