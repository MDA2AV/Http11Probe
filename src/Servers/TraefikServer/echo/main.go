package main

import (
	"io"
	"net/http"
	"strings"
)

func main() {
	http.HandleFunc("/cookie", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "text/plain")
		raw := r.Header.Get("Cookie")
		for _, pair := range strings.Split(raw, ";") {
			pair = strings.TrimLeft(pair, " ")
			if eq := strings.Index(pair, "="); eq > 0 {
				w.Write([]byte(pair[:eq] + "=" + pair[eq+1:] + "\n"))
			}
		}
	})

	http.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "Method Not Allowed", http.StatusMethodNotAllowed)
			return
		}

		body, err := io.ReadAll(r.Body)
		if err != nil {
			http.Error(w, "Failed to read body", http.StatusBadRequest)
			return
		}
		defer r.Body.Close()

		w.Header().Set("Content-Type", "text/plain")
		w.WriteHeader(http.StatusOK)
		w.Write(body)
	})

	http.ListenAndServe(":9090", nil)
}
