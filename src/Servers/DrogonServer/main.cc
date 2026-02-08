#include <drogon/drogon.h>
#include <cstdlib>

int main(int argc, char *argv[]) {
    int port = 8080;
    if (argc > 1) port = std::atoi(argv[1]);

    drogon::app()
        .registerHandlerViaRegex(
            "/.*",
            [](const drogon::HttpRequestPtr &req,
               std::function<void(const drogon::HttpResponsePtr &)> &&callback) {
                auto resp = drogon::HttpResponse::newHttpResponse();
                resp->setStatusCode(drogon::k200OK);
                resp->setContentTypeCode(drogon::CT_TEXT_PLAIN);
                resp->setBody("OK");
                callback(resp);
            })
        .addListener("0.0.0.0", port)
        .setThreadNum(1)
        .run();

    return 0;
}
