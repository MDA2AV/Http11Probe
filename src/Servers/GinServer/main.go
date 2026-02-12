package main

import (
	"io"
	"os"

	"github.com/gin-gonic/gin"
)

func main() {
	port := "8080"
	if len(os.Args) > 1 {
		port = os.Args[1]
	}

	gin.SetMode(gin.ReleaseMode)
	r := gin.New()
	r.NoRoute(func(c *gin.Context) {
		if c.Request.Method == "POST" {
			body, _ := io.ReadAll(c.Request.Body)
			c.Data(200, "text/plain", body)
			return
		}
		c.String(200, "OK")
	})
	r.Run("0.0.0.0:" + port)
}
