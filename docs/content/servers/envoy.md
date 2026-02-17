---
title: "Envoy"
toc: true
breadcrumbs: false
---

**Language:** C++ · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/EnvoyServer)

## Dockerfile

```dockerfile
FROM envoyproxy/envoy:v1.32-latest
COPY src/Servers/EnvoyServer/envoy.yaml /etc/envoy/envoy.yaml
```

## Source

```yaml
static_resources:
  listeners:
    - name: listener_0
      address:
        socket_address:
          address: 0.0.0.0
          port_value: 8080
      filter_chains:
        - filters:
            - name: envoy.filters.network.http_connection_manager
              typed_config:
                "@type": type.googleapis.com/envoy.extensions.filters.network.http_connection_manager.v3.HttpConnectionManager
                stat_prefix: ingress_http
                http_filters:
                  - name: envoy.filters.http.lua
                    typed_config:
                      "@type": type.googleapis.com/envoy.extensions.filters.http.lua.v3.Lua
                      default_source_code:
                        inline_string: |
                          function envoy_on_request(request_handle)
                            local path = request_handle:headers():get(":path")
                            if path == "/echo" then
                              local body = ""
                              local headers = request_handle:headers()
                              for key, value in pairs(headers) do
                                if key:sub(1,1) ~= ":" then
                                  body = body .. key .. ": " .. value .. "\n"
                                end
                              end
                              request_handle:respond({[":status"] = "200", ["content-type"] = "text/plain"}, body)
                            elseif path == "/cookie" then
                              local body = ""
                              local raw = request_handle:headers():get("cookie")
                              if raw then
                                for pair in raw:gmatch("[^;]+") do
                                  local trimmed = pair:match("^%s*(.*)")
                                  local eq = trimmed:find("=")
                                  if eq and eq > 1 then
                                    body = body .. trimmed:sub(1, eq-1) .. "=" .. trimmed:sub(eq+1) .. "\n"
                                  end
                                end
                              end
                              request_handle:respond({[":status"] = "200", ["content-type"] = "text/plain"}, body)
                            end
                          end
                  - name: envoy.filters.http.router
                    typed_config:
                      "@type": type.googleapis.com/envoy.extensions.filters.http.router.v3.Router
                route_config:
                  virtual_hosts:
                    - name: local_service
                      domains: ["*"]
                      routes:
                        - match:
                            prefix: "/"
                          direct_response:
                            status: 200
                            body:
                              inline_string: "OK"
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
  ProbeRender.renderServerPage('Envoy');
})();
</script>
