Deno.serve({ port: 8080, hostname: "0.0.0.0" }, async (req) => {
  if (req.method === "POST") {
    const body = await req.text();
    return new Response(body, { headers: { "content-type": "text/plain" } });
  }
  return new Response("OK", { headers: { "content-type": "text/plain" } });
});
