const port = parseInt(Bun.argv[2] || "8080", 10);

Bun.serve({
  port,
  hostname: "127.0.0.1",
  fetch() {
    return new Response("OK");
  },
});

console.log(`Bun listening on 127.0.0.1:${port}`);
