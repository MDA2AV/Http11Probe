function echo(r) {
    var body = '';
    var headers = r.headersIn;
    for (var name in headers) {
        body += name + ': ' + headers[name] + '\n';
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

export default { echo, handler };
