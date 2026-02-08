---
title: Add a Framework
toc: false
---

Http11Probe is designed so anyone can contribute their HTTP server and get compliance results without touching the test infrastructure.

## Steps

**1. Write a minimal server** — Create a directory under `src/Servers/YourServer/` with a simple HTTP server that listens on **port 8080** and returns `200 OK` on `GET /`. Any language, any framework.

**2. Add a Dockerfile** — Build and run your server. It will run with `--network host`.

**3. Add a `probe.json`** — One file, one field:

```json
{"name": "Your Server"}
```

That's it. Open a PR and the probe runs automatically.

## How It Works

The CI pipeline scans `src/Servers/*/probe.json` to discover servers. For each one it:

1. Builds the Docker image from the Dockerfile in that directory
2. Runs the container on port 8080 with `--network host`
3. Waits for the server to become ready
4. Runs the full compliance probe suite
5. Stops the container and moves to the next server

No workflow edits, no port allocation, no config files.

## Example

Here's the full Flask server as a reference:

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

**`src/Servers/FlaskServer/app.py`** — a minimal Flask app that reads the port from `sys.argv` and returns `200 OK` on `GET /`.
