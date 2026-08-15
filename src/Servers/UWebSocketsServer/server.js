const uWS = require('uWebSockets.js');

const port = Number.parseInt(process.argv[2] || '8080', 10);

/* uWS invalidates `req` the moment the handler returns, so everything needed
 * later has to be read out synchronously. Only the body echo is async here. */

function readBody(res, onDone) {
    const chunks = [];
    res.onAborted(() => { res.aborted = true; });
    res.onData((ab, isLast) => {
        /* The ArrayBuffer is only valid inside this callback — slice(0) copies it. */
        chunks.push(Buffer.from(ab.slice(0)));
        if (isLast) onDone(Buffer.concat(chunks));
    });
}

const app = uWS.App();

app.any('/cookie', (res, req) => {
    let body = '';
    const raw = req.getHeader('cookie');
    for (const pair of raw.split(';')) {
        const trimmed = pair.trimStart();
        const eq = trimmed.indexOf('=');
        if (eq > 0) body += trimmed.substring(0, eq) + '=' + trimmed.substring(eq + 1) + '\n';
    }
    res.writeHeader('Content-Type', 'text/plain');
    res.end(body);
});

app.any('/echo', (res, req) => {
    let body = '';
    req.forEach((name, value) => { body += name + ': ' + value + '\n'; });
    res.writeHeader('Content-Type', 'text/plain');
    res.end(body);
});

/* Wildcards must be registered last. */
app.any('/*', (res, req) => {
    if (req.getMethod() === 'post') {
        readBody(res, (body) => {
            if (res.aborted) return;
            /* Cork when responding from an async callback. */
            res.cork(() => {
                res.writeHeader('Content-Type', 'text/plain');
                res.end(body);
            });
        });
        return;
    }
    res.writeHeader('Content-Type', 'text/plain');
    res.end('OK');
});

app.listen('0.0.0.0', port, (token) => {
    if (!token) {
        console.error('Failed to listen on port ' + port);
        process.exit(1);
    }
});
