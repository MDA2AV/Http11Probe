const port = parseInt(Bun.argv[2] || "8080", 10);

Bun.serve({
  port,
  hostname: "0.0.0.0",
  async fetch(req) {
    if (req.method === "POST") {
      const body = await req.text();
      return new Response(body);
    }
    return new Response("OK");
  },
});

console.log(`Bun listening on 127.0.0.1:${port}`);
