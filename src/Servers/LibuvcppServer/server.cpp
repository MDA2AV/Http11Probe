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
