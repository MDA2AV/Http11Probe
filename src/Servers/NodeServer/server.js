const http = require('http');

const port = parseInt(process.argv[2] || '8080', 10);

const server = http.createServer((req, res) => {
    if (req.method === 'POST') {
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
