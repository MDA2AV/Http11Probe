---
title: "Uvicorn"
toc: true
breadcrumbs: false
---

**Language:** Python · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/UvicornServer)

## Dockerfile

```dockerfile
FROM python:3.13-slim
RUN pip install --no-cache-dir 'uvicorn[standard]'
COPY src/Servers/UvicornServer/app.py /app/app.py
WORKDIR /app
EXPOSE 8080
CMD ["uvicorn", "app:app", "--host", "0.0.0.0", "--port", "8080"]
```

## Source

```python
async def app(scope, receive, send):
    path = scope.get('path', '/')

    if path == '/cookie':
        cookie_val = ''
        for name, value in scope.get('headers', []):
            if name.lower() == b'cookie':
                cookie_val = value.decode('latin-1')
                break
        lines = []
        for pair in cookie_val.split(';'):
            pair = pair.strip()
            eq = pair.find('=')
            if eq > 0:
                lines.append(f"{pair[:eq]}={pair[eq+1:]}")
        body = ('\n'.join(lines) + '\n').encode('utf-8') if lines else b''
        await send({
            'type': 'http.response.start',
            'status': 200,
            'headers': [(b'content-type', b'text/plain')],
        })
        await send({
            'type': 'http.response.body',
            'body': body,
        })
        return

    if path == '/echo':
        lines = []
        for name, value in scope.get('headers', []):
            lines.append(f"{name.decode('latin-1')}: {value.decode('latin-1')}")
        body = ('\n'.join(lines) + '\n').encode('utf-8')
        await send({
            'type': 'http.response.start',
            'status': 200,
            'headers': [(b'content-type', b'text/plain')],
        })
        await send({
            'type': 'http.response.body',
            'body': body,
        })
        return

    body = b'OK'
    if scope.get('method') == 'POST':
        chunks = []
        while True:
            msg = await receive()
            chunks.append(msg.get('body', b''))
            if not msg.get('more_body', False):
                break
        body = b''.join(chunks)
    await send({
        'type': 'http.response.start',
        'status': 200,
        'headers': [(b'content-type', b'text/plain')],
    })
    await send({
        'type': 'http.response.body',
        'body': body,
    })
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
  ProbeRender.renderServerPage('Uvicorn');
})();
</script>
