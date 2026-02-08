FROM golang:1.23-alpine AS build
WORKDIR /src
COPY src/Servers/FastHttpServer/go.mod .
COPY src/Servers/FastHttpServer/main.go .
RUN go mod tidy && CGO_ENABLED=0 go build -o /fasthttp-server .

FROM alpine:3.20
COPY --from=build /fasthttp-server /usr/local/bin/
ENTRYPOINT ["fasthttp-server", "8080"]
