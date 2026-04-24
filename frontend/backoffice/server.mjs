import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { createReadStream, existsSync } from 'node:fs';
import path from 'node:path';

const host = process.env.HOST ?? '0.0.0.0';
const port = Number(process.env.PORT ?? '3001');
const dist = path.resolve('dist');

const contentTypes = {
  '.css': 'text/css; charset=utf-8',
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.map': 'application/json; charset=utf-8',
  '.svg': 'image/svg+xml',
};

function runtimeConfig() {
  return `window.__BACKOFFICE_CONFIG__ = ${JSON.stringify({
    apiEntrypoint: process.env.BACKOFFICE_API_ENTRYPOINT ?? 'http://localhost:18080',
    openApiUrl: process.env.BACKOFFICE_OPENAPI_URL ?? 'http://localhost:18080/openapi/backoffice.json',
    keycloakAuthority: process.env.BACKOFFICE_KEYCLOAK_AUTHORITY ?? 'http://localhost:18081/realms/demo-ecommerce',
    keycloakClientId: process.env.BACKOFFICE_KEYCLOAK_CLIENT_ID ?? 'backoffice-web',
  })};`;
}

function sendFile(response, filePath) {
  const extension = path.extname(filePath);
  response.writeHead(200, {
    'Content-Type': contentTypes[extension] ?? 'application/octet-stream',
  });
  createReadStream(filePath).pipe(response);
}

const server = createServer(async (request, response) => {
  if (request.url === '/health/ready' || request.url === '/health/live') {
    response.writeHead(200, { 'Content-Type': 'text/plain; charset=utf-8' });
    response.end('ok');
    return;
  }

  if (request.url === '/config.js') {
    response.writeHead(200, {
      'Cache-Control': 'no-store',
      'Content-Type': 'text/javascript; charset=utf-8',
    });
    response.end(runtimeConfig());
    return;
  }

  const requestPath = decodeURIComponent(new URL(request.url ?? '/', 'http://localhost').pathname);
  const safePath = path.normalize(requestPath).replace(/^(\.\.(\/|\\|$))+/, '');
  const filePath = path.join(dist, safePath === '/' ? 'index.html' : safePath);

  if (existsSync(filePath) && !filePath.endsWith(path.sep)) {
    sendFile(response, filePath);
    return;
  }

  const index = await readFile(path.join(dist, 'index.html'));
  response.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
  response.end(index);
});

server.listen(port, host, () => {
  console.log(`Backoffice listening on http://${host}:${port}`);
});
