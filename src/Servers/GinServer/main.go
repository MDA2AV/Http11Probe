package main

import (
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
		c.String(200, "OK")
	})
	r.Run("0.0.0.0:" + port)
}
