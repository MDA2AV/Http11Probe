FROM oven/bun:1-slim
WORKDIR /app
COPY src/Servers/BunServer/server.ts .
ENTRYPOINT ["bun", "run", "server.ts", "8080"]
