<script lang="ts">
  import { onMount } from 'svelte';
  import { cartId, userId, cartItems, cartTotal } from '../stores/cart';

  const gatewayUrl = import.meta.env.PUBLIC_GATEWAY_URL ?? 'http://localhost:8080';

  type Product = {
    id: string;
    sku: string;
    name: string;
    description: string;
    price: number;
  };

  let products: Product[] = [];
  let orderId = '';
  let orderStatus = '';
  let error = '';

  async function loadProducts() {
    error = '';
    const response = await fetch(`${gatewayUrl}/api/catalog/v1/products`);
    if (!response.ok) {
      error = 'Unable to load products';
      return;
    }

    products = await response.json();
  }

  async function addToCart(product: Product) {
    const cid = cartId.get();
    const payload = {
      userId: userId.get(),
      productId: product.id,
      sku: product.sku,
      name: product.name,
      quantity: 1,
      unitPrice: product.price
    };

    const response = await fetch(`${gatewayUrl}/api/cart/v1/carts/${cid}/items`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });

    if (!response.ok) {
      error = 'Unable to add item to cart';
      return;
    }

    const cartResponse = await fetch(`${gatewayUrl}/api/cart/v1/carts/${cid}`);
    const cart = await cartResponse.json();
    cartItems.set(cart.items ?? []);
  }

  async function checkout() {
    error = '';
    orderStatus = '';

    const response = await fetch(`${gatewayUrl}/api/order/v1/orders`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ cartId: cartId.get(), userId: userId.get() })
    });

    if (!response.ok) {
      error = 'Checkout failed';
      return;
    }

    const body = await response.json();
    orderId = body.orderId;
    orderStatus = body.status;
    await pollOrder();
  }

  async function pollOrder() {
    if (!orderId) {
      return;
    }

    for (let attempt = 0; attempt < 20; attempt += 1) {
      const response = await fetch(`${gatewayUrl}/api/order/v1/orders/${orderId}`);
      if (!response.ok) {
        await new Promise((r) => setTimeout(r, 800));
        continue;
      }

      const order = await response.json();
      orderStatus = order.status;
      if (orderStatus === 'Completed' || orderStatus === 'Failed') {
        return;
      }

      await new Promise((r) => setTimeout(r, 800));
    }
  }

  onMount(() => {
    loadProducts();
  });
</script>

<section class="space-y-6">
  <header class="rounded-xl bg-gradient-to-r from-brand-700 to-brand-500 p-6 text-white shadow">
    <h1 class="text-2xl font-bold">CQRS E-Commerce Demo</h1>
    <p class="mt-2 text-sm text-sky-100">Astro + Svelte frontend on top of event-driven microservices</p>
  </header>

  {#if error}
    <div class="rounded-lg border border-rose-300 bg-rose-50 p-3 text-rose-700">{error}</div>
  {/if}

  <div class="grid gap-4 md:grid-cols-3">
    {#each products as product}
      <article class="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
        <h2 class="text-base font-semibold">{product.name}</h2>
        <p class="mt-1 text-sm text-slate-600">{product.description}</p>
        <div class="mt-3 flex items-center justify-between">
          <span class="text-lg font-bold">${product.price.toFixed(2)}</span>
          <button class="rounded-md bg-brand-500 px-3 py-2 text-sm font-medium text-white hover:bg-brand-700" on:click={() => addToCart(product)}>
            Add
          </button>
        </div>
      </article>
    {/each}
  </div>

  <section class="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
    <h3 class="text-lg font-semibold">Cart</h3>
    <p class="mt-1 text-sm text-slate-600">Cart ID: {$cartId}</p>

    {#if $cartItems.length === 0}
      <p class="mt-3 text-sm text-slate-500">No items yet.</p>
    {:else}
      <ul class="mt-3 space-y-2">
        {#each $cartItems as item}
          <li class="flex justify-between text-sm">
            <span>{item.name} x {item.quantity}</span>
            <span>${(item.quantity * item.unitPrice).toFixed(2)}</span>
          </li>
        {/each}
      </ul>
      <p class="mt-3 text-sm font-semibold">Total: ${$cartTotal.toFixed(2)}</p>
      <button class="mt-3 rounded-md bg-emerald-600 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-700" on:click={checkout}>
        Checkout
      </button>
    {/if}
  </section>

  {#if orderId}
    <section class="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
      <h3 class="text-lg font-semibold">Order</h3>
      <p class="mt-2 text-sm">Order ID: {orderId}</p>
      <p class="text-sm">Status: <strong>{orderStatus}</strong></p>
    </section>
  {/if}
</section>
