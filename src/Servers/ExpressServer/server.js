const express = require("express");

const app = express();
const port = parseInt(process.argv[2] || "9003", 10);

app.get("/", (_req, res) => {
  res.send("OK");
});

app.post("/", (req, res) => {
  const chunks = [];
  req.on("data", (chunk) => chunks.push(chunk));
  req.on("end", () => res.send(Buffer.concat(chunks)));
});

app.all('/cookie', (req, res) => {
  let body = '';
  const raw = req.headers.cookie || '';
  for (const pair of raw.split(';')) {
    const trimmed = pair.trimStart();
    const eq = trimmed.indexOf('=');
    if (eq > 0) body += trimmed.substring(0, eq) + '=' + trimmed.substring(eq + 1) + '\n';
  }
  res.set('Content-Type', 'text/plain').send(body);
});

app.all('/echo', (req, res) => {
  let body = '';
  for (const [name, value] of Object.entries(req.headers)) {
    if (Array.isArray(value)) value.forEach(v => body += name + ': ' + v + '\n');
    else body += name + ': ' + value + '\n';
  }
  res.set('Content-Type', 'text/plain').send(body);
});

app.listen(port, "127.0.0.1", () => {
  console.log(`Express listening on 127.0.0.1:${port}`);
});
