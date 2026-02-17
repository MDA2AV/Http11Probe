---
title: "FastHTTP"
toc: true
breadcrumbs: false
---

**Language:** Go · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/FastHttpServer)

## Dockerfile

```dockerfile
FROM golang:1.23-alpine AS build
WORKDIR /src
COPY src/Servers/FastHttpServer/go.mod .
COPY src/Servers/FastHttpServer/main.go .
RUN go mod tidy && CGO_ENABLED=0 go build -o /fasthttp-server .

FROM alpine:3.20
COPY --from=build /fasthttp-server /usr/local/bin/
ENTRYPOINT ["fasthttp-server", "8080"]
```

## Source

```go
package main

import (
	"os"
	"strings"

	"github.com/valyala/fasthttp"
)

func main() {
	port := "8080"
	if len(os.Args) > 1 {
		port = os.Args[1]
	}

	handler := func(ctx *fasthttp.RequestCtx) {
		ctx.SetStatusCode(200)
		switch string(ctx.Path()) {
		case "/echo":
			ctx.SetContentType("text/plain")
			ctx.Request.Header.VisitAll(func(key, value []byte) {
				ctx.WriteString(string(key) + ": " + string(value) + "\n")
			})
		case "/cookie":
			ctx.SetContentType("text/plain")
			raw := string(ctx.Request.Header.Peek("Cookie"))
			for _, pair := range strings.Split(raw, ";") {
				pair = strings.TrimLeft(pair, " ")
				if eq := strings.Index(pair, "="); eq > 0 {
					ctx.WriteString(pair[:eq] + "=" + pair[eq+1:] + "\n")
				}
			}
		default:
			if string(ctx.Method()) == "POST" {
				ctx.SetBody(ctx.Request.Body())
				return
			}
			ctx.SetBodyString("OK")
		}
	}

	fasthttp.ListenAndServe("0.0.0.0:"+port, handler)
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

<script src="/Http11Probe/probe/data.js"></script>
<script src="/Http11Probe/probe/render.js"></script>
<script>
(function() {
  if (!window.PROBE_DATA) {
    document.getElementById('server-summary').innerHTML = '<p><em>No probe data available yet. Run the Probe workflow on <code>main</code> to generate results.</em></p>';
    return;
  }
  ProbeRender.renderServerPage('FastHTTP');
})();
</script>
