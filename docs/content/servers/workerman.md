---
title: "Workerman"
toc: true
breadcrumbs: false
---

**Language:** PHP · [View source on GitHub](https://github.com/MDA2AV/tree/main/src/Servers/WorkermanServer)

## Dockerfile

```dockerfile
FROM php:8.5-cli

COPY --from=composer/composer:latest-bin --link /composer /usr/local/bin/composer

RUN docker-php-ext-install pcntl > /dev/null
RUN apt-get update -yqq && apt-get install -yqq git unzip > /dev/null && rm -rf /var/lib/apt/lists/*

WORKDIR /workerman
COPY src/Servers/WorkermanServer/composer.json .
RUN composer install --optimize-autoloader --classmap-authoritative --no-dev --quiet
COPY src/Servers/WorkermanServer/php.ini /etc/php/8.5/cli/php.ini

COPY src/Servers/WorkermanServer/start.php .

EXPOSE 8080

CMD ["php", "/workerman/start.php", "start"]
```

## Source

```php
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
  ProbeRender.renderServerPage('Workerman');
})();
</script>
