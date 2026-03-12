<script lang="ts">
  import { onMount } from 'svelte';
  import { fetchOrder, manualCancelOrder, manualCompleteOrder, type OrderView } from '../lib/api';

  export let orderId: string;
  export let canWrite = false;

  let order: OrderView | null = null;
  let error = '';
  let loading = false;
  let copied = false;
  let actionLoading = false;
  let actionError = '';
  let canComplete = false;
  let canCancel = false;
  let backgroundRefreshTimer: ReturnType<typeof setTimeout> | null = null;
  let backgroundRefreshInFlight = false;

  const FAST_REFRESH_MS = 1000;
  const TERMINAL_REFRESH_MS = 5000;
  const HIDDEN_REFRESH_MS = 10000;

  const formatMoney = (value: number): string => `EUR ${value.toFixed(2)}`;

  const statusBadgeByCode: Record<string, string> = {
    pending: 'border-amber-200 bg-amber-50 text-amber-700',
    stockreserved: 'border-sky-200 bg-sky-50 text-sky-700',
    paymentauthorized: 'border-blue-200 bg-blue-50 text-blue-700',
    completed: 'border-emerald-200 bg-emerald-50 text-emerald-700',
    cancelled: 'border-rose-200 bg-rose-50 text-rose-700',
    failed: 'border-rose-200 bg-rose-50 text-rose-700'
  };

  const shippingStatusBadgeByCode: Record<string, string> = {
    preparing: 'border-amber-200 bg-amber-50 text-amber-700',
    created: 'border-sky-200 bg-sky-50 text-sky-700',
    cancelled: 'border-rose-200 bg-rose-50 text-rose-700'
  };

  $: canComplete = !!order && order.status !== 'Completed';
  $: canCancel = !!order && order.status !== 'Cancelled';

  function getStatusBadgeClass(status: string): string {
    return statusBadgeByCode[status.replace(/\s+/g, '').toLowerCase()] ?? 'border-slate-200 bg-slate-50 text-slate-700';
  }

  function formatStatus(status: string): string {
    const normalized = status.replace(/([a-z])([A-Z])/g, '$1 $2').trim();
    return normalized.length > 0 ? normalized : status;
  }

  function getShippingStatusCode(currentOrder: OrderView): 'preparing' | 'created' | 'cancelled' {
    const normalizedOrderStatus = currentOrder.status.replace(/\s+/g, '').toLowerCase();
    if (normalizedOrderStatus === 'failed') {
      return 'cancelled';
    }

    if (currentOrder.trackingCode && currentOrder.trackingCode.trim().length > 0) {
      return 'created';
    }

    return 'preparing';
  }

  function getShippingStatusLabel(currentOrder: OrderView): string {
    const code = getShippingStatusCode(currentOrder);
    if (code === 'cancelled') return 'Annullata';
    if (code === 'created') return 'Creata';
    return 'In preparazione';
  }

  function getShippingStatusBadgeClass(currentOrder: OrderView): string {
    return shippingStatusBadgeByCode[getShippingStatusCode(currentOrder)];
  }

  function isTerminalOrderStatus(status: string): boolean {
    const normalized = status.replace(/\s+/g, '').toLowerCase();
    return normalized === 'completed' || normalized === 'failed';
  }

  function getBackgroundRefreshMs(): number {
    if (document.hidden) {
      return HIDDEN_REFRESH_MS;
    }

    if (order && isTerminalOrderStatus(order.status)) {
      return TERMINAL_REFRESH_MS;
    }

    return FAST_REFRESH_MS;
  }

  function clearBackgroundRefreshTimer() {
    if (backgroundRefreshTimer !== null) {
      clearTimeout(backgroundRefreshTimer);
      backgroundRefreshTimer = null;
    }
  }

  function scheduleBackgroundRefresh() {
    clearBackgroundRefreshTimer();
    backgroundRefreshTimer = setTimeout(async () => {
      if (!actionLoading && !backgroundRefreshInFlight) {
        await loadOrder(true);
      }

      scheduleBackgroundRefresh();
    }, getBackgroundRefreshMs());
  }

  async function loadOrder(background = false) {
    if (!background) {
      loading = true;
      error = '';
      order = null;
    } else {
      backgroundRefreshInFlight = true;
    }

    if (!orderId?.trim()) {
      error = 'Order ID mancante';
      if (!background) {
        loading = false;
      } else {
        backgroundRefreshInFlight = false;
      }
      return;
    }

    try {
      order = await fetchOrder(orderId.trim());
      if (background) {
        error = '';
      }
    } catch (err) {
      if (!background || !order) {
        error = err instanceof Error ? err.message : 'Errore caricamento ordine';
      }
    } finally {
      if (!background) {
        loading = false;
      } else {
        backgroundRefreshInFlight = false;
      }
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

  async function completeManually() {
    if (!canWrite) return;
    if (!order || actionLoading) return;
    if (order.status === 'Completed') return;

    const confirmed = window.confirm('Confermare completamento manuale ordine?');
    if (!confirmed) return;

    actionLoading = true;
    actionError = '';

    try {
      const trackingCode = window.prompt('Tracking code (opzionale):', order.trackingCode ?? '') ?? '';
      const transactionId = window.prompt('Transaction ID (opzionale):', order.transactionId ?? '') ?? '';
      await manualCompleteOrder(order.id, { trackingCode, transactionId });
      await loadOrder(false);
    } catch (err) {
      actionError = err instanceof Error ? err.message : 'Errore completamento manuale ordine';
    } finally {
      actionLoading = false;
    }
  }

  async function cancelManually() {
    if (!canWrite) return;
    if (!order || actionLoading) return;
    if (order.status === 'Cancelled') return;

    const confirmed = window.confirm('Confermare annullamento manuale ordine?');
    if (!confirmed) return;

    actionLoading = true;
    actionError = '';

    try {
      const reason = window.prompt('Motivo annullamento (opzionale):', 'Cancelled by backoffice') ?? '';
      await manualCancelOrder(order.id, reason);
      await loadOrder(false);
    } catch (err) {
      actionError = err instanceof Error ? err.message : 'Errore annullamento manuale ordine';
    } finally {
      actionLoading = false;
    }
  }

  onMount(() => {
    const handleVisibilityChange = () => {
      if (!document.hidden) {
        void loadOrder(true);
      }
    };

    document.addEventListener('visibilitychange', handleVisibilityChange);
    void loadOrder(false);
    scheduleBackgroundRefresh();

    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange);
      clearBackgroundRefreshTimer();
    };
  });
