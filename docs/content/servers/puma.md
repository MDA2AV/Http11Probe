---
title: "Puma"
toc: true
breadcrumbs: false
---

**Language:** Ruby · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/PumaServer)

## Dockerfile

```dockerfile
FROM ruby:3.3-slim
RUN apt-get update && apt-get install -y --no-install-recommends build-essential && \
    gem install puma --no-doc && \
    apt-get purge -y build-essential && apt-get autoremove -y && \
    rm -rf /var/lib/apt/lists/*
COPY src/Servers/PumaServer/config.ru /app/config.ru
WORKDIR /app
EXPOSE 8080
CMD ["puma", "-b", "tcp://0.0.0.0:8080"]
```

## Source

```ruby
app = proc { |env|
  if env['PATH_INFO'] == '/echo'
    headers = env.select { |k, _| k.start_with?('HTTP_') }
    body = headers.map { |k, v| "#{k.sub('HTTP_', '').split('_').map(&:capitalize).join('-')}: #{v}" }.join("\n") + "\n"
    body += "Content-Type: #{env['CONTENT_TYPE']}\n" if env['CONTENT_TYPE']
    body += "Content-Length: #{env['CONTENT_LENGTH']}\n" if env['CONTENT_LENGTH']
    [200, { 'Content-Type' => 'text/plain' }, [body]]
  elsif env['PATH_INFO'] == '/cookie'
    body = ""
    if env['HTTP_COOKIE']
      env['HTTP_COOKIE'].split(';').each do |pair|
        trimmed = pair.lstrip
        eq = trimmed.index('=')
        if eq && eq > 0
          body += "#{trimmed[0...eq]}=#{trimmed[(eq+1)..]}\n"
        end
      end
    end
    [200, { 'Content-Type' => 'text/plain' }, [body]]
  elsif env['REQUEST_METHOD'] == 'POST'
    body = env['rack.input'].read
    [200, { 'content-type' => 'text/plain' }, [body]]
  else
    [200, { 'content-type' => 'text/plain' }, ['OK']]
  end
}
run app
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
  ProbeRender.renderServerPage('Puma');
})();
</script>
