const port = parseInt(Bun.argv[2] || "8080", 10);

Bun.serve({
  port,
  hostname: "0.0.0.0",
  async fetch(req) {
    const url = new URL(req.url);
    if (url.pathname === "/echo") {
      let body = "";
      for (const [name, value] of req.headers) {
        body += name + ": " + value + "\n";
      }
      return new Response(body, { headers: { "Content-Type": "text/plain" } });
    }
    if (url.pathname === "/cookie") {
      let body = "";
      const raw = req.headers.get("cookie") || "";
      for (const pair of raw.split(";")) {
        const trimmed = pair.trimStart();
        const eq = trimmed.indexOf("=");
        if (eq > 0) body += trimmed.substring(0, eq) + "=" + trimmed.substring(eq + 1) + "\n";
      }
      return new Response(body, { headers: { "Content-Type": "text/plain" } });
    }
    if (req.method === "POST") {
      const body = await req.text();
      return new Response(body);
    }
    return new Response("OK");
  },
});

console.log(`Bun listening on 127.0.0.1:${port}`);
