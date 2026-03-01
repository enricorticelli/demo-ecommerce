const gatewayBaseUrl = process.env.GATEWAY_URL ?? 'http://localhost:8080';
const iterations = Number(process.env.ITERATIONS ?? '20');
const concurrency = Number(process.env.CONCURRENCY ?? '5');
const demoUserId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

async function post(url, body) {
  const response = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body)
  });

  if (!response.ok) {
    const text = await response.text();
    throw new Error(`POST ${url} failed (${response.status}): ${text}`);
  }

  return response;
}

async function get(url) {
  const response = await fetch(url);
  if (!response.ok) {
    const text = await response.text();
    throw new Error(`GET ${url} failed (${response.status}): ${text}`);
  }

  return response;
}

async function runCheckout(products) {
  const cartId = crypto.randomUUID();
  const selected = products[Math.floor(Math.random() * products.length)];

  await post(`${gatewayBaseUrl}/api/cart/v1/carts/${cartId}/items`, {
    userId: demoUserId,
    productId: selected.id,
    sku: selected.sku,
    name: selected.name,
    quantity: 1,
    unitPrice: selected.price
  });

  const orderCreated = await post(`${gatewayBaseUrl}/api/order/v1/orders`, {
    cartId,
    userId: demoUserId
  });

  const { orderId } = await orderCreated.json();

  const deadline = Date.now() + 30_000;
  while (Date.now() < deadline) {
    const orderResponse = await get(`${gatewayBaseUrl}/api/order/v1/orders/${orderId}`);
    const order = await orderResponse.json();
    if (order.status === 'Completed') {
      return { ok: true };
    }

    if (order.status === 'Failed') {
      return { ok: false, reason: 'failed' };
    }

    await new Promise((resolve) => setTimeout(resolve, 600));
  }

  return { ok: false, reason: 'timeout' };
}

async function worker(products, queue, stats) {
  while (queue.length > 0) {
    queue.pop();
    const start = performance.now();
    try {
      const result = await runCheckout(products);
      const latency = performance.now() - start;
      stats.latencies.push(latency);
      if (result.ok) {
        stats.success += 1;
      } else {
        stats.failed += 1;
      }
    } catch {
      stats.failed += 1;
    }
  }
}

async function main() {
  const productsResponse = await get(`${gatewayBaseUrl}/api/catalog/v1/products`);
  const products = await productsResponse.json();
  if (!Array.isArray(products) || products.length === 0) {
    throw new Error('No products available');
  }

  const queue = Array.from({ length: iterations }, (_, i) => i);
  const stats = { success: 0, failed: 0, latencies: [] };

  const startedAt = performance.now();
  await Promise.all(Array.from({ length: concurrency }, () => worker(products, queue, stats)));
  const totalMs = performance.now() - startedAt;

  const p95 = stats.latencies.length === 0
    ? 0
    : [...stats.latencies].sort((a, b) => a - b)[Math.floor(stats.latencies.length * 0.95) - 1] || 0;

  console.log(JSON.stringify({
    iterations,
    concurrency,
    success: stats.success,
    failed: stats.failed,
    totalMs: Number(totalMs.toFixed(2)),
    p95Ms: Number(p95.toFixed(2))
  }, null, 2));
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
