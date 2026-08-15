---
title: Add a Framework
description: "How to add an HTTP server to Http11Probe: implement the required endpoints, add a Dockerfile and probe.json, and get automatic RFC 9110/9112 compliance results."
toc: true
---

Http11Probe is designed so anyone can contribute their HTTP server and get compliance results without touching the test infrastructure.

## Required Endpoints

Your server must listen on **port 8080** and implement three endpoints:

| Endpoint | Method | Behavior |
|----------|--------|----------|
| `/` | `GET` | Return `200 OK`. This is the baseline reachability check. |
| `/` | `HEAD` | Return `200 OK` with no body. Used by smuggling tests that check body handling on HEAD requests. |
| `/` | `POST` | Read the full request body and return it in the response. Used by body handling and smuggling tests. |
| `/` | `OPTIONS` | Return `200 OK`. Used by smuggling tests that check body handling on OPTIONS requests. |
| `/echo` | `GET`, `POST` | Return all received request headers in the response body, one per line as `Name: Value`. Used by normalization tests. |
| `/cookie` | `GET`, `POST` | Parse the `Cookie` header and return each cookie as `name=value` on its own line. Used by the Cookies test suite. |

HEAD and OPTIONS are handled automatically by virtually all frameworks — a catch-all route on `/` is usually enough, and you should not need to implement them explicitly. What matters is that they return `200` rather than `405`, so the smuggling tests can evaluate body handling instead of getting a method-not-allowed response.

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

### Response format for `/cookie`

Split the `Cookie` header on `;`, trim leading whitespace from each pair, find the first `=`, and output `name=value` on its own line. Given `Cookie: foo=bar; baz=qux`, the response body is:

```
foo=bar
baz=qux
```

The Cookies tests use this to see how the server parses cookie pairs it receives, so echo back what the server actually parsed rather than re-parsing the raw header yourself.

## Steps

**1. Create a server directory** — Add a directory under `src/Servers/YourServer/` with your server source code implementing the endpoints above.

**2. Add a Dockerfile** — Build and run your server. The build context is the repository root, so `COPY src/Servers/YourServer/...` paths are correct. The container runs with `--network host`, so bind to `0.0.0.0:8080`. Use `ENTRYPOINT` rather than `CMD` for the server process.

**3. Add a `probe.json`** — The display name and the implementation language:

```json
{"name": "Your Server", "language": "Python"}
```

The name appears on the leaderboard and in PR comments, and the language is used to group servers on the site. You can also add an optional `"repository"` field linking to the framework's own project.

**4. Add a server documentation page** — Create `docs/content/servers/{server-name-lowercase}.md` so the framework gets a page on the site:

````markdown
---
title: "Your Server"
description: "Your Server (Language) tested against RFC 9110/9112 for HTTP/1.1 compliance, request smuggling resistance, and malformed input handling."
toc: true
breadcrumbs: false
---

**Language:** Language · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/YourServer)

## Dockerfile

```dockerfile
[Complete Dockerfile contents]
```

## Source — `filename.ext`

```language
[Complete source file contents]
```
````

The Dockerfile section comes first, then one **Source** section per source file (excluding `probe.json`), each with the right syntax-highlight language for that file.

## Verify

Before opening the PR, build and exercise the server locally:

```bash
docker build -f src/Servers/YourServer/Dockerfile -t yourserver .
docker run --network host yourserver
```

Then check each endpoint:

```bash
curl http://localhost:8080/                                    # 200 OK
curl -X POST -d "hello" http://localhost:8080/                 # hello
curl -X POST -d "test" http://localhost:8080/echo              # one header per line
curl -H "Cookie: foo=bar; baz=qux" http://localhost:8080/cookie  # foo=bar / baz=qux
```

Finally, run the probe against it:

```bash
dotnet run --project src/Http11Probe.Cli -- --host localhost --port 8080
```

Then open a PR and the probe runs automatically.

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
{"name": "Flask", "language": "Python"}
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

@app.route('/cookie', methods=['GET','POST','PUT','DELETE','PATCH'])
def cookie_endpoint():
    lines = []
    for name, value in request.cookies.items():
        lines.append(f"{name}={value}")
    return '\n'.join(lines) + '\n', 200, {'Content-Type': 'text/plain'}

@app.route('/echo', methods=['GET','POST','PUT','DELETE','PATCH'])
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
- **`/cookie`** — echoes the cookies Flask parsed, one `name=value` per line.
- **`POST /`** — reads and returns the request body (needed for body and smuggling tests).
- **`GET /`** (catch-all) — returns `"OK"` with `200`.
- **`HEAD /`** and **`OPTIONS /`** — handled by the catch-all; return `200` so smuggling tests can evaluate body handling instead of getting `405`.
