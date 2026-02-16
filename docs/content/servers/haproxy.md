---
title: "HAProxy"
toc: true
breadcrumbs: false
---

**Language:** C · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/HAProxyServer)

## Dockerfile

```dockerfile
FROM haproxy:3.0-alpine
COPY src/Servers/HAProxyServer/haproxy.cfg /usr/local/etc/haproxy/haproxy.cfg
COPY src/Servers/HAProxyServer/echo.lua /usr/local/etc/haproxy/echo.lua
```

## Source

**`haproxy.cfg`**

```text
global
    log stdout format raw local0
    lua-load /usr/local/etc/haproxy/echo.lua

defaults
    mode http
    timeout client 10s
    timeout connect 5s
    timeout server 10s

frontend http_in
    bind *:8080
    use_backend echo_backend if { path /echo }
    use_backend cookie_backend if { path /cookie }
    use_backend post_echo_backend if { method POST }
    http-request return status 200 content-type "text/plain" string "OK"

backend echo_backend
    http-request use-service lua.echo

backend cookie_backend
    http-request use-service lua.cookie

backend post_echo_backend
    http-request use-service lua.echo_body
```

**`echo.lua`**

```lua
core.register_service("echo", "http", function(applet)
    local body = ""
    local hdrs = applet.headers
    for name, values in pairs(hdrs) do
        for _, v in ipairs(values) do
            body = body .. name .. ": " .. v .. "\n"
        end
    end
    applet:set_status(200)
    applet:add_header("Content-Type", "text/plain")
    applet:add_header("Content-Length", tostring(#body))
    applet:start_response()
    applet:send(body)
end)

core.register_service("cookie", "http", function(applet)
    local body = ""
    local hdrs = applet.headers
    if hdrs["cookie"] then
        for _, raw in ipairs(hdrs["cookie"]) do
            for pair in raw:gmatch("[^;]+") do
                local trimmed = pair:match("^%s*(.*)")
                local eq = trimmed:find("=")
                if eq and eq > 1 then
                    body = body .. trimmed:sub(1, eq-1) .. "=" .. trimmed:sub(eq+1) .. "\n"
                end
            end
        end
    end
    applet:set_status(200)
    applet:add_header("Content-Type", "text/plain")
    applet:add_header("Content-Length", tostring(#body))
    applet:start_response()
    applet:send(body)
end)

core.register_service("echo_body", "http", function(applet)
    local body = applet:receive()
    if body == nil then body = "" end
    applet:set_status(200)
    applet:add_header("Content-Type", "text/plain")
    applet:add_header("Content-Length", tostring(#body))
    applet:start_response()
    applet:send(body)
end)
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
  ProbeRender.renderServerPage('HAProxy');
})();
</script>
