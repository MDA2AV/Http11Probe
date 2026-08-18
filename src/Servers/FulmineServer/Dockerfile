FROM node:22-trixie-slim
# fulmine.js depends on uWebSockets.js, which npm installs from its git tag rather than from the
# registry, and whose prebuilt binary needs glibc 2.38, which is why this is trixie and not the
# bookworm -slim every other server here uses. git fetches it over https because npm asks for the
# repository over ssh by default and this image carries no key.
RUN apt-get update \
    && apt-get install -y --no-install-recommends git ca-certificates \
    && rm -rf /var/lib/apt/lists/* \
    && git config --global url."https://github.com/".insteadOf ssh://git@github.com/
WORKDIR /app
COPY src/Servers/FulmineServer/package.json .
# the version is pinned rather than ranged, since this repo does not keep lock files, and nothing
# here needs an install script to run
RUN npm install --omit=dev --ignore-scripts
# ExpressServer's application, not a copy of it. Fulmine.js is a drop-in replacement for Express 5,
# so the package is installed under the name "express" and the same file runs unchanged: what is
# probed here is the same application as the Express row, with only the framework behind it swapped.
COPY src/Servers/ExpressServer/server.js .
USER node
ENTRYPOINT ["node", "server.js", "8080"]
