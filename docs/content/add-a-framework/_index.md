---
title: Add a Framework
toc: true
---

Http11Probe is designed so anyone can contribute their HTTP server and get compliance results without touching the test infrastructure.

## Required Endpoints

Your server must listen on **port 8080** and implement three endpoints:

| Endpoint | Method | Behavior |
|----------|--------|----------|
| `/` | `GET` | Return `200 OK`. This is the baseline reachability check. |
| `/` | `POST` | Read the full request body and return it in the response. Used by body handling and smuggling tests. |
| `/echo` | `POST` | Return all received request headers in the response body, one per line as `Name: Value`. Used by normalization tests. |

### Why `/echo`?

Normalization tests need to see how the server internally represents headers after parsing. For example, if the test sends `Content_Length: 99`, the `/echo` endpoint reveals whether the server normalized the underscore to a hyphen, preserved it as-is, or dropped it entirely. Without this endpoint, normalization tests cannot run.

### Response format for `/echo`

The response body should contain one header per line in `Name: Value` format:

```
Host: localhost:8080
Content-Length: 11
Content-Type: text/plain
```

The order does not matter. Include all headers the server received (framework-added headers like `Connection` are fine).

## Steps

**1. Create a server directory** — Add a directory under `src/Servers/YourServer/` with your server source code implementing the three endpoints above.

**2. Add a Dockerfile** — Build and run your server. It will run with `--network host`.

**3. Add a `probe.json`** — One file, one field:

```json
{"name": "Your Server"}
```

Open a PR and the probe runs automatically.

## How It Works

The CI pipeline scans `src/Servers/*/probe.json` to discover servers. For each one it:

1. Builds the Docker image from the Dockerfile in that directory
2. Runs the container on port 8080 with `--network host`
3. Waits for the server to become ready
4. Runs the full probe suite (compliance, smuggling, malformed input, normalization)
5. Stops the container and moves to the next server

No workflow edits, no port allocation, no config files.

## Example

Here's the Flask server as a reference:

**`src/Servers/FlaskServer/probe.json`**
```json
{"name": "Flask"}
```

**`src/Servers/FlaskServer/Dockerfile`**
```dockerfile
FROM python:3.12-slim
WORKDIR /app
RUN pip install --no-cache-dir flask
COPY src/Servers/FlaskServer/app.py .
ENTRYPOINT ["python3", "app.py", "8080"]
```

**`src/Servers/FlaskServer/app.py`**
```python
import sys
from flask import Flask, request
from werkzeug.routing import Rule

app = Flask(__name__)

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

The key parts:
- **`/echo`** — echoes all received headers back as plain text.
- **`POST /`** — reads and returns the request body (needed for body and smuggling tests).
- **`GET /`** (catch-all) — returns `"OK"` with `200`.
