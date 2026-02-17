---
title: "Jetty"
toc: true
breadcrumbs: false
---

**Language:** Java · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/JettyServer)

## Dockerfile

```dockerfile
FROM maven:3.9-eclipse-temurin-21 AS build
WORKDIR /src
COPY src/Servers/JettyServer/pom.xml .
RUN mvn dependency:go-offline -q
COPY src/Servers/JettyServer/src/ src/
RUN mvn package -q -DskipTests

FROM eclipse-temurin:21-jre
WORKDIR /app
COPY --from=build /src/target/jetty-server-1.0.0.jar app.jar
ENTRYPOINT ["java", "-jar", "app.jar", "8080"]
```

## Source

```java
package server;

import java.nio.ByteBuffer;
import java.nio.charset.StandardCharsets;

import org.eclipse.jetty.http.HttpField;
import org.eclipse.jetty.server.Server;
import org.eclipse.jetty.server.ServerConnector;
import org.eclipse.jetty.server.Request;
import org.eclipse.jetty.server.Response;
import org.eclipse.jetty.server.Handler;
import org.eclipse.jetty.util.Callback;

public class Application extends Handler.Abstract {

    private static final ByteBuffer OK_BODY =
            ByteBuffer.wrap("OK".getBytes(StandardCharsets.UTF_8)).asReadOnlyBuffer();

    @Override
    public boolean handle(Request request, Response response, Callback callback) throws Exception {
        response.setStatus(200);
        response.getHeaders().put("Content-Type", "text/plain");

        if ("/cookie".equals(request.getHttpURI().getPath())) {
            StringBuilder sb = new StringBuilder();
            for (HttpField field : request.getHeaders()) {
                if ("Cookie".equalsIgnoreCase(field.getName())) {
                    for (String pair : field.getValue().split(";")) {
                        String trimmed = pair.stripLeading();
                        int eq = trimmed.indexOf('=');
                        if (eq > 0) {
                            sb.append(trimmed, 0, eq).append("=").append(trimmed.substring(eq + 1)).append("\n");
                        }
                    }
                }
            }
            byte[] cookieBody = sb.toString().getBytes(StandardCharsets.UTF_8);
            response.write(true, ByteBuffer.wrap(cookieBody), callback);
        } else if ("/echo".equals(request.getHttpURI().getPath())) {
            StringBuilder sb = new StringBuilder();
            for (HttpField field : request.getHeaders()) {
                sb.append(field.getName()).append(": ").append(field.getValue()).append("\n");
            }
            byte[] echoBody = sb.toString().getBytes(StandardCharsets.UTF_8);
            response.write(true, ByteBuffer.wrap(echoBody), callback);
        } else if ("POST".equals(request.getMethod())) {
            byte[] body = Request.asInputStream(request).readAllBytes();
            response.write(true, ByteBuffer.wrap(body), callback);
        } else {
            response.write(true, OK_BODY.slice(), callback);
        }
        return true;
    }

    public static void main(String[] args) throws Exception {
        int port = args.length > 0 ? Integer.parseInt(args[0]) : 9007;

        Server server = new Server();
        ServerConnector connector = new ServerConnector(server);
        connector.setHost("127.0.0.1");
        connector.setPort(port);
        server.addConnector(connector);
        server.setHandler(new Application());
        server.start();
        server.join();
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
  ProbeRender.renderServerPage('Jetty');
})();
</script>
