FROM node:22-slim
WORKDIR /app
COPY src/Servers/ExpressServer/package.json .
RUN npm install --omit=dev
COPY src/Servers/ExpressServer/server.js .
ENTRYPOINT ["node", "server.js", "9003"]
