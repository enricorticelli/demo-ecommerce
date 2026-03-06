<script lang="ts">
  import { onMount } from 'svelte';
  import {
    authorizePaymentSession,
    getPaymentSessionById,
    rejectPaymentSession,
    type PaymentSession,
  } from '../lib/api';
  import { formatCurrency } from '../lib/format';

  export let sessionId: string;
  export let orderId: string;

  let session: PaymentSession | null = null;
  let isLoading = true;
  let isSubmitting = false;
  let error = '';

  async function loadSession() {
    isLoading = true;
    error = '';

    try {
      session = await getPaymentSessionById(sessionId);
      if (!session) {
        error = 'Sessione pagamento non trovata.';
      }
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore nel caricamento sessione pagamento.';
    } finally {
      isLoading = false;
    }
  }

  async function confirmPayment() {
    isSubmitting = true;
    error = '';

    try {
      await authorizePaymentSession(sessionId);
      window.location.href = `/orders/${orderId}`;
    } catch (err) {
      error = err instanceof Error ? err.message : 'Autorizzazione pagamento non riuscita.';
      isSubmitting = false;
    }
  }

  async function cancelPayment() {
    isSubmitting = true;
    error = '';

    try {
      await rejectPaymentSession(sessionId, 'Payment cancelled by customer');
      window.location.href = `/orders/${orderId}`;
    } catch (err) {
      error = err instanceof Error ? err.message : 'Annullamento pagamento non riuscito.';
      isSubmitting = false;
    }
  }

  onMount(loadSession);
</script>

<div class="mx-auto max-w-2xl space-y-6 reveal">
  <div class="text-center">
    <p class="text-xs font-semibold uppercase tracking-[0.2em] text-[#8c9196]">Hosted Payment Gateway</p>
    <h1 class="mt-2 font-title text-4xl font-extrabold text-[#202223]">Conferma pagamento</h1>
    <p class="mt-2 text-sm text-[#616161]">Flusso simulato con redirect, sostituibile con provider reale S2S.</p>
  </div>

  <div class="surface-card p-6">
    {#if isLoading}
      <p class="text-sm text-[#616161]">Caricamento sessione pagamento...</p>
    {:else if error}
      <p class="rounded-xl border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700">{error}</p>
    {:else if session}
      <div class="space-y-5">
        <div class="surface-muted space-y-2 p-4 text-sm text-[#4a4f55]">
          <div class="flex justify-between"><span>Sessione</span><span class="font-mono">{session.sessionId}</span></div>
          <div class="flex justify-between"><span>Ordine</span><span class="font-mono">{session.orderId}</span></div>
          <div class="flex justify-between"><span>Utente</span><span class="font-mono">{session.userId}</span></div>
          <div class="flex justify-between text-base font-bold text-[#202223]"><span>Importo</span><span>{formatCurrency(session.amount)}</span></div>
        </div>

        <div class="rounded-xl border border-[#d0ebe4] bg-[#f1f8f5] px-4 py-3 text-xs text-[#005940]">
          Questa e una pagina di pagamento hosted. In produzione verra sostituita da un PSP esterno con callback server-to-server.
        </div>

        <div class="grid gap-3 sm:grid-cols-2">
          <button class="btn-secondary" on:click={cancelPayment} disabled={isSubmitting}>Rifiuta pagamento</button>
          <button class="btn-primary" on:click={confirmPayment} disabled={isSubmitting}>
            {isSubmitting ? 'Elaborazione...' : 'Autorizza pagamento'}
          </button>
        </div>
      </div>
    {/if}
  </div>
</div>
