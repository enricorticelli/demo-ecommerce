<script lang="ts">
  import { onMount, onDestroy } from 'svelte';
  import { fetchOrder, type OrderView } from '../lib/api';
  import { getProductImage } from '../lib/catalog-presenter';
  import { formatCurrency } from '../lib/format';

  export let orderId: string;

  let order: OrderView | null = null;
  let isLoading = true;
  let notFound = false;
  let loadError = '';
  let pollingActive = true;
  let pollAttempts = 0;
  const maxPoll = 50;

  type StatusInfo = {
    label: string;
    color: string;
    background: string;
    description: string;
  };

  const statusMap: Record<string, StatusInfo> = {
    Pending: {
      label: 'In attesa',
      color: 'text-amber-700',
      background: 'bg-amber-50 border-amber-200',
      description: 'Ordine ricevuto. In elaborazione iniziale.',
    },
    Processing: {
      label: 'In elaborazione',
      color: 'text-sky-700',
      background: 'bg-sky-50 border-sky-200',
      description: 'Stiamo verificando disponibilita e pagamento.',
    },
    Completed: {
      label: 'Completato',
      color: 'text-emerald-700',
      background: 'bg-emerald-50 border-emerald-200',
      description: 'Ordine confermato e pronto per la spedizione.',
    },
    Failed: {
      label: 'Non completato',
      color: 'text-rose-700',
      background: 'bg-rose-50 border-rose-200',
      description: 'Si è verificato un problema durante l\'elaborazione.',
    },
  };

  const pipelineSteps = ['Ricevuto', 'Magazzino', 'Pagamento', 'Spedizione'];

  $: statusInfo = order
    ? statusMap[order.status] ?? {
        label: order.status,
        color: 'text-[#202223]',
        background: 'bg-white border-[#e1e3e5]',
        description: 'Stato ordine in aggiornamento.',
      }
    : null;

  $: isDone = order?.status === 'Completed' || order?.status === 'Failed';

  $: pipelineStep = order
    ? order.status === 'Completed'
      ? 4
      : order.status === 'Failed'
        ? -1
        : order.status === 'Processing'
          ? 1
          : 0
    : 0;

  async function poll() {
    while (pollingActive && pollAttempts < maxPoll) {
      try {
        order = await fetchOrder(orderId);
        if (order.status === 'Completed' || order.status === 'Failed') {
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

      pollAttempts += 1;
      await new Promise((resolve) => setTimeout(resolve, 1200));
    }
  }

  onMount(async () => {
    isLoading = true;

    try {
      order = await fetchOrder(orderId);
      isLoading = false;
      if (!isDone) await poll();
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
  <div>
    <a href="/" class="text-sm text-[#616161] hover:text-[#202223]">← Torna al catalogo</a>
    <h1 class="mt-2 font-title text-4xl font-extrabold text-[#202223]">Tracciamento ordine</h1>
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
    {#if statusInfo}
      <div class="surface-card border p-5 {statusInfo.background}">
        <p class="text-xs font-semibold uppercase tracking-[0.2em] text-[#6d7175]">Stato ordine</p>
        <p class="mt-2 text-2xl font-bold {statusInfo.color}">{statusInfo.label}</p>
        <p class="mt-1 text-sm text-[#4a4f55]">{statusInfo.description}</p>
        {#if order.failureReason}
          <p class="mt-2 text-xs text-rose-700">Motivo: {order.failureReason}</p>
        {/if}
        {#if !isDone}
          <p class="mt-2 text-xs text-[#008060]">Aggiornamento live attivo</p>
        {/if}
      </div>
    {/if}

    <div class="surface-card p-5">
      <h2 class="text-sm font-semibold uppercase tracking-[0.2em] text-[#6d7175]">Pipeline</h2>
      <div class="mt-4 grid gap-3 sm:grid-cols-4">
        {#each pipelineSteps as label, idx}
          {@const done = pipelineStep > idx || order.status === 'Completed'}
          {@const active = pipelineStep === idx && !isDone}
          <div class="rounded-xl border p-3 text-center" class:border-[#008060]={done || active} class:bg-[#f1f8f5]={done || active} class:border-[#e1e3e5]={!(done || active)}>
            <div class="mx-auto mb-2 flex h-7 w-7 items-center justify-center rounded-full text-xs font-bold"
              class:bg-[#008060]={done}
              class:bg-[#36a38b]={active}
              class:text-white={done || active}
              class:bg-[#ecf0f1]={!(done || active)}
              class:text-[#6d7175]={!(done || active)}
            >
              {#if done}✓{:else if active}…{:else}{idx + 1}{/if}
            </div>
            <p class="text-sm font-semibold text-[#202223]">{label}</p>
          </div>
        {/each}
      </div>
    </div>

    <div class="grid gap-4 md:grid-cols-2">
      <div class="surface-card p-5 text-sm">
        <h3 class="font-semibold uppercase tracking-[0.18em] text-[#6d7175]">Dettagli ordine</h3>
        <dl class="mt-3 space-y-2">
          <div class="flex justify-between gap-2">
            <dt class="text-[#616161]">Order ID</dt>
            <dd class="max-w-[220px] truncate font-mono text-xs text-[#202223]">{order.id}</dd>
          </div>
          <div class="flex justify-between gap-2">
            <dt class="text-[#616161]">Totale</dt>
            <dd class="font-bold text-[#202223]">{formatCurrency(order.totalAmount)}</dd>
          </div>
          {#if order.transactionId}
            <div class="flex justify-between gap-2">
              <dt class="text-[#616161]">Transaction ID</dt>
              <dd class="max-w-[220px] truncate font-mono text-xs text-[#202223]">{order.transactionId}</dd>
            </div>
          {/if}
          {#if order.trackingCode}
            <div class="flex justify-between gap-2">
              <dt class="text-[#616161]">Tracking</dt>
              <dd class="font-mono text-xs font-bold text-[#008060]">{order.trackingCode}</dd>
            </div>
          {/if}
        </dl>
      </div>

      <div class="surface-card p-5 text-sm">
        <h3 class="font-semibold uppercase tracking-[0.18em] text-[#6d7175]">Spedizione</h3>
        <ul class="mt-3 space-y-2 text-[#4a4f55]">
          <li>Corriere espresso tracciato</li>
          <li>Consegna stimata in 2-3 giorni lavorativi</li>
          <li>Notifica email al cambio stato</li>
        </ul>
      </div>
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
                <p class="text-xs text-[#8c9196]">SKU {item.sku} · Quantita {item.quantity}</p>
              </div>
              <p class="font-semibold text-[#202223]">{formatCurrency(item.quantity * item.unitPrice)}</p>
            </div>
          {/each}
        </div>
      </div>
    {/if}

    <a href="/" class="btn-primary w-fit">Continua lo shopping</a>
  {/if}
</div>
