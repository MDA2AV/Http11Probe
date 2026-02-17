---
title: "Quarkus"
toc: true
breadcrumbs: false
---

**Language:** Java · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/QuarkusServer)

## Dockerfile

```dockerfile
FROM maven:3.9-eclipse-temurin-21 AS build
WORKDIR /src
COPY src/Servers/QuarkusServer/pom.xml .
RUN mvn dependency:go-offline -q
COPY src/Servers/QuarkusServer/src/ src/
RUN mvn package -q -DskipTests

FROM eclipse-temurin:21-jre
WORKDIR /app
COPY --from=build /src/target/quarkus-app/ quarkus-app/
ENTRYPOINT ["java", "-Dquarkus.http.port=8080", "-jar", "quarkus-app/quarkus-run.jar"]
```

## Source

```java
package server;

import java.io.InputStream;
import java.io.IOException;
import java.util.List;
import java.util.Map;

import jakarta.ws.rs.GET;
import jakarta.ws.rs.POST;
import jakarta.ws.rs.Path;
import jakarta.ws.rs.Produces;
import jakarta.ws.rs.core.Context;
import jakarta.ws.rs.core.HttpHeaders;
import jakarta.ws.rs.core.MediaType;
import jakarta.ws.rs.core.Response;

@Path("/")
public class Application {

    @GET
    @Path("{path:.*}")
    @Produces(MediaType.TEXT_PLAIN)
    public String catchAll() {
        return "OK";
    }

    @POST
    @Path("{path:.*}")
    @Produces(MediaType.TEXT_PLAIN)
    public byte[] catchAllPost(InputStream body) throws IOException {
        return body.readAllBytes();
    }

    @GET
    @Path("/cookie")
    @Produces(MediaType.TEXT_PLAIN)
    public Response cookieGet(@Context HttpHeaders headers) {
        return parseCookies(headers);
    }

    @POST
    @Path("/cookie")
    @Produces(MediaType.TEXT_PLAIN)
    public Response cookiePost(@Context HttpHeaders headers) {
        return parseCookies(headers);
    }

    @GET
    @Path("/echo")
    @Produces(MediaType.TEXT_PLAIN)
    public Response echoGet(@Context HttpHeaders headers) {
        return echoHeaders(headers);
    }

    @POST
    @Path("/echo")
    @Produces(MediaType.TEXT_PLAIN)
    public Response echoPost(@Context HttpHeaders headers) {
        return echoHeaders(headers);
    }

    private Response parseCookies(HttpHeaders headers) {
        StringBuilder sb = new StringBuilder();
        List<String> cookieHeaders = headers.getRequestHeader("Cookie");
        if (cookieHeaders != null) {
            for (String raw : cookieHeaders) {
                for (String pair : raw.split(";")) {
                    String trimmed = pair.stripLeading();
                    int eq = trimmed.indexOf('=');
                    if (eq > 0) {
                        sb.append(trimmed, 0, eq).append("=").append(trimmed.substring(eq + 1)).append("\n");
                    }
                }
            }
        }
        return Response.ok(sb.toString(), MediaType.TEXT_PLAIN).build();
    }

    private Response echoHeaders(HttpHeaders headers) {
        StringBuilder sb = new StringBuilder();
        for (Map.Entry<String, List<String>> entry : headers.getRequestHeaders().entrySet()) {
            for (String value : entry.getValue()) {
                sb.append(entry.getKey()).append(": ").append(value).append("\n");
            }
        }
        return Response.ok(sb.toString(), MediaType.TEXT_PLAIN).build();
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

<script src="/Http11Probe/probe/data.js"></script>
<script src="/Http11Probe/probe/render.js"></script>
<script>
(function() {
  if (!window.PROBE_DATA) {
    document.getElementById('server-summary').innerHTML = '<p><em>No probe data available yet. Run the Probe workflow on <code>main</code> to generate results.</em></p>';
    return;
  }
  ProbeRender.renderServerPage('Quarkus');
})();
</script>
