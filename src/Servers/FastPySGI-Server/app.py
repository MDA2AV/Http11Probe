import fastpysgi

def app(environ, start_response):
    path = environ["PATH_INFO"]
    req_method = environ.get('REQUEST_METHOD', '')

    if path == '/cookie':
        cookie_str = environ.get('HTTP_COOKIE', '')
        lines = [ ]
        for pair in cookie_str.split(';'):
            pair = pair.strip()
            eq = pair.find('=')
            if eq > 0:
                lines.append(f"{pair[:eq]}={pair[eq+1:]}")
        body = '\n'.join(lines) + '\n' if lines else ''
        start_response('200 OK', [ ('Content-Type', 'text/plain') ])
        return [ body.encode() ] 

    if path == '/echo':
        lines = [ ]
        for key, value in environ.items():
            header_name = None
            if key.startswith('HTTP_'):
                header_name = key[5:]
            if key == 'CONTENT_TYPE':
                header_name = 'Content-Type'
            if key == 'CONTENT_LENGTH':
                header_name = 'Content-Length'
            if header_name:
                header_name = header_name.replace('_', '-').title()
                lines.append(f"{header_name}: {value}")
        body = '\n'.join(lines) + '\n'
        start_response('200 OK', [ ('Content-Type', 'text/plain') ])
        return [ body.encode() ]

    if req_method == 'POST':
        try:
            length = int(environ.get('CONTENT_LENGTH', 0) or 0)
        except ValueError:
            length = 0
        body = environ['wsgi.input'].read(length) if length > 0 else b''
        start_response('200 OK', [ ('Content-Type', 'text/plain') ])
        return [ body ]

    start_response('200 OK', [ ('Content-Type', 'text/plain') ])
    return [ b'OK' ]

if __name__ == "__main__":
    import sys
    port = int(sys.argv[1]) if len(sys.argv) > 1 else 8080
    fastpysgi.run(app, host="0.0.0.0", port=port, loglevel=0)
