<script lang="ts">
  import { onMount, onDestroy } from 'svelte';
  import { fetchOrder, fetchShipmentByOrder, manualCancelOrder, type OrderView, type ShipmentView } from '../lib/api';
  import { getProductImage } from '../lib/catalog-presenter';
  import { formatCurrency } from '../lib/format';
  import { cartId, startNewCart, userId } from '../stores/cart';

  export let orderId: string;

  let order: OrderView | null = null;
  let shipment: ShipmentView | null = null;
  let isLoading = true;
  let notFound = false;
  let loadError = '';
  let pollingActive = true;
  let isCancelling = false;
  let cancelError = '';
  let cancelSuccess = '';
  const FAST_POLL_MS = 1200;
  const TERMINAL_ORDER_POLL_MS = 5000;
  const HIDDEN_POLL_MS = 10000;

  const paymentMethodLabels: Record<string, string> = {
    stripe_card: 'Carta di credito',
    paypal: 'PayPal',
    satispay: 'Satispay',
  };
  const shipmentStatusLabels: Record<string, string> = {
    Preparing: 'In lavorazione',
    Created: 'Spedizione avviata',
    InTransit: 'In transito',
    Delivered: 'Consegnata',
    Cancelled: 'Annullata',
  };
  const shippingTrackingSteps = [
    'Ordine confermato',
    'Spedizione avviata',
    'Preparazione pacco',
    'In transito',
    'Consegnata',
  ] as const;

  const orderStatusLabels: Record<string, string> = {
    Pending: 'In attesa',
    StockReserved: 'Stock riservato',
    Processing: 'In lavorazione',
    PaymentAuthorized: 'Pagamento autorizzato',
    Completed: 'Completato',
    Failed: 'Non completato',
  };

  function orderStatusCardClass(status: string | undefined): string {
    if (status === 'Completed') return 'border-emerald-200 bg-emerald-50/60';
    if (status === 'Failed') return 'border-rose-200 bg-rose-50/60';
    if (status === 'Processing' || status === 'StockReserved') return 'border-sky-200 bg-sky-50/60';
    if (status === 'PaymentAuthorized') return 'border-indigo-200 bg-indigo-50/60';
    if (status === 'Pending') return 'border-amber-200 bg-amber-50/60';
    return 'border-[#e1e3e5] bg-white';
  }

  $: isDone = order?.status === 'Completed' || order?.status === 'Failed';
  $: shippingInProcessing = shipment?.status === 'Preparing' || shipment?.status === 'InTransit' || shipment?.status === 'Delivered';
  $: canCustomerCancel = !!order && !shippingInProcessing && order.status !== 'Failed' && shipment?.status !== 'Cancelled';
  $: orderStatusLabel = order ? orderStatusLabels[order.status] ?? order.status : '-';
  $: orderStatusSupportText = !order
    ? ''
    : order.status === 'Completed'
      ? 'Ordine completato correttamente.'
      : order.status === 'Failed'
        ? 'Ordine non completato.'
        : canCustomerCancel
          ? 'Puoi ancora annullare questo ordine.'
          : 'Ordine in lavorazione: annullamento non disponibile.';
  $: itemsSubtotal = order
    ? order.items.reduce((sum, item) => sum + item.quantity * item.unitPrice, 0)
    : 0;
  $: paymentMethodLabel = order ? paymentMethodLabels[order.paymentMethod] ?? order.paymentMethod : '-';
  $: trackingCode = shipment?.trackingCode || order?.trackingCode || '';
  $: shippingIsCancelled = shipment?.status === 'Cancelled';
  $: shippingStep = shipment
    ? shipment.status === 'Delivered'
      ? 5
      : shipment.status === 'InTransit'
        ? 4
        : shipment.status === 'Preparing'
          ? 3
          : shipment.status === 'Created'
            ? 2
            : 1
    : 1;
  $: shippingLastUpdate = shipment?.updatedAtUtc
    ? new Date(shipment.updatedAtUtc).toLocaleString('it-IT', { dateStyle: 'medium', timeStyle: 'short' })
    : '';

  function isTerminalOrderStatus(status: string | undefined): boolean {
    return status === 'Completed' || status === 'Failed';
  }

  function isTerminalShipmentStatus(status: string | undefined): boolean {
    return status === 'Delivered' || status === 'Cancelled';
  }

  function shouldContinuePolling(): boolean {
    if (!order) return true;
    if (!isTerminalOrderStatus(order.status)) return true;

    // Keep refreshing after order completion to track shipment progression.
    if (order.status === 'Completed') {
      return !isTerminalShipmentStatus(shipment?.status);
    }

    return false;
  }

  function getPollDelayMs(): number {
    if (document.hidden) {
      return HIDDEN_POLL_MS;
    }

    if (order && isTerminalOrderStatus(order.status)) {
      return TERMINAL_ORDER_POLL_MS;
    }

    return FAST_POLL_MS;
  }

  async function cancelOrderByCustomer() {
    if (!order || !canCustomerCancel || isCancelling) return;

    const confirmed = window.confirm('Confermi l\'annullamento dell\'ordine?');
    if (!confirmed) return;

    isCancelling = true;
    cancelError = '';
    cancelSuccess = '';

    try {
      await manualCancelOrder(order.id, 'Cancelled by customer from storefront', { anonymousId: $userId });
      order = await fetchOrder(orderId, { anonymousId: $userId });
      shipment = await fetchShipmentByOrder(orderId, { anonymousId: $userId });
      cancelSuccess = 'Richiesta di annullamento inviata correttamente.';
    } catch (err) {
      cancelError = err instanceof Error ? err.message : 'Annullamento non riuscito. Riprova.';
    } finally {
      isCancelling = false;
    }
  }

  async function poll() {
    while (pollingActive) {
      try {
        order = await fetchOrder(orderId, { anonymousId: $userId });
        if (order.status === 'Completed' && order.cartId === cartId.get()) {
          startNewCart();
        }
        shipment = await fetchShipmentByOrder(orderId, { anonymousId: $userId });
        if (!shouldContinuePolling()) {
          pollingActive = false;
          break;
        }
      } catch (err) {
        if (err instanceof Error && err.name === 'NotFoundError') {
          notFound = true;
          pollingActive = false;
          break;
        }
      }

      await new Promise((resolve) => setTimeout(resolve, getPollDelayMs()));
    }
  }

  onMount(async () => {
    isLoading = true;

    try {
      order = await fetchOrder(orderId, { anonymousId: $userId });
      if (order.status === 'Completed' && order.cartId === cartId.get()) {
        startNewCart();
      }
      shipment = await fetchShipmentByOrder(orderId, { anonymousId: $userId });
      isLoading = false;
      if (shouldContinuePolling()) await poll();
    } catch (err) {
      if (err instanceof Error && err.name === 'NotFoundError') {
        notFound = true;
      } else {
        loadError = 'Impossibile recuperare lo stato dell\'ordine.';
      }
      isLoading = false;
    }
  });

  onDestroy(() => {
    pollingActive = false;
  });
