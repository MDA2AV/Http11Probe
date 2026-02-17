---
title: "Gunicorn"
toc: true
breadcrumbs: false
---

**Language:** Python · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/GunicornServer)

## Dockerfile

```dockerfile
FROM python:3.13-slim
RUN pip install --no-cache-dir gunicorn
COPY src/Servers/GunicornServer/app.py /app/app.py
WORKDIR /app
EXPOSE 8080
CMD ["gunicorn", "-b", "0.0.0.0:8080", "app:app"]
```

## Source

```python
def app(environ, start_response):
    path = environ.get('PATH_INFO', '/')

    if path == '/cookie':
        cookie_str = environ.get('HTTP_COOKIE', '')
        lines = []
        for pair in cookie_str.split(';'):
            pair = pair.strip()
            eq = pair.find('=')
            if eq > 0:
                lines.append(f"{pair[:eq]}={pair[eq+1:]}")
        body = ('\n'.join(lines) + '\n').encode('utf-8') if lines else b''
        start_response('200 OK', [('Content-Type', 'text/plain')])
        return [body]

    if path == '/echo':
        lines = []
        for key, value in environ.items():
            if key.startswith('HTTP_'):
                header_name = key[5:].replace('_', '-').title()
                lines.append(f"{header_name}: {value}")
        if environ.get('CONTENT_TYPE'):
            lines.append(f"Content-Type: {environ['CONTENT_TYPE']}")
        if environ.get('CONTENT_LENGTH'):
            lines.append(f"Content-Length: {environ['CONTENT_LENGTH']}")
        body = ('\n'.join(lines) + '\n').encode('utf-8')
        start_response('200 OK', [('Content-Type', 'text/plain')])
        return [body]

    start_response('200 OK', [('Content-Type', 'text/plain')])
    if environ['REQUEST_METHOD'] == 'POST':
        try:
            length = int(environ.get('CONTENT_LENGTH', 0) or 0)
        except ValueError:
            length = 0
        body = environ['wsgi.input'].read(length) if length > 0 else b''
        return [body]
    return [b'OK']
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
  ProbeRender.renderServerPage('Gunicorn');
})();
</script>
