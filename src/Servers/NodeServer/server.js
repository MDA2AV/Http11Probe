const http = require('http');

const port = parseInt(process.argv[2] || '8080', 10);

const server = http.createServer((req, res) => {
    res.writeHead(200, { 'Content-Type': 'text/plain' });
    res.end('OK');
});

server.listen(port, '0.0.0.0');
