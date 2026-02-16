---
title: "PHP"
toc: true
breadcrumbs: false
---

**Language:** PHP · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/PhpServer)

## Dockerfile

```dockerfile
FROM php:8.3-cli
COPY src/Servers/PhpServer/index.php /app/index.php
WORKDIR /app
EXPOSE 8080
CMD ["php", "-S", "0.0.0.0:8080", "index.php"]
```

## Source

```php
<?php
if ($_SERVER['REQUEST_URI'] === '/echo') {
    header('Content-Type: text/plain');
    foreach (getallheaders() as $name => $value) {
        echo "$name: $value\n";
    }
    exit;
}

if ($_SERVER['REQUEST_URI'] === '/cookie') {
    header('Content-Type: text/plain');
    foreach ($_COOKIE as $name => $value) {
        echo "$name=$value\n";
    }
    exit;
}

header('Content-Type: text/plain');
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    echo file_get_contents('php://input');
} else {
    echo 'OK';
}
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

<script src="/Http11Probe/probe/data.js"></script>
<script src="/Http11Probe/probe/render.js"></script>
<script>
(function() {
  if (!window.PROBE_DATA) {
    document.getElementById('server-summary').innerHTML = '<p><em>No probe data available yet. Run the Probe workflow on <code>main</code> to generate results.</em></p>';
    return;
  }
  ProbeRender.renderServerPage('PHP');
})();
</script>
