import http from 'http';
import httpProxy from 'http-proxy';

const proxy = httpProxy.createProxyServer({});

const server = http.createServer((req, res) => {
  if (req.url.startsWith('/api') || req.url.startsWith('/swagger') || req.url.startsWith('/uploads') || req.url.startsWith('/hubs') || req.url.startsWith('/media')) {
    // Route to Backend
    proxy.web(req, res, { target: 'http://localhost:5000' });
  } else if (req.url.startsWith('/mobile-app')) {
    // Route to Third Project (e.g. Mobile Web now served by Backend)
    proxy.web(req, res, { target: 'http://localhost:5000' });
  } else {
    // Route to Web Frontend
    proxy.web(req, res, { target: 'http://localhost:5175' });
  }
});

proxy.on('error', (err, req, res) => {
  res.writeHead(500, { 'Content-Type': 'text/plain' });
  res.end('Proxy Error: ' + err.message);
});

const PORT = 8080;
server.listen(PORT, () => {
  console.log(`Reverse proxy running on http://localhost:${PORT}`);
  console.log(`Routing /api, /swagger, /uploads, /hubs, /media, /mobile-app -> http://localhost:5000`);
  console.log(`Routing everything else -> http://localhost:5175`);
});
