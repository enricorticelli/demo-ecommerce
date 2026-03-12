<script lang="ts">
  import { onMount } from 'svelte';
  import { fetchOrder, getPaymentSessionById, type OrderView, type PaymentSession } from '../lib/api';
  import { formatCurrency } from '../lib/format';
  import { userId } from '../stores/cart';

  export let sessionId: string;
  export let orderId: string;

  let session: PaymentSession | null = null;
  let order: OrderView | null = null;
  let isLoading = true;
  let error = '';

  function resolveOrderId(): string {
    return orderId || session?.orderId || '';
  }

  async function loadSession() {
    isLoading = true;
    error = '';

    try {
      session = await getPaymentSessionById(sessionId, { anonymousId: $userId });
      if (!session) {
        error = 'Sessione pagamento non trovata.';
      } else {
        order = await fetchOrder(session.orderId, { anonymousId: $userId }).catch(() => null);
      }
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore nel caricamento sessione pagamento.';
    } finally {
      isLoading = false;
    }
  }

  function goToReturnPage() {
    const targetOrderId = resolveOrderId();
    if (!targetOrderId) {
      error = 'Ordine non associato alla sessione di pagamento.';
      return;
    }

    window.location.href = `/payment/return?orderId=${targetOrderId}&sessionId=${sessionId}`;
  }

  onMount(loadSession);
</script>

<div class="mx-auto max-w-2xl space-y-6 reveal">
  <div class="text-center">
    <p class="text-xs font-semibold uppercase tracking-[0.2em] text-[#8c9196]">Hosted Payment Session</p>
    <h1 class="mt-2 font-title text-4xl font-extrabold text-[#202223]">Sessione pagamento</h1>
    <p class="mt-2 text-sm text-[#616161]">Autorizzazione gestita via callback server-to-server.</p>
  </div>

  <div class="surface-card p-6">
    {#if isLoading}
      <p class="text-sm text-[#616161]">Caricamento sessione pagamento...</p>
    {:else if error}
      <p class="rounded-xl border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700">{error}</p>
    {:else if session}
      <div class="space-y-5">
        <div class="surface-muted space-y-2 p-4 text-sm text-[#4a4f55]">
          <div class="flex justify-between"><span>Provider</span><span class="font-semibold uppercase">{session.providerCode || '-'}</span></div>
          <div class="flex justify-between"><span>Sessione</span><span class="font-mono">{session.sessionId}</span></div>
          <div class="flex justify-between"><span>Ordine</span><span class="font-mono">{session.orderId}</span></div>
          <div class="flex justify-between"><span>Checkout esterno</span><span class="font-mono">{session.externalCheckoutId ?? '-'}</span></div>
          <div class="flex justify-between text-base font-bold text-[#202223]"><span>Importo</span><span>{formatCurrency(order?.totalAmount ?? session.amount)}</span></div>
        </div>

        <div class="rounded-xl border border-[#d0ebe4] bg-[#f1f8f5] px-4 py-3 text-xs text-[#005940]">
          Se hai appena completato il pagamento sul provider esterno, usa il pulsante sotto per verificare lo stato ordine.
        </div>

        <button class="btn-primary w-full" on:click={goToReturnPage}>Verifica stato ordine</button>
      </div>
    {/if}
  </div>
</div>
