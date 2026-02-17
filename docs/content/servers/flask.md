---
title: "Flask"
toc: true
breadcrumbs: false
---

**Language:** Python · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/FlaskServer)

## Dockerfile

```dockerfile
FROM python:3.12-slim
WORKDIR /app
RUN pip install --no-cache-dir flask
COPY src/Servers/FlaskServer/app.py .
ENTRYPOINT ["python3", "app.py", "8080"]
```

## Source

```python
import sys
from flask import Flask, request
from werkzeug.routing import Rule

app = Flask(__name__)

@app.route('/cookie', methods=['GET','POST','PUT','DELETE','PATCH','OPTIONS','HEAD'])
def cookie_endpoint():
    lines = []
    for name, value in request.cookies.items():
        lines.append(f"{name}={value}")
    return '\n'.join(lines) + '\n', 200, {'Content-Type': 'text/plain'}

@app.route('/echo', methods=['GET','POST','PUT','DELETE','PATCH','OPTIONS','HEAD'])
def echo():
    lines = []
    for name, value in request.headers:
        lines.append(f"{name}: {value}")
    return '\n'.join(lines) + '\n', 200, {'Content-Type': 'text/plain'}

app.url_map.add(Rule('/', defaults={"path": ""}, endpoint='catch_all'))
app.url_map.add(Rule('/<path:path>', endpoint='catch_all'))

@app.endpoint('catch_all')
def catch_all(path):
    if request.method == 'POST':
        return request.get_data(as_text=True)
    return "OK"

if __name__ == "__main__":
    port = int(sys.argv[1]) if len(sys.argv) > 1 else 8080
    app.run(host="0.0.0.0", port=port)
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
  ProbeRender.renderServerPage('Flask');
})();
</script>
