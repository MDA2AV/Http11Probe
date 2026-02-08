Deno.serve({ port: 8080, hostname: "0.0.0.0" }, () => {
  return new Response("OK", { headers: { "content-type": "text/plain" } });
});
