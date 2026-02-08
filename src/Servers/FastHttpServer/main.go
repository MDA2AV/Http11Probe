package main

import (
	"os"

	"github.com/valyala/fasthttp"
)

func main() {
	port := "8080"
	if len(os.Args) > 1 {
		port = os.Args[1]
	}

	handler := func(ctx *fasthttp.RequestCtx) {
		ctx.SetStatusCode(200)
		ctx.SetBodyString("OK")
	}

	fasthttp.ListenAndServe("0.0.0.0:"+port, handler)
}
