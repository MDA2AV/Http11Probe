---
title: "H2O"
toc: true
breadcrumbs: false
---

**Language:** C · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/H2OServer)

## Dockerfile

```dockerfile
FROM ubuntu:24.04 AS build
RUN apt-get update && apt-get install -y cmake gcc g++ pkg-config libssl-dev zlib1g-dev git ruby bison && rm -rf /var/lib/apt/lists/*
RUN git clone --recurse-submodules --depth 1 https://github.com/h2o/h2o.git /src/h2o
WORKDIR /src/h2o/build
RUN cmake .. -DCMAKE_BUILD_TYPE=Release -DCMAKE_INSTALL_PREFIX=/usr/local -DWITH_MRUBY=ON && make -j$(nproc) && make install

FROM ubuntu:24.04
RUN apt-get update && apt-get install -y libssl3t64 && rm -rf /var/lib/apt/lists/*
COPY --from=build /usr/local/bin/h2o /usr/local/bin/
COPY --from=build /usr/local/share/h2o/ /usr/local/share/h2o/
COPY src/Servers/H2OServer/h2o.conf /etc/h2o/h2o.conf
RUN mkdir -p /var/www && echo "OK" > /var/www/index.html
ENTRYPOINT ["h2o", "-c", "/etc/h2o/h2o.conf"]
```

## Source

```yaml
listen: 8080
hosts:
  default:
    paths:
      /:
        mruby.handler: |
          proc {|env|
            if env["PATH_INFO"] == "/echo"
              body = ""
              env.each do |k, v|
                if k.start_with?("HTTP_")
                  name = k.sub("HTTP_", "").split("_").map(&:capitalize).join("-")
                  body += "#{name}: #{v}\n"
                end
              end
              body += "Content-Type: #{env['CONTENT_TYPE']}\n" if env['CONTENT_TYPE'] && !env['CONTENT_TYPE'].empty?
              body += "Content-Length: #{env['CONTENT_LENGTH']}\n" if env['CONTENT_LENGTH'] && !env['CONTENT_LENGTH'].empty?
              [200, {"content-type" => "text/plain"}, [body]]
            elsif env["PATH_INFO"] == "/cookie"
              body = ""
              if env["HTTP_COOKIE"]
                env["HTTP_COOKIE"].split(";").each do |pair|
                  trimmed = pair.lstrip
                  eq = trimmed.index("=")
                  if eq && eq > 0
                    body += "#{trimmed[0...eq]}=#{trimmed[(eq+1)..]}\n"
                  end
                end
              end
              [200, {"content-type" => "text/plain"}, [body]]
            elsif env["REQUEST_METHOD"] == "POST"
              body = env["rack.input"] ? env["rack.input"].read : ""
              [200, {"content-type" => "text/plain"}, [body]]
            else
              [200, {"content-type" => "text/plain"}, ["OK"]]
            end
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
  ProbeRender.renderServerPage('H2O');
})();
</script>
