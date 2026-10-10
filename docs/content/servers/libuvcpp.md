---
title: "libuvcpp"
description: "libuvcpp (C++) tested against RFC 9110/9112 for HTTP/1.1 compliance, request smuggling resistance, and malformed input handling."
toc: true
breadcrumbs: false
---

**Language:** C++ · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/LibuvcppServer)

A C++11 HTTP framework built on libuv's event loop, serving through `uvcpp_web_app` -- the library's high-level server, which owns routing, the HEAD-to-GET fallback and the automatic OPTIONS answer. Built against the pinned v1.6.0 linux-x64 release package, whose shared object already contains libuv, OpenSSL, nghttp2, ngtcp2 and zlib; the sha256 in the Dockerfile is what fixes which bytes get compiled.

## Dockerfile

```dockerfile
# libuvcpp entry for Http11Probe.
#
# The build downloads the official v1.6.0 linux-x64 release package and compiles
# server.cpp against it -- the same artifact a user gets from the GitHub release.
# That package's shared object already carries libuv, OpenSSL, nghttp2, ngtcp2
# and zlib, so the runtime image needs no HTTP library and no toolchain.
#
# The download is pinned by sha256 rather than by tag: a release tag can be moved
# and its assets re-uploaded, so the hash is what fixes which bytes get built.
# The transfer is forced over HTTPS, redirects included, rather than left to
# curl's default of following a redirect into whatever scheme it names.
#
# Both stages are ubuntu:24.04 (glibc 2.39; the release package's floor is
# 2.34), so the compiler that links the binary and the libc that runs it are the
# same ones. Alpine is not an option: the package is glibc-linked.

FROM ubuntu:24.04 AS build

ARG LIBUVCPP_VERSION=1.6.0
ARG LIBUVCPP_SHA256=b64ae68d121b49543e7a37126bbf38adf95a00ac51bff463b11be2dc499a138b

RUN apt-get update \
 && apt-get install -y --no-install-recommends \
        ca-certificates curl g++ unzip \
 && rm -rf /var/lib/apt/lists/*

WORKDIR /build
RUN curl -fsSL --proto '=https' --proto-redir '=https' --tlsv1.2 \
        --retry 3 --retry-connrefused -o libuvcpp.zip \
        "https://github.com/Antruly/libuvcpp/releases/download/v${LIBUVCPP_VERSION}/libuvcpp-${LIBUVCPP_VERSION}-linux-x64.zip" \
 && echo "${LIBUVCPP_SHA256}  libuvcpp.zip" | sha256sum -c - \
 && unzip -q libuvcpp.zip -d /opt \
 && rm -f libuvcpp.zip

COPY src/Servers/LibuvcppServer/server.cpp .
RUN g++ -std=c++11 -O2 -DNDEBUG -o /probe-server server.cpp \
        -I/opt/libuvcpp-1.6.0-linux-x64/include \
        -L/opt/libuvcpp-1.6.0-linux-x64/lib \
        -luvcpp -Wl,-rpath,/opt/libuvcpp-1.6.0-linux-x64/lib -pthread

FROM ubuntu:24.04

# g++ links the C++ runtime dynamically, so the runtime layer needs it.
RUN apt-get update \
 && apt-get install -y --no-install-recommends libstdc++6 \
 && rm -rf /var/lib/apt/lists/*

COPY --from=build /opt/libuvcpp-1.6.0-linux-x64/lib/libuvcpp.so \
                  /opt/libuvcpp-1.6.0-linux-x64/lib/libuvcpp.so
COPY --from=build /probe-server /usr/local/bin/probe-server

USER nobody
ENTRYPOINT ["probe-server", "8080"]
```

## Source — `server.cpp`

```cpp
// libuvcpp entry for Http11Probe.
//
// Three endpoints, per docs/content/add-a-framework: `/` (baseline, and body
// echo on POST), `/echo` (the headers the server received) and `/cookie` (the
// cookies the server parsed). Everything goes through `uvcpp_web_app`, the
// library's documented high-level server.

#include <webapp/uvcpp_web_app.h>
#include <webapp/uvcpp_web_util.h>

#include <cstdio>
#include <cstdlib>
#include <string>
#include <utility>
#include <vector>

using namespace uvcpp;

namespace {

typedef std::vector<std::pair<std::string, std::string> > cookie_list;

std::string cookies_to_lines(const cookie_list& cs) {
  std::string out;
  for (size_t i = 0; i < cs.size(); ++i) {
    out += cs[i].first;
    out += '=';
    out += cs[i].second;
    out += '\n';
  }
  return out;
}

// The baseline endpoint: 200 for GET, and the request body echoed back for
// POST.
void root_get(uvcpp_web_request&, uvcpp_web_response& resp, uvcpp_web_next) {
  resp.text("OK");
  resp.end();
}

void root_post(uvcpp_web_request& req, uvcpp_web_response& resp, uvcpp_web_next) {
  const char* p = req.body_data();
  if (p == nullptr) {
    resp.text(std::string());
  } else {
    resp.body(p, req.body_size(), "text/plain");
  }
  resp.end();
}

// One "name: value" line per header, in the order the parser recorded them.
void echo(uvcpp_web_request& req, uvcpp_web_response& resp, uvcpp_web_next) {
  std::string out;
  const http_headers& hs = req.headers();
  for (size_t i = 0; i < hs.size(); ++i) {
    out += hs[i].name;
    out += ": ";
    out += hs[i].value;
    out += '\n';
  }
  resp.text(out);
  resp.end();
}

// What the library's own cookie parser made of the Cookie header.
void cookie(uvcpp_web_request& req, uvcpp_web_response& resp, uvcpp_web_next) {
  resp.text(cookies_to_lines(web_parse_cookies(req.header("cookie"))));
  resp.end();
}

}  // namespace

int main(int argc, char** argv) {
  const int port = argc > 1 ? std::atoi(argv[1]) : 8080;

  uvcpp_web_app app;

  // `/*path` is the catch-all; the framework's own HEAD-to-GET fallback and
  // automatic OPTIONS answer the other methods, so neither is registered here.
  app.get("/", root_get);
  app.post("/", root_post);
  app.get("/echo", echo);
  app.post("/echo", echo);
  app.get("/cookie", cookie);
  app.post("/cookie", cookie);
  app.get("/*path", root_get);
  app.post("/*path", root_post);

  // Everything else is the framework's default: 0.0.0.0, port 8080 unless
  // overridden, and the documented HEAD/OPTIONS behaviour above.
  app.set_port(port);

  const int rc = app.start_background();
  if (rc != 0) {
    std::fprintf(stderr, "listen on %d failed: %d\n", port, rc);
    return 1;
  }
  app.join();
  return 0;
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
  ProbeRender.renderServerPage('libuvcpp');
})();
</script>