</script>

<div class="space-y-6">
  <section class="surface-card overflow-hidden">
    <div class="bg-gradient-to-r from-[#f4f8ff] via-white to-[#eef7ff] p-5 md:p-6">
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div>
          <p class="text-xs font-semibold uppercase tracking-[0.16em] text-[#4c5a6e]">Backoffice Ordini</p>
          <h1 class="mt-1 text-3xl font-extrabold text-[#1c2430]">Dettaglio Ordine</h1>
          <p class="mt-2 text-sm text-[#5a6472]">Vista operativa completa con dati cliente, spedizione e righe articolo.</p>
          {#if order}
            <div class="mt-3 flex flex-wrap items-center gap-2">
              <span class="rounded-lg border border-[#d9dee8] bg-white px-2.5 py-1 font-mono text-xs text-[#1c2430]">
                {order.id}
              </span>
              <span class={`rounded-full border px-2.5 py-1 text-xs font-semibold ${getStatusBadgeClass(order.status)}`}>
                {formatStatus(order.status)}
              </span>
            </div>
          {/if}
        </div>
        {#if order}
          <div class="grid w-full gap-2 sm:max-w-[420px] sm:grid-cols-2">
            <div class="rounded-xl border border-[#dce5f4] bg-white/90 p-3">
              <p class="text-[11px] font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Totale ordine</p>
              <p class="mt-1 text-base font-extrabold text-[#1c2430]">{formatMoney(order.totalAmount)}</p>
            </div>
            <div class="rounded-xl border border-[#dce5f4] bg-white/90 p-3">
              <p class="text-[11px] font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Articoli</p>
              <p class="mt-1 text-base font-extrabold text-[#1c2430]">{order.items.length}</p>
            </div>
            <div class="rounded-xl border border-[#dce5f4] bg-white/90 p-3">
              <p class="text-[11px] font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Pagamento</p>
              <p class="mt-1 text-base font-semibold text-[#1c2430]">{order.paymentMethod}</p>
            </div>
          </div>
        {/if}
      </div>
    </div>

    <div class="border-t border-[#d9dee8] p-4 md:p-5">
      <div class="flex flex-wrap items-center justify-end gap-2">
        <a class="action-btn action-btn-neutral" href="/orders">Torna alla lista</a>
        <button class="action-btn action-btn-neutral" on:click={() => loadOrder(false)} disabled={loading}>
          {loading ? 'Aggiornamento...' : 'Aggiorna'}
        </button>
        {#if order}
          <button class="action-btn action-btn-neutral" on:click={copyOrderId}>
            {copied ? 'ID copiato' : 'Copia Order ID'}
          </button>
          <a
            class="action-btn action-btn-store"
            href={`http://localhost:3000/orders/${order.id}`}
            target="_blank"
            rel="noreferrer"
          >
            Apri pagina store
          </a>
          {#if canWrite && canCancel}
            <button class="action-btn action-btn-danger" on:click={cancelManually} disabled={actionLoading}>
              {actionLoading ? 'Elaborazione...' : 'Annulla'}
            </button>
          {/if}
          {#if canWrite && canComplete}
            <button class="action-btn action-btn-success" on:click={completeManually} disabled={actionLoading}>
              {actionLoading ? 'Elaborazione...' : 'Completa'}
            </button>
          {/if}
        {/if}
      </div>
      {#if !canWrite}
        <p class="mt-3 rounded-lg bg-amber-50 px-3 py-2 text-sm text-amber-700">
          Permesso mancante: orders:write. Azioni di workflow in sola lettura.
        </p>
      {/if}
    </div>
  </section>

  {#if loading && !order && !error}
    <section class="surface-card p-5">
      <div class="h-36 animate-pulse rounded-xl bg-[#edf2fb]"></div>
    </section>
  {/if}

  {#if error}
    <section class="surface-card p-5">
      <p class="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700">{error}</p>
    </section>
  {/if}

  {#if actionError}
    <section class="surface-card p-5">
      <p class="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700">{actionError}</p>
    </section>
  {/if}

  {#if order}
    <section class="grid gap-6 xl:grid-cols-[1.3fr_1fr]">
      <div class="surface-card p-5">
        <h2 class="text-2xl font-bold text-[#1c2430]">Identita e stato ordine</h2>
        <div class="mt-4 grid gap-3 md:grid-cols-2">
          <div class="rounded-xl border border-[#d9dee8] bg-[#fcfdff] p-4">
            <p class="text-xs font-semibold uppercase tracking-[0.16em] text-[#5a6472]">Order ID</p>
            <p class="mt-1 break-all font-mono text-xs text-[#1c2430]">{order.id}</p>
          </div>
          <div class="rounded-xl border border-[#d9dee8] bg-[#fcfdff] p-4">
            <p class="text-xs font-semibold uppercase tracking-[0.16em] text-[#5a6472]">Cart ID</p>
            <p class="mt-1 break-all font-mono text-xs text-[#1c2430]">{order.cartId}</p>
          </div>
          <div class="rounded-xl border border-[#d9dee8] bg-[#fcfdff] p-4">
            <p class="text-xs font-semibold uppercase tracking-[0.16em] text-[#5a6472]">User ID</p>
            <p class="mt-1 break-all font-mono text-xs text-[#1c2430]">{order.userId}</p>
          </div>
          <div class="rounded-xl border border-[#d9dee8] bg-[#fcfdff] p-4">
            <p class="text-xs font-semibold uppercase tracking-[0.16em] text-[#5a6472]">Identity</p>
            <p class="mt-1 text-sm font-semibold text-[#1c2430]">{order.identityType}</p>
          </div>
          <div class="rounded-xl border border-[#d9dee8] bg-[#fcfdff] p-4 md:col-span-2">
            <p class="text-xs font-semibold uppercase tracking-[0.16em] text-[#5a6472]">Identity ID</p>
            <p class="mt-1 break-all font-mono text-xs text-[#1c2430]">
              {order.identityType === 'Registered'
                ? (order.authenticatedUserId ?? '-')
                : (order.anonymousId ?? '-')}
            </p>
          </div>
        </div>

        {#if order.failureReason}
          <div class="mt-4 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
            <p class="font-semibold">Motivo errore workflow</p>
            <p class="mt-1">{order.failureReason}</p>
          </div>
        {/if}
      </div>

      <div class="space-y-4">
        <div class="surface-card p-5">
          <h3 class="text-lg font-bold text-[#1c2430]">Tracking e pagamento</h3>
          <div class="mt-4 space-y-3">
            <div class="rounded-xl border border-[#d9dee8] bg-[#fcfdff] p-3">
              <p class="text-xs font-semibold uppercase tracking-[0.16em] text-[#5a6472]">Stato spedizione</p>
              <p class="mt-1">
                <span class={`rounded-full border px-2.5 py-1 text-xs font-semibold ${getShippingStatusBadgeClass(order)}`}>
                  {getShippingStatusLabel(order)}
                </span>
              </p>
            </div>
            <div class="rounded-xl border border-[#d9dee8] bg-[#fcfdff] p-3">
              <p class="text-xs font-semibold uppercase tracking-[0.16em] text-[#5a6472]">Tracking code</p>
              <p class="mt-1 break-all font-mono text-xs text-[#1c2430]">{order.trackingCode ?? '-'}</p>
            </div>
            <div class="rounded-xl border border-[#d9dee8] bg-[#fcfdff] p-3">
              <p class="text-xs font-semibold uppercase tracking-[0.16em] text-[#5a6472]">Transaction ID</p>
              <p class="mt-1 break-all font-mono text-xs text-[#1c2430]">{order.transactionId ?? '-'}</p>
            </div>
          </div>
        </div>
      </div>
    </section>

    <section class="surface-card p-5">
      <h2 class="text-2xl font-bold text-[#1c2430]">Dati cliente e indirizzi</h2>
      <div class="mt-4 grid gap-3 lg:grid-cols-3">
        <div class="rounded-xl border border-[#d9dee8] bg-[#fcfdff] p-4 text-sm text-[#1c2430]">
          <p class="text-xs font-semibold uppercase tracking-[0.16em] text-[#5a6472]">Cliente</p>
          <p class="mt-2 text-base font-semibold">{order.customer.firstName} {order.customer.lastName}</p>
          <p class="mt-1 break-all">{order.customer.email}</p>
          <p class="mt-1">{order.customer.phone || '-'}</p>
        </div>
        <div class="rounded-xl border border-[#d9dee8] bg-[#fcfdff] p-4 text-sm text-[#1c2430]">
          <p class="text-xs font-semibold uppercase tracking-[0.16em] text-[#5a6472]">Indirizzo spedizione</p>
          <p class="mt-2">{order.shippingAddress.street}</p>
          <p>{order.shippingAddress.postalCode} {order.shippingAddress.city}</p>
          <p>{order.shippingAddress.country}</p>
        </div>
        <div class="rounded-xl border border-[#d9dee8] bg-[#fcfdff] p-4 text-sm text-[#1c2430]">
          <p class="text-xs font-semibold uppercase tracking-[0.16em] text-[#5a6472]">Indirizzo fatturazione</p>
          <p class="mt-2">{order.billingAddress.street}</p>
          <p>{order.billingAddress.postalCode} {order.billingAddress.city}</p>
          <p>{order.billingAddress.country}</p>
        </div>
      </div>
    </section>

    <section class="surface-card overflow-hidden p-5">
      <div class="mb-4 flex items-center justify-between gap-2">
        <h2 class="text-2xl font-bold text-[#1c2430]">Righe ordine</h2>
        <p class="text-sm text-[#5a6472]">{order.items.length} prodotti</p>
      </div>
      <div class="mt-4 overflow-x-auto">
        <table class="w-full min-w-[980px] text-left text-sm">
          <thead>
            <tr class="border-y border-[#d9dee8] bg-[#f8fbff] text-[#5a6472]">
              <th class="px-3 py-3 font-semibold">Product ID</th>
              <th class="px-3 py-3 font-semibold">SKU</th>
              <th class="px-3 py-3 font-semibold">Nome</th>
              <th class="px-3 py-3 font-semibold">Qta</th>
              <th class="px-3 py-3 font-semibold">Prezzo unitario</th>
              <th class="px-3 py-3 font-semibold">Subtotale</th>
            </tr>
          </thead>
          <tbody>
            {#each order.items as item}
              <tr class="border-b border-[#edf1f7] transition hover:bg-[#fafcff]">
                <td class="px-3 py-3 font-mono text-xs">{item.productId}</td>
                <td class="px-3 py-3 font-mono text-xs">{item.sku}</td>
                <td class="px-3 py-3">{item.name}</td>
                <td class="px-3 py-3">{item.quantity}</td>
                <td class="px-3 py-3">{formatMoney(item.unitPrice)}</td>
                <td class="px-3 py-3 font-semibold text-[#1c2430]">{formatMoney(item.quantity * item.unitPrice)}</td>
              </tr>
            {:else}
              <tr>
                <td class="empty-row" colspan="6">
                  <div class="empty-box">
                    <p class="empty-title">Nessuna riga ordine disponibile</p>
                    <p class="empty-description">Questo ordine non contiene ancora articoli.</p>
                  </div>
                </td>
              </tr>
            {/each}
          </tbody>
          <tfoot>
            <tr class="border-t border-[#d9dee8] bg-[#f8fbff]">
              <td class="px-3 py-3 text-right text-sm font-semibold text-[#1c2430]" colspan="5">Totale ordine</td>
              <td class="px-3 py-3 text-sm font-bold text-[#1c2430]">{formatMoney(order.totalAmount)}</td>
            </tr>
          </tfoot>
        </table>
      </div>
    </section>
  {/if}
</div>

<style>
  .action-btn {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    border-radius: 0.85rem;
    padding: 0.62rem 0.95rem;
    font-size: 0.84rem;
    font-weight: 700;
    line-height: 1;
    border: 1px solid transparent;
    transition: transform 120ms ease, box-shadow 160ms ease, filter 160ms ease, background-color 160ms ease;
  }

  .action-btn:hover {
    transform: translateY(-1px);
  }

  .action-btn:disabled {
    cursor: not-allowed;
    transform: none;
    opacity: 0.6;
  }

  .action-btn-neutral {
    border-color: #d5ddea;
    background: linear-gradient(180deg, #ffffff 0%, #f7faff 100%);
    color: #233147;
  }

  .action-btn-neutral:hover {
    background: linear-gradient(180deg, #ffffff 0%, #eef4ff 100%);
  }

  .action-btn-store {
    background: linear-gradient(135deg, #0b5fff 0%, #1f7bff 100%);
    color: #ffffff;
  }

  .action-btn-store:hover {
    filter: brightness(1.03);
  }

  .action-btn-success {
    background: linear-gradient(135deg, #0c8f6b 0%, #0a7962 100%);
    color: #ffffff;
  }

  .action-btn-success:hover {
    filter: brightness(1.04);
  }

  .action-btn-danger {
    background: linear-gradient(135deg, #d43838 0%, #bc2727 100%);
    color: #ffffff;
  }

  .action-btn-danger:hover {
    filter: brightness(1.03);
  }
</style>
