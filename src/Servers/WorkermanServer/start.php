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

    $path = $request->path();

    if ($path === '/echo') {
        $body = '';
        foreach ($request->header() as $name => $value) {
            $body .= "$name: $value\n";
        }

        return $connection->send( new Response(
            200, 
            ['Content-Type' => 'text/plain'],
            $body
        ));
    }

    if ($path === '/cookie') {
        $body = '';
        foreach ($request->cookie() as $name => $value) {
            $body .= "$name=$value\n";
        }
        
        return $connection->send( new Response(
            200,
            ['Content-Type' => 'text/plain'],
            $body
        ));
    }

    if ($path === '/') {
        $body = $request->method() === 'POST' ? $request->rawBody() : 'OK';
        
        return $connection->send( new Response(
            200,
            ['Content-Type' => 'text/plain'],
            $body
        ));
    }
};

// Run all workers
Worker::runAll();
