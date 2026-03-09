<script lang="ts">
  import { onMount } from 'svelte';
  import { fetchOrder, getPaymentSessionById, pollOrderUntilDone, type OrderView, type PaymentSession } from '../lib/api';
  import { addToast } from '../stores/ui';
  import { startNewCart } from '../stores/cart';

  export let orderId: string;
  export let sessionId: string;
  export let provider: string;
  export let result: string;
  export let webhook: string;

  let session: PaymentSession | null = null;
  let order: OrderView | null = null;
  let loading = true;
  let message = 'Verifica stato pagamento in corso...';
  let error = '';

  async function loadContext() {
    loading = true;
    error = '';

    try {
      if (!orderId) {
        throw new Error('orderId mancante nel ritorno provider.');
      }

      if (sessionId) {
        session = await getPaymentSessionById(sessionId).catch(() => null);
      }

      const terminalOrder = await pollOrderUntilDone(orderId, (updatedOrder) => {
        order = updatedOrder;
      }, 30, 1000);

      if (!terminalOrder) {
        message = 'Pagamento ricevuto, ordine ancora in elaborazione. Riprova tra pochi secondi.';
        return;
      }

      order = terminalOrder;

      if (terminalOrder.status === 'Completed') {
        startNewCart();
        addToast('Pagamento autorizzato e ordine completato.', 'success');
        window.location.href = `/orders/${orderId}`;
        return;
      }

      if (terminalOrder.status === 'Failed') {
        const failure = terminalOrder.failureReason
          ? `Pagamento non completato: ${terminalOrder.failureReason}`
          : 'Pagamento non completato.';
        addToast(failure, 'error');
        message = failure;
        return;
      }

      message = `Ordine in stato ${terminalOrder.status}.`;
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore durante la verifica stato pagamento.';
    } finally {
      loading = false;
    }
  }

  function goToOrder() {
    if (!orderId) return;
    window.location.href = `/orders/${orderId}`;
  }

  function retryCheckout() {
    if (!orderId) {
      window.location.href = '/checkout';
      return;
    }

    window.location.href = `/checkout?payment=cancelled&orderId=${orderId}`;
  }

  onMount(loadContext);
</script>

<div class="mx-auto max-w-2xl space-y-6 reveal">
  <div class="text-center">
    <p class="text-xs font-semibold uppercase tracking-[0.2em] text-[#8c9196]">Payment Return</p>
    <h1 class="mt-2 font-title text-4xl font-extrabold text-[#202223]">Rientro dal provider</h1>
    <p class="mt-2 text-sm text-[#616161]">Provider: {provider || session?.providerCode || '-'} · Esito redirect: {result || 'n/a'} · Webhook: {webhook || 'n/a'}</p>
  </div>

  <div class="surface-card space-y-5 p-6">
    <div class="surface-muted space-y-2 p-4 text-sm text-[#4a4f55]">
      <div class="flex justify-between"><span>Ordine</span><span class="font-mono">{orderId || '-'}</span></div>
      <div class="flex justify-between"><span>Sessione</span><span class="font-mono">{sessionId || session?.sessionId || '-'}</span></div>
      <div class="flex justify-between"><span>Checkout esterno</span><span class="font-mono">{session?.externalCheckoutId ?? '-'}</span></div>
      <div class="flex justify-between"><span>Stato pagamento</span><span class="font-semibold">{session?.status ?? '-'}</span></div>
      <div class="flex justify-between"><span>Stato ordine</span><span class="font-semibold">{order?.status ?? '-'}</span></div>
    </div>

    {#if loading}
      <p class="rounded-xl border border-blue-200 bg-blue-50 px-3 py-2 text-sm text-blue-700">{message}</p>
    {:else if error}
      <p class="rounded-xl border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700">{error}</p>
    {:else}
      <p class="rounded-xl border border-[#d0ebe4] bg-[#f1f8f5] px-3 py-2 text-sm text-[#005940]">{message}</p>
    {/if}

    <div class="grid gap-3 sm:grid-cols-2">
      <button class="btn-secondary" on:click={retryCheckout}>Torna al checkout</button>
      <button class="btn-primary" on:click={goToOrder}>Apri ordine</button>
    </div>
  </div>
</div>
