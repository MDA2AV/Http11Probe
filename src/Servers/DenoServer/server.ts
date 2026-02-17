Deno.serve({ port: 8080, hostname: "0.0.0.0" }, async (req) => {
  const url = new URL(req.url);
  if (url.pathname === "/echo") {
    let body = "";
    for (const [name, value] of req.headers) {
      body += name + ": " + value + "\n";
    }
    return new Response(body, { headers: { "content-type": "text/plain" } });
  }
  if (url.pathname === "/cookie") {
    let body = "";
    const raw = req.headers.get("cookie") || "";
    for (const pair of raw.split(";")) {
      const trimmed = pair.trimStart();
      const eq = trimmed.indexOf("=");
      if (eq > 0) body += trimmed.substring(0, eq) + "=" + trimmed.substring(eq + 1) + "\n";
    }
    return new Response(body, { headers: { "content-type": "text/plain" } });
  }
  if (req.method === "POST") {
    const body = await req.text();
    return new Response(body, { headers: { "content-type": "text/plain" } });
  }
  return new Response("OK", { headers: { "content-type": "text/plain" } });
});
