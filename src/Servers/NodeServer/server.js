const http = require('http');

const port = parseInt(process.argv[2] || '8080', 10);

const server = http.createServer((req, res) => {
    let pathname;
    try {
        pathname = new URL(req.url, `http://${req.headers.host || 'localhost'}`).pathname;
    } catch {
        pathname = req.url;
    }
    if (pathname === '/cookie') {
        let body = '';
        const raw = req.headers.cookie || '';
        for (const pair of raw.split(';')) {
            const trimmed = pair.trimStart();
            const eq = trimmed.indexOf('=');
            if (eq > 0) body += trimmed.substring(0, eq) + '=' + trimmed.substring(eq + 1) + '\n';
        }
        res.writeHead(200, { 'Content-Type': 'text/plain' });
        res.end(body);
    } else if (pathname === '/echo') {
        let body = '';
        for (const [name, value] of Object.entries(req.headers)) {
            if (Array.isArray(value)) value.forEach(v => body += name + ': ' + v + '\n');
            else body += name + ': ' + value + '\n';
        }
        res.writeHead(200, { 'Content-Type': 'text/plain' });
        res.end(body);
    } else if (req.method === 'POST') {
        const chunks = [];
        req.on('data', (chunk) => chunks.push(chunk));
        req.on('end', () => {
            res.writeHead(200, { 'Content-Type': 'text/plain' });
            res.end(Buffer.concat(chunks));
        });
    } else {
        res.writeHead(200, { 'Content-Type': 'text/plain' });
        res.end('OK');
    }
});

server.listen(port, '0.0.0.0');
