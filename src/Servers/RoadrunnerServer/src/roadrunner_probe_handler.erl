-module(roadrunner_probe_handler).
-behaviour(roadrunner_handler).

-export([routes/0]).
-export([handle/1]).

%% `/` and every other path share one catch-all handler. Every route
%% answers GET, HEAD and POST; roadrunner's router answers any other
%% method with 405 and an `Allow` header. HEAD needs no clause of its
%% own: roadrunner answers it like GET without the body.
routes() ->
    [
        route(~"/echo", echo),
        route(~"/cookie", cookie),
        route(~"/", root),
        route(~"/*path", root)
    ].

route(Path, State) ->
    #{
        path => Path,
        handler => ?MODULE,
        state => State,
        methods => [~"GET", ~"HEAD", ~"POST"]
    }.

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
