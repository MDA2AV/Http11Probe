<?php

use Workerman\Worker;
use Workerman\Protocols\Http\Response;

require_once __DIR__ . '/vendor/autoload.php';

// #### http worker ####
$http_worker = new Worker('http://0.0.0.0:8080');
$http_worker->reusePort = true;
$http_worker->count = (int) shell_exec('nproc');
$http_worker->name = 'bench';

// Data received
$http_worker->onMessage = static function ($connection, $request) {

    return match($request->path()) {

        '/echo'   => $connection->send( new Response(
                                        200, 
                                        ['Content-Type' => 'text/plain'],
                                        implode("\n", array_map(fn($name, $value) => "$name: $value", $request->header(), $request->header())))
                                        ),

        '/cookie' => $connection->send( new Response(
                                        200,
                                        ['Content-Type' => 'text/plain'],
                                        implode("\n", array_map(fn($name, $value) => "$name=$value", $request->cookie(), $request->cookie())))
                                        ),

        '/'       => $connection->send( new Response(
                                        200,
                                        ['Content-Type' => 'text/plain'],
                                        $request->method() === 'POST' ? $request->rawBody() : 'OK')
                                        ),
        
        default => null,
    };
};

// Run all workers
Worker::runAll();
