---
title: "Traefik"
toc: true
breadcrumbs: false
---

**Language:** Go · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/TraefikServer)

## Dockerfile

```dockerfile
FROM alpine:3.20 AS plugin
RUN apk add --no-cache git
RUN git clone https://github.com/jdel/staticresponse.git /plugin

FROM golang:1.23-alpine AS echo-build
WORKDIR /build
COPY src/Servers/TraefikServer/echo/main.go main.go
RUN go build -o /echo-server main.go

FROM traefik:v3.2
COPY --from=plugin /plugin /plugins-local/src/github.com/jdel/staticresponse/
COPY --from=echo-build /echo-server /usr/local/bin/echo-server
COPY src/Servers/TraefikServer/traefik.yml /etc/traefik/traefik.yml
COPY src/Servers/TraefikServer/dynamic.yml /etc/traefik/dynamic.yml
COPY src/Servers/TraefikServer/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh
ENTRYPOINT ["/entrypoint.sh"]
```

## Source

**`traefik.yml`**

```yaml
entryPoints:
  web:
    address: ":8080"

providers:
  file:
    filename: /etc/traefik/dynamic.yml

experimental:
  localPlugins:
    staticresponse:
      moduleName: github.com/jdel/staticresponse
```

**`dynamic.yml`**

```yaml
http:
  routers:
    echo:
      rule: "Path(`/echo`)"
      entryPoints:
        - web
      service: echo-svc

    cookie:
      rule: "Path(`/cookie`)"
      entryPoints:
        - web
      service: echo-svc

    catchall:
      rule: "PathPrefix(`/`)"
      entryPoints:
        - web
      middlewares:
        - static-ok
      service: noop@internal

  services:
    echo-svc:
      loadBalancer:
        servers:
          - url: "http://127.0.0.1:9090"

  middlewares:
    static-ok:
      plugin:
        staticresponse:
          statusCode: 200
          body: "OK"
```

**`echo/main.go`**

```go
package main

import (
	"io"
	"net/http"
	"strings"
)

func main() {
	http.HandleFunc("/cookie", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "text/plain")
		raw := r.Header.Get("Cookie")
		for _, pair := range strings.Split(raw, ";") {
			pair = strings.TrimLeft(pair, " ")
			if eq := strings.Index(pair, "="); eq > 0 {
				w.Write([]byte(pair[:eq] + "=" + pair[eq+1:] + "\n"))
			}
		}
	})

	http.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "Method Not Allowed", http.StatusMethodNotAllowed)
			return
		}

		body, err := io.ReadAll(r.Body)
		if err != nil {
			http.Error(w, "Failed to read body", http.StatusBadRequest)
			return
		}
		defer r.Body.Close()

		w.Header().Set("Content-Type", "text/plain")
		w.WriteHeader(http.StatusOK)
		w.Write(body)
	})

	http.ListenAndServe(":9090", nil)
}
```

**`entrypoint.sh`**

```bash
#!/bin/sh
/usr/local/bin/echo-server &
exec traefik "$@"
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
  ProbeRender.renderServerPage('Traefik');
})();
</script>
