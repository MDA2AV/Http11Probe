import sys
from flask import Flask
from werkzeug.routing import Rule

app = Flask(__name__)

app.url_map.add(Rule('/', defaults={"path": ""}, endpoint='catch_all'))
app.url_map.add(Rule('/<path:path>', endpoint='catch_all'))

@app.endpoint('catch_all')
def catch_all(path):
    return "OK"

if __name__ == "__main__":
    port = int(sys.argv[1]) if len(sys.argv) > 1 else 8080
    app.run(host="0.0.0.0", port=port)
