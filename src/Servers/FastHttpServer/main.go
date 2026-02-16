package main

import (
	"os"
	"strings"

	"github.com/valyala/fasthttp"
)

func main() {
	port := "8080"
	if len(os.Args) > 1 {
		port = os.Args[1]
	}

	handler := func(ctx *fasthttp.RequestCtx) {
		ctx.SetStatusCode(200)
		switch string(ctx.Path()) {
		case "/echo":
			ctx.SetContentType("text/plain")
			ctx.Request.Header.VisitAll(func(key, value []byte) {
				ctx.WriteString(string(key) + ": " + string(value) + "\n")
			})
		case "/cookie":
			ctx.SetContentType("text/plain")
			raw := string(ctx.Request.Header.Peek("Cookie"))
			for _, pair := range strings.Split(raw, ";") {
				pair = strings.TrimLeft(pair, " ")
				if eq := strings.Index(pair, "="); eq > 0 {
					ctx.WriteString(pair[:eq] + "=" + pair[eq+1:] + "\n")
				}
			}
		default:
			if string(ctx.Method()) == "POST" {
				ctx.SetBody(ctx.Request.Body())
				return
			}
			ctx.SetBodyString("OK")
		}
	}

	fasthttp.ListenAndServe("0.0.0.0:"+port, handler)
}
