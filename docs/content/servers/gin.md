---
title: "Gin"
toc: true
breadcrumbs: false
---

**Language:** Go · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/GinServer)

## Dockerfile

```dockerfile
FROM golang:1.23-alpine AS build
WORKDIR /src
COPY src/Servers/GinServer/go.mod .
COPY src/Servers/GinServer/main.go .
RUN go mod tidy && CGO_ENABLED=0 go build -o /gin-server .

FROM alpine:3.20
COPY --from=build /gin-server /usr/local/bin/
ENTRYPOINT ["gin-server", "8080"]
```

## Source

```go
package main

import (
	"io"
	"os"
	"strings"

	"github.com/gin-gonic/gin"
)

func main() {
	port := "8080"
	if len(os.Args) > 1 {
		port = os.Args[1]
	}

	gin.SetMode(gin.ReleaseMode)
	r := gin.New()
	r.Any("/cookie", func(c *gin.Context) {
		var sb strings.Builder
		raw := c.GetHeader("Cookie")
		for _, pair := range strings.Split(raw, ";") {
			pair = strings.TrimLeft(pair, " ")
			if eq := strings.Index(pair, "="); eq > 0 {
				sb.WriteString(pair[:eq] + "=" + pair[eq+1:] + "\n")
			}
		}
		c.Data(200, "text/plain", []byte(sb.String()))
	})
	r.Any("/echo", func(c *gin.Context) {
		var sb strings.Builder
		for name, values := range c.Request.Header {
			for _, v := range values {
				sb.WriteString(name + ": " + v + "\n")
			}
		}
		c.Data(200, "text/plain", []byte(sb.String()))
	})
	r.NoRoute(func(c *gin.Context) {
		if c.Request.Method == "POST" {
			body, _ := io.ReadAll(c.Request.Body)
			c.Data(200, "text/plain", body)
			return
		}
		c.String(200, "OK")
	})
	r.Run("0.0.0.0:" + port)
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
  ProbeRender.renderServerPage('Gin');
})();
</script>