</script>

<div class="space-y-6 reveal">
  <div class="flex flex-wrap items-end justify-between gap-3">
    <div>
      <a href="/" class="text-sm text-[#616161] hover:text-[#202223]">← Torna al catalogo</a>
      <h1 class="mt-2 font-title text-4xl font-extrabold text-[#202223]">Tracciamento ordine</h1>
    </div>

    {#if !isLoading && !notFound && !loadError && order && canCustomerCancel}
      <button
        class="inline-flex items-center justify-center rounded-xl bg-rose-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-rose-700 disabled:cursor-not-allowed disabled:bg-rose-200 disabled:text-rose-500"
        disabled={isCancelling}
        on:click={cancelOrderByCustomer}
      >
        {isCancelling ? 'Annullamento in corso...' : 'Annulla ordine'}
      </button>
    {/if}
  </div>

  {#if isLoading}
    <div class="space-y-3">
      <div class="surface-card h-28 animate-pulse"></div>
      <div class="surface-card h-48 animate-pulse"></div>
    </div>
  {:else if notFound}
    <div class="surface-card p-12 text-center">
      <h2 class="font-title text-2xl font-bold text-[#202223]">Ordine non trovato</h2>
      <p class="mt-2 text-sm text-[#616161]">Verifica l'identificativo e riprova.</p>
      <a href="/" class="btn-secondary mt-5">Torna allo store</a>
    </div>
  {:else if loadError}
    <div class="surface-card border-rose-200 bg-rose-50 p-5 text-sm text-rose-700">{loadError}</div>
  {:else if order}
    {#if cancelSuccess}
      <p class="text-right text-xs text-emerald-700">{cancelSuccess}</p>
    {/if}
    {#if cancelError}
      <p class="text-right text-xs text-rose-700">{cancelError}</p>
    {/if}

    <div class="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
      <div class={`surface-card border p-5 ${orderStatusCardClass(order.status)}`}>
        <p class="text-xs font-semibold uppercase tracking-[0.2em] text-[#6d7175]">Stato ordine</p>
        <p class="mt-2 text-xl font-bold text-[#202223]">{orderStatusLabel}</p>
        <p class="mt-1 text-sm text-[#4a4f55]">{orderStatusSupportText}</p>

        <p class="mt-3 text-xs text-[#8c9196]">Codice ordine</p>
        <p class="font-mono text-xs text-[#4a4f55] break-all">{order.id}</p>

        {#if order.failureReason}
          <p class="mt-2 text-xs text-rose-700">Motivo: {order.failureReason}</p>
        {/if}
      </div>

      <div class="surface-card border p-5">
        <p class="text-xs font-semibold uppercase tracking-[0.2em] text-[#6d7175]">Pagamento</p>
        <div class="mt-2 grid gap-4 md:grid-cols-2">
          <div>
            <p class="text-base font-bold text-[#202223]">{paymentMethodLabel}</p>
            <p class="mt-1 text-sm text-[#4a4f55]">Totale: {formatCurrency(order.totalAmount)}</p>
            {#if order.transactionId}
              <p class="mt-2 text-xs text-[#8c9196]">Transazione: <span class="font-mono text-[#202223]">{order.transactionId}</span></p>
            {/if}
          </div>
          <div>
            <p class="text-xs font-semibold uppercase tracking-[0.14em] text-[#6d7175]">Fatturazione</p>
            <p class="mt-1 text-sm text-[#4a4f55]">{order.billingAddress.street}</p>
            <p class="text-sm text-[#4a4f55]">{order.billingAddress.postalCode} {order.billingAddress.city}, {order.billingAddress.country}</p>
          </div>
        </div>
      </div>

      <div class="surface-card border p-5">
        <p class="text-xs font-semibold uppercase tracking-[0.2em] text-[#6d7175]">Spedizione</p>
        <p class="mt-2 text-base font-bold text-[#202223]">{order.customer.firstName} {order.customer.lastName}</p>
        <p class="mt-1 text-sm text-[#4a4f55]">{order.shippingAddress.street}</p>
        <p class="text-sm text-[#4a4f55]">{order.shippingAddress.postalCode} {order.shippingAddress.city}, {order.shippingAddress.country}</p>
        <p class="mt-2 text-xs text-[#8c9196]">{order.customer.email}{order.customer.phone ? ` · ${order.customer.phone}` : ''}</p>
      </div>
    </div>

    <div class="surface-card p-5">
      <div class="flex flex-wrap items-center justify-between gap-3">
        <h2 class="font-semibold uppercase tracking-[0.18em] text-[#6d7175]">Tracking spedizione</h2>
      </div>

      {#if shippingIsCancelled}
        <p class="mt-3 rounded-xl border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700">
          Spedizione annullata. Contatta il supporto per maggiori dettagli.
        </p>
      {:else}
        <div class="mt-4 w-full overflow-x-auto pb-1">
          <div class="min-w-[760px] overflow-hidden rounded-xl border border-[#e1e3e5] bg-white">
            <div class="grid grid-cols-5">
              {#each shippingTrackingSteps as step, idx}
                {@const stepNumber = idx + 1}
                {@const done = shippingStep > stepNumber}
                {@const active = shippingStep === stepNumber}
                <div
                  class="flex min-h-[58px] items-center justify-between gap-2 px-3 py-2"
                  class:border-r={idx < shippingTrackingSteps.length - 1}
                  class:border-[#e1e3e5]={idx < shippingTrackingSteps.length - 1}
                  class:bg-emerald-50={done}
                  class:bg-sky-50={active}
                >
                  <div class="flex min-w-0 items-center gap-2">
                    <span
                      class="inline-flex h-6 w-6 items-center justify-center rounded-md border text-[11px] font-bold"
                      class:border-emerald-300={done}
                      class:bg-emerald-100={done}
                      class:text-emerald-800={done}
                      class:border-sky-300={active}
                      class:bg-sky-100={active}
                      class:text-sky-800={active}
                      class:border-[#d5d9dd]={!(done || active)}
                      class:text-[#6d7175]={!(done || active)}
                    >
                      {stepNumber}
                    </span>
                    <p class="truncate text-sm font-semibold leading-tight text-[#202223]">{step}</p>
                  </div>
                  <div class="shrink-0 text-sm" title={done ? 'Completato' : active ? 'Attuale' : 'In attesa'}>
                    {#if done}
                      <span class="text-emerald-700">✓</span>
                    {:else if active}
                      <span class="text-sky-700">●</span>
                    {:else}
                      <span class="text-[#b7bcc3]">○</span>
                    {/if}
                  </div>
                </div>
              {/each}
            </div>
          </div>
        </div>
      {/if}

      {#if trackingCode}
        <p class="mt-4 text-sm text-[#4a4f55]">
          Codice tracking: <span class="font-mono font-bold text-[#008060]">{trackingCode}</span>
        </p>
      {:else}
        <p class="mt-4 text-sm text-[#4a4f55]">
          Codice tracking non ancora disponibile.
        </p>
      {/if}

      {#if shippingLastUpdate}
        <p class="mt-1 text-xs text-[#8c9196]">Ultimo aggiornamento: {shippingLastUpdate}</p>
      {/if}
    </div>

    {#if order.items && order.items.length > 0}
      <div class="surface-card p-5">
        <h3 class="font-semibold uppercase tracking-[0.18em] text-[#6d7175]">Articoli ordinati</h3>
        <div class="mt-3 space-y-3">
          {#each order.items as item}
            <div class="surface-muted flex items-center gap-3 p-3">
              <img src={getProductImage(item.sku, 96, 72)} alt={item.name} class="h-12 w-16 rounded-lg object-cover" loading="lazy" />
              <div class="flex-1">
                <p class="font-medium text-[#202223]">{item.name}</p>
                <p class="text-xs text-[#8c9196]">{item.sku} · Quantita {item.quantity}</p>
              </div>
              <p class="font-semibold text-[#202223]">{formatCurrency(item.quantity * item.unitPrice)}</p>
            </div>
          {/each}
        </div>

        <div class="mt-4 border-t border-[#e1e3e5] pt-4">
          <h4 class="text-xs font-semibold uppercase tracking-[0.16em] text-[#6d7175]">Resoconto totali</h4>
          <div class="mt-2 space-y-1 text-sm text-[#4a4f55]">
            <div class="flex items-center justify-between gap-3">
              <span>Subtotale articoli</span>
              <span class="font-medium text-[#202223]">{formatCurrency(itemsSubtotal)}</span>
            </div>
            <div class="flex items-center justify-between gap-3 border-t border-[#eef1f3] pt-2 text-base font-bold text-[#202223]">
              <span>Totale ordine</span>
              <span>{formatCurrency(order.totalAmount)}</span>
            </div>
          </div>
        </div>
      </div>
    {/if}

    <a href="/search" class="btn-primary w-fit">Continua lo shopping</a>
  {/if}
</div>
