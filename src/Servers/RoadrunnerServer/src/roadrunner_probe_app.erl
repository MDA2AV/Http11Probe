-module(roadrunner_probe_app).
-behaviour(application).
-behaviour(supervisor).

-export([start/2, stop/1]).
-export([init/1]).

%% Starts one HTTP/1.1 listener on port 8080 with roadrunner's defaults,
%% plus `roadrunner_etag`, roadrunner's conditional-request middleware: it
%% gives each GET/HEAD 200 a weak ETag and answers a matching
%% If-None-Match with 304. The listener runs under roadrunner's own
%% supervisor; this empty supervisor is only the application's top
%% process.
start(_StartType, _StartArgs) ->
    {ok, Sup} = supervisor:start_link(?MODULE, []),
    {ok, _} = roadrunner:start_listener(probe, #{
        port => 8080,
        routes => roadrunner_probe_handler:routes(),
        middlewares => [roadrunner_etag]
    }),
    {ok, Sup}.

stop(_State) ->
    ok.

init([]) ->
    {ok, {#{}, []}}.
