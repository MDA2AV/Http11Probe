FROM golang:1.23-alpine AS build
WORKDIR /src
COPY src/Servers/GinServer/go.mod .
COPY src/Servers/GinServer/main.go .
RUN go mod tidy && CGO_ENABLED=0 go build -o /gin-server .

FROM alpine:3.20
COPY --from=build /gin-server /usr/local/bin/
ENTRYPOINT ["gin-server", "8080"]
