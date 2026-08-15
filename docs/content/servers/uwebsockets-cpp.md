---
title: "uWebSockets"
description: "uWebSockets (C++) tested against RFC 9110/9112 for HTTP/1.1 compliance, request smuggling resistance, and malformed input handling."
toc: true
breadcrumbs: false
---

**Language:** C++ · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/UWebSocketsCppServer)

The C++ library, built from `master` rather than a pinned release so upstream fixes show up in these results as they land. `uWebSockets.js` is the Node.js binding over this same core.

## Dockerfile

```dockerfile
FROM debian:trixie-slim AS build
RUN apt-get update && apt-get install -y --no-install-recommends \
        ca-certificates g++ git make zlib1g-dev \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /src
# Deliberately unpinned: upstream asked that results track master so their
# fixes show up here. Rebuilding picks up whatever master is at that moment.
RUN git clone --depth 1 https://github.com/uNetworking/uWebSockets.git . \
    && git submodule update --init --depth 1 uSockets
RUN make -C uSockets
COPY src/Servers/UWebSocketsCppServer/server.cpp .
RUN g++ -std=c++2b -O3 -Isrc -IuSockets/src server.cpp uSockets/*.o -lz -o /uws-server

FROM debian:trixie-slim
RUN apt-get update && apt-get install -y --no-install-recommends zlib1g \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /uws-server /usr/local/bin/uws-server
USER nobody
ENTRYPOINT ["uws-server", "8080"]
```

## Source — `server.cpp`

```cpp
#include "App.h"

#include <charconv>
#include <cstdlib>
#include <iostream>
#include <memory>
#include <string>
#include <string_view>

/* uWS invalidates req as soon as the handler returns, so everything needed
 * later is read out synchronously. Only the body echo is async. */

static std::string parseCookies(std::string_view raw) {
    std::string out;
    size_t pos = 0;
    while (pos < raw.size()) {
        size_t semi = raw.find(';', pos);
        std::string_view pair = raw.substr(pos, semi == std::string_view::npos ? raw.size() - pos : semi - pos);
        size_t start = pair.find_first_not_of(" \t");
        if (start != std::string_view::npos) {
            pair.remove_prefix(start);
            size_t eq = pair.find('=');
            if (eq != std::string_view::npos && eq > 0) {
                out.append(pair.substr(0, eq));
                out.push_back('=');
                out.append(pair.substr(eq + 1));
                out.push_back('\n');
            }
        }
        if (semi == std::string_view::npos) {
            break;
        }
        pos = semi + 1;
    }
    return out;
}

int main(int argc, char **argv) {
    int port = 8080;
    if (argc > 1) {
        std::string_view arg(argv[1]);
        std::from_chars(arg.data(), arg.data() + arg.size(), port);
    }

    uWS::App().any("/cookie", [](auto *res, auto *req) {
        std::string body = parseCookies(req->getHeader("cookie"));
        res->writeHeader("Content-Type", "text/plain");
        res->end(body);
    }).any("/echo", [](auto *res, auto *req) {
        std::string body;
        for (auto [key, value] : *req) {
            body.append(key);
            body.append(": ");
            body.append(value);
            body.push_back('\n');
        }
        res->writeHeader("Content-Type", "text/plain");
        res->end(body);
    /* Wildcards must be registered last. */
    }).any("/*", [](auto *res, auto *req) {
        if (req->getMethod() != "post") {
            res->writeHeader("Content-Type", "text/plain");
            res->end("OK");
            return;
        }

        /* Already inside the socket callback, so uWS corks these writes for us. */
        auto buffer = std::make_shared<std::string>();
        res->onData([res, buffer](std::string_view chunk, bool isFin) {
            buffer->append(chunk);
            if (isFin) {
                res->writeHeader("Content-Type", "text/plain");
                res->end(*buffer);
            }
        });
        res->onAborted([]() {});
    }).listen("0.0.0.0", port, [port](auto *listen_socket) {
        if (!listen_socket) {
            std::cerr << "Failed to listen on port " << port << std::endl;
            std::exit(1);
        }
    }).run();
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

<script src="/probe/data.js"></script>
<script src="/probe/render.js"></script>
<script>
(function() {
  if (!window.PROBE_DATA) {
    document.getElementById('server-summary').innerHTML = '<p><em>No probe data available yet. Run the Probe workflow on <code>main</code> to generate results.</em></p>';
    return;
  }
  ProbeRender.renderServerPage('uWebSockets');
})();
</script>
