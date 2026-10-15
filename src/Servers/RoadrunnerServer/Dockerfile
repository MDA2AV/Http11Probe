FROM erlang:29.0-alpine AS build
WORKDIR /src
COPY src/Servers/RoadrunnerServer/rebar.config src/Servers/RoadrunnerServer/rebar.lock ./
COPY src/Servers/RoadrunnerServer/src ./src
RUN rebar3 as prod release

FROM alpine:3.23
RUN apk add --no-cache libstdc++ ncurses-libs openssl
COPY --from=build /src/_build/prod/rel/roadrunner_probe /app
ENTRYPOINT ["/app/bin/roadrunner_probe", "foreground"]
