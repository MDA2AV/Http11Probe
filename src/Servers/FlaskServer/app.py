import sys
from flask import Flask

app = Flask(__name__)

@app.route("/", defaults={"path": ""})
@app.route("/<path:path>")
def catch_all(path):
    return "OK"

if __name__ == "__main__":
    port = int(sys.argv[1]) if len(sys.argv) > 1 else 9002
    app.run(host="127.0.0.1", port=port)
