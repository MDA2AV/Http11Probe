function echo(r) {
    var body = '';
    var headers = r.headersIn;
    for (var name in headers) {
        body += name + ': ' + headers[name] + '\n';
    }
    r.return(200, body);
}

function cookie(r) {
    var body = '';
    var raw = r.headersIn['Cookie'];
    if (raw) {
        var pairs = raw.split(';');
        for (var i = 0; i < pairs.length; i++) {
            var trimmed = pairs[i].replace(/^\s+/, '');
            var eq = trimmed.indexOf('=');
            if (eq > 0) {
                body += trimmed.substring(0, eq) + '=' + trimmed.substring(eq + 1) + '\n';
            }
        }
    }
    r.return(200, body);
}

function handler(r) {
    if (r.method === 'POST') {
        r.return(200, r.requestText || '');
    } else {
        r.return(200, 'OK');
    }
}

export default { echo, cookie, handler };
