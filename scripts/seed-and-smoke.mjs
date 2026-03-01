const catalogServiceUrl = process.env.CATALOG_URL ?? 'http://catalog-api:8080';

async function waitFor(url, retries = 60, delayMs = 2000) {
  for (let attempt = 1; attempt <= retries; attempt += 1) {
    try {
      const response = await fetch(url);
      if (response.ok) {
        return;
      }
    } catch {
      // retry
    }

    await new Promise((resolve) => setTimeout(resolve, delayMs));
  }

  throw new Error(`Service not ready: ${url}`);
}

async function post(url, body) {
  const response = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: body ? JSON.stringify(body) : undefined
  });

  if (!response.ok) {
    const text = await response.text();
    throw new Error(`POST ${url} failed (${response.status}): ${text}`);
  }

  return response;
}

async function main() {
  console.log('[seed] waiting for catalog service');
  await waitFor(`${catalogServiceUrl}/health/ready`);
  console.log('[seed] seeding products only');
  await post(`${catalogServiceUrl}/internal/seed/products`);
  console.log('[seed] done');
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
