<script lang="ts">
  import { onMount } from 'svelte';
  import { fetchOrder, type OrderView } from '../lib/api';

  export let orderId: string;

  let order: OrderView | null = null;
  let error = '';
  let loading = false;
  let copied = false;

  const formatMoney = (value: number): string => `EUR ${value.toFixed(2)}`;

  const totalQuantity = (items: OrderView['items']): number =>
    items.reduce((total, item) => total + item.quantity, 0);

  async function loadOrder() {
    loading = true;
    error = '';
    order = null;

    if (!orderId?.trim()) {
      error = 'Order ID mancante';
      loading = false;
      return;
    }

    try {
      order = await fetchOrder(orderId.trim());
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore caricamento ordine';
    } finally {
      loading = false;
    }
  }

  async function copyOrderId() {
    if (!order) return;
    await navigator.clipboard.writeText(order.id);
    copied = true;
    setTimeout(() => {
      copied = false;
    }, 1500);
  }

  onMount(loadOrder);
</script>

<div class="space-y-6">
  <section class="surface-card p-5">
    <div class="flex flex-wrap items-start justify-between gap-3">
      <div>
        <h1 class="text-3xl font-extrabold text-[#1c2430]">Dettaglio Ordine</h1>
        <p class="mt-2 text-sm text-[#5a6472]">Vista completa ordine con dati operativi.</p>
      </div>
      <div class="flex flex-wrap items-center justify-end gap-2">
        <a class="btn-secondary" href="/orders">Torna alla lista</a>
        <button class="btn-secondary" on:click={loadOrder} disabled={loading}>
          {loading ? 'Aggiornamento...' : 'Aggiorna'}
        </button>
        {#if order}
          <button class="btn-secondary" on:click={copyOrderId}>
            {copied ? 'ID copiato' : 'Copia Order ID'}
          </button>
          <a
            class="btn-primary"
            href={`http://localhost:3000/orders/${order.id}`}
            target="_blank"
            rel="noreferrer"
          >
            Apri pagina store
          </a>
        {/if}
      </div>
    </div>
  </section>

  {#if error}
    <section class="surface-card p-5">
      <p class="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700">{error}</p>
    </section>
  {/if}

  {#if order}
    <section class="surface-card p-5">
      <h2 class="text-2xl font-bold text-[#1c2430]">Dati ordine</h2>
      <div class="mt-4 grid gap-3 md:grid-cols-2 xl:grid-cols-3">
        <div class="rounded-xl border border-[#d9dee8] p-4">
          <p class="text-xs font-semibold uppercase tracking-[0.16em] text-[#5a6472]">Order ID</p>
          <p class="mt-1 font-mono text-xs text-[#1c2430]">{order.id}</p>
        </div>
        <div class="rounded-xl border border-[#d9dee8] p-4">
          <p class="text-xs font-semibold uppercase tracking-[0.16em] text-[#5a6472]">Cart ID</p>
          <p class="mt-1 font-mono text-xs text-[#1c2430]">{order.cartId}</p>
        </div>
        <div class="rounded-xl border border-[#d9dee8] p-4">
          <p class="text-xs font-semibold uppercase tracking-[0.16em] text-[#5a6472]">User ID</p>
          <p class="mt-1 font-mono text-xs text-[#1c2430]">{order.userId}</p>
        </div>
        <div class="rounded-xl border border-[#d9dee8] p-4">
          <p class="text-xs font-semibold uppercase tracking-[0.16em] text-[#5a6472]">Stato</p>
          <p class="mt-1 text-sm font-semibold text-[#1c2430]">{order.status}</p>
        </div>
        <div class="rounded-xl border border-[#d9dee8] p-4">
          <p class="text-xs font-semibold uppercase tracking-[0.16em] text-[#5a6472]">Totale</p>
          <p class="mt-1 text-sm font-semibold text-[#1c2430]">{formatMoney(order.totalAmount)}</p>
        </div>
        <div class="rounded-xl border border-[#d9dee8] p-4">
          <p class="text-xs font-semibold uppercase tracking-[0.16em] text-[#5a6472]">Quantita totale</p>
          <p class="mt-1 text-sm font-semibold text-[#1c2430]">{totalQuantity(order.items)}</p>
        </div>
      </div>

      <div class="mt-4 grid gap-3 md:grid-cols-2">
        <div class="rounded-xl border border-[#d9dee8] p-4">
          <p class="text-xs font-semibold uppercase tracking-[0.16em] text-[#5a6472]">Tracking</p>
          <p class="mt-1 font-mono text-xs text-[#1c2430]">{order.trackingCode ?? '-'}</p>
        </div>
        <div class="rounded-xl border border-[#d9dee8] p-4">
          <p class="text-xs font-semibold uppercase tracking-[0.16em] text-[#5a6472]">Transaction</p>
          <p class="mt-1 font-mono text-xs text-[#1c2430]">{order.transactionId ?? '-'}</p>
        </div>
      </div>

      {#if order.failureReason}
        <p class="mt-4 rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700">
          <strong>Motivo errore:</strong> {order.failureReason}
        </p>
      {/if}
    </section>

    <section class="surface-card p-5">
      <h2 class="text-2xl font-bold text-[#1c2430]">Righe ordine</h2>
      <div class="mt-4 overflow-x-auto">
        <table class="w-full min-w-[980px] text-left text-sm">
          <thead>
            <tr class="border-b border-[#d9dee8] text-[#5a6472]">
              <th class="px-2 py-2">Product ID</th>
              <th class="px-2 py-2">SKU</th>
              <th class="px-2 py-2">Nome</th>
              <th class="px-2 py-2">Qta</th>
              <th class="px-2 py-2">Prezzo unitario</th>
              <th class="px-2 py-2">Subtotale</th>
            </tr>
          </thead>
          <tbody>
            {#each order.items as item}
              <tr class="border-b border-[#edf1f7]">
                <td class="px-2 py-2 font-mono text-xs">{item.productId}</td>
                <td class="px-2 py-2 font-mono text-xs">{item.sku}</td>
                <td class="px-2 py-2">{item.name}</td>
                <td class="px-2 py-2">{item.quantity}</td>
                <td class="px-2 py-2">{formatMoney(item.unitPrice)}</td>
                <td class="px-2 py-2">{formatMoney(item.quantity * item.unitPrice)}</td>
              </tr>
            {/each}
          </tbody>
          <tfoot>
            <tr>
              <td class="px-2 py-3 text-right text-sm font-semibold text-[#1c2430]" colspan="5">Totale ordine</td>
              <td class="px-2 py-3 text-sm font-bold text-[#1c2430]">{formatMoney(order.totalAmount)}</td>
            </tr>
          </tfoot>
        </table>
      </div>
    </section>
  {/if}
</div>
