#include "lithium_http_server.hh"
#include <cstdlib>

int main(int argc, char *argv[]) {
    int port = 8080;
    if (argc > 1) port = std::atoi(argv[1]);

    li::http_api api;

    api.get("/") = [&](li::http_request &request, li::http_response &response) {
        response.write("OK");
    };

    api.get("/{{path}}") = [&](li::http_request &request, li::http_response &response) {
        response.write("OK");
    };

    li::http_serve(api, port);

    return 0;
}
