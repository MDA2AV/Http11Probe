# trixie, not the default bookworm: uWS ships prebuilt binaries needing glibc >= 2.38
FROM node:22-trixie-slim
WORKDIR /app
COPY src/Servers/UWebSocketsServer/package.json src/Servers/UWebSocketsServer/package-lock.json ./
RUN npm ci --omit=dev --ignore-scripts
COPY src/Servers/UWebSocketsServer/server.js .
USER node
ENTRYPOINT ["node", "server.js", "8080"]
