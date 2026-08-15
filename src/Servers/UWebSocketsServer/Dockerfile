# trixie, not the default bookworm: uWS ships prebuilt binaries needing glibc >= 2.38
FROM node:22-trixie-slim
WORKDIR /app
COPY src/Servers/UWebSocketsServer/package.json .
RUN npm install --omit=dev
COPY src/Servers/UWebSocketsServer/server.js .
ENTRYPOINT ["node", "server.js", "8080"]
