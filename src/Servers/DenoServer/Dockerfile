FROM denoland/deno:2
COPY src/Servers/DenoServer/server.ts /app/server.ts
WORKDIR /app
RUN deno cache server.ts
EXPOSE 8080
CMD ["deno", "run", "--allow-net", "server.ts"]
