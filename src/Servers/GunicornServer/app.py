def app(environ, start_response):
    start_response('200 OK', [('Content-Type', 'text/plain')])
    if environ['REQUEST_METHOD'] == 'POST':
        try:
            length = int(environ.get('CONTENT_LENGTH', 0) or 0)
        except ValueError:
            length = 0
        body = environ['wsgi.input'].read(length) if length > 0 else b''
        return [body]
    return [b'OK']
