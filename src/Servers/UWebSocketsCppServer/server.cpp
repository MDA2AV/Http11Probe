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
            if (req->getMethod() != "head") {
                res->end("OK");
            } else {
                res->endWithoutBody();
            }
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
