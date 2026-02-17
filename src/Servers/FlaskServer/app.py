import sys
from flask import Flask, request
from werkzeug.routing import Rule

app = Flask(__name__)

@app.route('/cookie', methods=['GET','POST','PUT','DELETE','PATCH','OPTIONS','HEAD'])
def cookie_endpoint():
    lines = []
    for name, value in request.cookies.items():
        lines.append(f"{name}={value}")
    return '\n'.join(lines) + '\n', 200, {'Content-Type': 'text/plain'}

@app.route('/echo', methods=['GET','POST','PUT','DELETE','PATCH','OPTIONS','HEAD'])
def echo():
    lines = []
    for name, value in request.headers:
        lines.append(f"{name}: {value}")
    return '\n'.join(lines) + '\n', 200, {'Content-Type': 'text/plain'}

app.url_map.add(Rule('/', defaults={"path": ""}, endpoint='catch_all'))
app.url_map.add(Rule('/<path:path>', endpoint='catch_all'))

@app.endpoint('catch_all')
def catch_all(path):
    if request.method == 'POST':
        return request.get_data(as_text=True)
    return "OK"

if __name__ == "__main__":
    port = int(sys.argv[1]) if len(sys.argv) > 1 else 8080
    app.run(host="0.0.0.0", port=port)
