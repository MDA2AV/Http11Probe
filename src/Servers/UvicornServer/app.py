async def app(scope, receive, send):
    path = scope.get('path', '/')

    if path == '/cookie':
        cookie_val = ''
        for name, value in scope.get('headers', []):
            if name.lower() == b'cookie':
                cookie_val = value.decode('latin-1')
                break
        lines = []
        for pair in cookie_val.split(';'):
            pair = pair.strip()
            eq = pair.find('=')
            if eq > 0:
                lines.append(f"{pair[:eq]}={pair[eq+1:]}")
        body = ('\n'.join(lines) + '\n').encode('utf-8') if lines else b''
        await send({
            'type': 'http.response.start',
            'status': 200,
            'headers': [(b'content-type', b'text/plain')],
        })
        await send({
            'type': 'http.response.body',
            'body': body,
        })
        return

    if path == '/echo':
        lines = []
        for name, value in scope.get('headers', []):
            lines.append(f"{name.decode('latin-1')}: {value.decode('latin-1')}")
        body = ('\n'.join(lines) + '\n').encode('utf-8')
        await send({
            'type': 'http.response.start',
            'status': 200,
            'headers': [(b'content-type', b'text/plain')],
        })
        await send({
            'type': 'http.response.body',
            'body': body,
        })
        return

    body = b'OK'
    if scope.get('method') == 'POST':
        chunks = []
        while True:
            msg = await receive()
            chunks.append(msg.get('body', b''))
            if not msg.get('more_body', False):
                break
        body = b''.join(chunks)
    await send({
        'type': 'http.response.start',
        'status': 200,
        'headers': [(b'content-type', b'text/plain')],
    })
    await send({
        'type': 'http.response.body',
        'body': body,
    })
