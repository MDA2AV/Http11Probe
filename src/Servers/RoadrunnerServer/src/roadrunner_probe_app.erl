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
