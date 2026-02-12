const express = require("express");

const app = express();
const port = parseInt(process.argv[2] || "9003", 10);

app.get("/", (_req, res) => {
  res.send("OK");
});

app.post("/", (_req, res) => {
  res.send("OK");
});

app.listen(port, "127.0.0.1", () => {
  console.log(`Express listening on 127.0.0.1:${port}`);
});
