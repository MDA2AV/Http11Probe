FROM alpine:3.20 AS plugin
RUN apk add --no-cache git
RUN git clone https://github.com/jdel/staticresponse.git /plugin

FROM traefik:v3.2
COPY --from=plugin /plugin /plugins-local/src/github.com/jdel/staticresponse/
COPY src/Servers/TraefikServer/traefik.yml /etc/traefik/traefik.yml
COPY src/Servers/TraefikServer/dynamic.yml /etc/traefik/dynamic.yml
