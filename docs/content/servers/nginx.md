---
title: "Nginx"
toc: true
breadcrumbs: false
---

**Language:** C · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/NginxServer)

## Dockerfile

```dockerfile
FROM nginx:1.27
COPY src/Servers/NginxServer/nginx.conf /etc/nginx/nginx.conf
COPY src/Servers/NginxServer/echo.js /etc/nginx/echo.js
```

## Source

**`nginx.conf`**

```nginx
load_module modules/ngx_http_js_module.so;

worker_processes 1;
pid /tmp/nginx.pid;
error_log /tmp/nginx_error.log;

events {
    worker_connections 64;
}

http {
    access_log /tmp/nginx_access.log;
    client_body_temp_path /tmp/nginx_client_body;
    proxy_temp_path       /tmp/nginx_proxy;
    fastcgi_temp_path     /tmp/nginx_fastcgi;
    uwsgi_temp_path       /tmp/nginx_uwsgi;
    scgi_temp_path        /tmp/nginx_scgi;

    js_import echo from /etc/nginx/echo.js;

    server {
        listen 8080;
        server_name localhost;

        location /echo {
            js_content echo.echo;
        }

        location /cookie {
            js_content echo.cookie;
        }

        location / {
            js_content echo.handler;
        }
    }
}
```

**`echo.js`**

```javascript
function echo(r) {
    var body = '';
    var headers = r.headersIn;
    for (var name in headers) {
        body += name + ': ' + headers[name] + '\n';
    }
    r.return(200, body);
}

function cookie(r) {
    var body = '';
    var raw = r.headersIn['Cookie'];
    if (raw) {
        var pairs = raw.split(';');
        for (var i = 0; i < pairs.length; i++) {
            var trimmed = pairs[i].replace(/^\s+/, '');
            var eq = trimmed.indexOf('=');
            if (eq > 0) {
                body += trimmed.substring(0, eq) + '=' + trimmed.substring(eq + 1) + '\n';
            }
        }
    }
    r.return(200, body);
}

function handler(r) {
    if (r.method === 'POST') {
        r.return(200, r.requestText || '');
    } else {
        r.return(200, 'OK');
    }
}

export default { echo, cookie, handler };
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
  ProbeRender.renderServerPage('Nginx');
})();
</script>
