def app(environ, start_response):
    path = environ.get('PATH_INFO', '/')

    if path == '/cookie':
        cookie_str = environ.get('HTTP_COOKIE', '')
        lines = []
        for pair in cookie_str.split(';'):
            pair = pair.strip()
            eq = pair.find('=')
            if eq > 0:
                lines.append(f"{pair[:eq]}={pair[eq+1:]}")
        body = ('\n'.join(lines) + '\n').encode('utf-8') if lines else b''
        start_response('200 OK', [('Content-Type', 'text/plain')])
        return [body]

    if path == '/echo':
        lines = []
        for key, value in environ.items():
            if key.startswith('HTTP_'):
                header_name = key[5:].replace('_', '-').title()
                lines.append(f"{header_name}: {value}")
        if environ.get('CONTENT_TYPE'):
            lines.append(f"Content-Type: {environ['CONTENT_TYPE']}")
        if environ.get('CONTENT_LENGTH'):
            lines.append(f"Content-Length: {environ['CONTENT_LENGTH']}")
        body = ('\n'.join(lines) + '\n').encode('utf-8')
        start_response('200 OK', [('Content-Type', 'text/plain')])
        return [body]

    if environ['REQUEST_METHOD'] == 'POST':
        if environ.get('HTTP_TRANSFER_ENCODING'):
            # chunked request: no Content-Length; Gunicorn's worker dechunks, so read to EOF
            try:
                body = environ['wsgi.input'].read()
            except Exception:
                start_response('400 Bad Request', [('Content-Type', 'text/plain')])
                return [b'']
        else:
            try:
                length = int(environ.get('CONTENT_LENGTH', 0) or 0)
            except ValueError:
                length = 0
            body = environ['wsgi.input'].read(length) if length > 0 else b''
        start_response('200 OK', [('Content-Type', 'text/plain')])
        return [body]
    start_response('200 OK', [('Content-Type', 'text/plain')])
    return [b'OK']
