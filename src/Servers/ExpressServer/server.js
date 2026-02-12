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

app.listen(port, "127.0.0.1", () => {
  console.log(`Express listening on 127.0.0.1:${port}`);
});
