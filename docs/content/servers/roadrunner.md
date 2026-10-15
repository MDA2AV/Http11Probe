---
title: "Roadrunner"
description: "Roadrunner (Erlang) tested against RFC 9110/9112 for HTTP/1.1 compliance, request smuggling resistance, and malformed input handling."
toc: true
breadcrumbs: false
---

**Language:** Erlang · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/RoadrunnerServer)

[Roadrunner](https://github.com/arizona-framework/roadrunner) is a pure-Erlang HTTP/1.1, HTTP/2, HTTP/3 and WebSocket server from the Arizona framework. This entry runs roadrunner 0.10.1 from Hex as an OTP release: one HTTP/1.1 listener on port 8080 with roadrunner's default options, and a handler that serves `/echo`, `/cookie`, and a catch-all for every other path. HEAD needs no route of its own, since roadrunner answers it like GET without the body.

## Dockerfile

```dockerfile
FROM erlang:29.0-alpine AS build
WORKDIR /src
COPY src/Servers/RoadrunnerServer/rebar.config src/Servers/RoadrunnerServer/rebar.lock ./
COPY src/Servers/RoadrunnerServer/src ./src
RUN rebar3 as prod release

FROM alpine:3.23
RUN apk add --no-cache libstdc++ ncurses-libs openssl
COPY --from=build /src/_build/prod/rel/roadrunner_probe /app
USER nobody
ENTRYPOINT ["/app/bin/roadrunner_probe", "foreground"]
```

## Source

**`rebar.config`**

```erlang
{erl_opts, [debug_info]}.

{deps, [
    {roadrunner, "0.10.1"}
]}.

{relx, [
    {release, {roadrunner_probe, "0.1.0"}, [roadrunner_probe]},
    {dev_mode, false},
    {include_erts, true},
    {extended_start_script, true}
]}.

{profiles, [
    {prod, [
        {relx, [{mode, prod}]}
    ]}
]}.
```

**`roadrunner_probe.app.src`**

```erlang
{application, roadrunner_probe, [
    {description, "Http11Probe entry for the Roadrunner HTTP server"},
    {vsn, "0.1.0"},
    {registered, []},
    {mod, {roadrunner_probe_app, []}},
    {applications, [kernel, stdlib, roadrunner]},
    {env, []}
]}.
```

**`roadrunner_probe_app.erl`**

```erlang
-module(roadrunner_probe_app).
-behaviour(application).
-behaviour(supervisor).

-export([start/2, stop/1]).
-export([init/1]).

%% Starts one HTTP/1.1 listener on port 8080 with roadrunner's defaults.
%% The listener runs under roadrunner's own supervisor; this empty
%% supervisor is only the application's top process.
start(_StartType, _StartArgs) ->
    {ok, Sup} = supervisor:start_link(?MODULE, []),
    {ok, _} = roadrunner:start_listener(probe, #{
        port => 8080,
        routes => roadrunner_probe_handler:routes()
    }),
    {ok, Sup}.

stop(_State) ->
    ok.

init([]) ->
    {ok, {#{}, []}}.
```

**`roadrunner_probe_handler.erl`**

```erlang
-module(roadrunner_probe_handler).
-behaviour(roadrunner_handler).

-export([routes/0]).
-export([handle/1]).

%% `/` and every other path share one catch-all handler. HEAD needs no
%% clause of its own: roadrunner answers it like GET without the body.
routes() ->
    [
        {~"/echo", ?MODULE, echo},
        {~"/cookie", ?MODULE, cookie},
        {~"/", ?MODULE, root},
        {~"/*path", ?MODULE, root}
    ].

handle(Req) ->
    handle(roadrunner_req:state(Req), Req).

%% Every header roadrunner parsed, one `Name: Value` per line.
handle(echo, Req) ->
    Lines = [[Name, ~": ", Value, ~"\n"] || {Name, Value} <- roadrunner_req:headers(Req)],
    {roadrunner_resp:text(200, Lines), Req};
%% Every cookie roadrunner parsed, one `name=value` per line.
handle(cookie, Req) ->
    Lines = [[Name, ~"=", Value, ~"\n"] || {Name, Value} <- roadrunner_req:parse_cookies(Req)],
    {roadrunner_resp:text(200, Lines), Req};
handle(root, Req) ->
    case roadrunner_req:method(Req) of
        ~"POST" ->
            {ok, Body, Req2} = roadrunner_req:read_body(Req),
            {roadrunner_resp:text(200, Body), Req2};
        _ ->
            {roadrunner_resp:text(200, ~"OK"), Req}
    end.
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
  ProbeRender.renderServerPage('Roadrunner');
})();
</script>
