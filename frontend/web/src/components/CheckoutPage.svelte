<script lang="ts">
  import { onMount } from 'svelte';
  import { createOrder, getPaymentSessionByOrder } from '../lib/api';
  import { getProductImage } from '../lib/catalog-presenter';
  import { formatCurrency } from '../lib/format';
  import { cartId, userId, cartItems, cartTotal, clearCart } from '../stores/cart';
  import { addToast } from '../stores/ui';

  let step: 'shipping' | 'payment' | 'review' = 'shipping';

  let firstName = 'Mario';
  let lastName = 'Rossi';
  let email = 'mario.rossi@example.com';
  let phone = '+39 333 1234567';
  let address = 'Via Roma 1';
  let city = 'Milano';
  let zip = '20100';
  let country = 'Italia';

  let cardName = 'Mario Rossi';
  let cardNumber = '4242 4242 4242 4242';
  let cardExpiry = '12/28';
  let cardCvc = '123';

  let isSubmitting = false;
  let submitError = '';

  $: items = $cartItems;
  $: subtotal = $cartTotal;
  $: shipping = subtotal > 0 ? (subtotal >= 120 ? 0 : 7.9) : 0;
  $: tax = subtotal * 0.22;
  $: total = subtotal + shipping + tax;
  $: isEmpty = items.length === 0;

  const steps = ['Spedizione', 'Pagamento', 'Conferma'];
  const stepIndex: Record<typeof step, number> = { shipping: 0, payment: 1, review: 2 };

  function formatCard(value: string): string {
    const digits = value.replace(/\D/g, '');
    return `•••• •••• •••• ${digits.slice(-4)}`;
  }

  async function placeOrder() {
    isSubmitting = true;
    submitError = '';

    try {
      const result = await createOrder($cartId, $userId);
      clearCart();
      const paymentSession = await getPaymentSessionByOrder(result.orderId);
      if (paymentSession) {
        window.location.href = paymentSession.redirectUrl;
        return;
      }

      window.location.href = `/orders/${result.orderId}`;
    } catch (err) {
      submitError = err instanceof Error ? err.message : 'Errore durante la creazione dell\'ordine.';
      addToast(submitError, 'error');
    } finally {
      isSubmitting = false;
    }
  }

  onMount(() => {
    if ($cartItems.length === 0) {
      window.location.href = '/cart';
    }
  });
</script>

<div class="space-y-6 reveal">
  <div>
    <a href="/cart" class="text-sm text-[#616161] hover:text-[#202223]">← Torna al carrello</a>
    <h1 class="mt-2 font-title text-4xl font-extrabold text-[#202223]">Checkout</h1>
  </div>

  <nav class="surface-card flex items-center gap-2 p-3 text-sm" aria-label="Passaggi checkout">
    {#each steps as label, idx}
      <div class="flex items-center gap-2">
        <span
          class="flex h-7 w-7 items-center justify-center rounded-full text-xs font-bold"
          class:bg-[#008060]={stepIndex[step] >= idx}
          class:text-white={stepIndex[step] >= idx}
          class:bg-[#ecf0f1]={stepIndex[step] < idx}
          class:text-[#6d7175]={stepIndex[step] < idx}
        >
          {stepIndex[step] > idx ? '✓' : idx + 1}
        </span>
        <span class:text-[#202223]={stepIndex[step] === idx} class:text-[#8c9196]={stepIndex[step] !== idx}>{label}</span>
      </div>
      {#if idx < steps.length - 1}
        <div class="h-px flex-1 bg-[#e1e3e5]"></div>
      {/if}
    {/each}
  </nav>

  {#if isEmpty}
    <div class="surface-card p-10 text-center">
      <p class="text-sm text-[#616161]">Il carrello e vuoto.</p>
      <a href="/" class="btn-secondary mt-4">Torna al catalogo</a>
    </div>
  {:else}
    <div class="grid gap-6 lg:grid-cols-[minmax(0,1fr)_340px]">
      <div>
        {#if step === 'shipping'}
          <form class="surface-card space-y-4 p-6" on:submit|preventDefault={() => (step = 'payment')}>
            <h2 class="font-title text-2xl font-bold text-[#202223]">Dati di spedizione</h2>

            <div class="grid gap-4 sm:grid-cols-2">
              <label>
                <span class="form-label">Nome</span>
                <input class="form-input" type="text" bind:value={firstName} required />
              </label>
              <label>
                <span class="form-label">Cognome</span>
                <input class="form-input" type="text" bind:value={lastName} required />
              </label>
              <label class="sm:col-span-2">
                <span class="form-label">Email</span>
                <input class="form-input" type="email" bind:value={email} required />
              </label>
              <label>
                <span class="form-label">Telefono</span>
                <input class="form-input" type="tel" bind:value={phone} />
              </label>
              <label class="sm:col-span-2">
                <span class="form-label">Indirizzo</span>
                <input class="form-input" type="text" bind:value={address} required />
              </label>
              <label>
                <span class="form-label">Citta</span>
                <input class="form-input" type="text" bind:value={city} required />
              </label>
              <label>
                <span class="form-label">CAP</span>
                <input class="form-input" type="text" bind:value={zip} required />
              </label>
              <label class="sm:col-span-2">
                <span class="form-label">Paese</span>
                <input class="form-input" type="text" bind:value={country} required />
              </label>
            </div>

            <button type="submit" class="btn-primary w-full">Continua al pagamento</button>
          </form>
        {:else if step === 'payment'}
          <form class="surface-card space-y-4 p-6" on:submit|preventDefault={() => (step = 'review')}>
            <h2 class="font-title text-2xl font-bold text-[#202223]">Pagamento</h2>

            <div class="rounded-xl border border-[#d0ebe4] bg-[#f1f8f5] px-4 py-3 text-xs text-[#005940]">
              Pagamento simulato in ambiente demo. Nessun addebito reale.
            </div>

            <label>
              <span class="form-label">Intestatario carta</span>
              <input class="form-input" type="text" bind:value={cardName} required />
            </label>
            <label>
              <span class="form-label">Numero carta</span>
              <input class="form-input font-mono" type="text" bind:value={cardNumber} maxlength="19" required />
            </label>

            <div class="grid grid-cols-2 gap-4">
              <label>
                <span class="form-label">Scadenza</span>
                <input class="form-input font-mono" type="text" bind:value={cardExpiry} maxlength="5" placeholder="MM/AA" required />
              </label>
              <label>
                <span class="form-label">CVC</span>
                <input class="form-input font-mono" type="text" bind:value={cardCvc} maxlength="4" placeholder="123" required />
              </label>
            </div>

            <div class="flex gap-3">
              <button type="button" on:click={() => (step = 'shipping')} class="btn-secondary flex-1">Indietro</button>
              <button type="submit" class="btn-primary flex-1">Rivedi ordine</button>
            </div>
          </form>
        {:else}
          <div class="surface-card space-y-5 p-6">
            <h2 class="font-title text-2xl font-bold text-[#202223]">Conferma ordine</h2>

            <div class="surface-muted space-y-1 p-4 text-sm text-[#4a4f55]">
              <p class="font-semibold text-[#202223]">Spedizione</p>
              <p>{firstName} {lastName}</p>
              <p>{address}, {zip} {city}, {country}</p>
              <p>{email} · {phone}</p>
            </div>

            <div class="surface-muted space-y-1 p-4 text-sm text-[#4a4f55]">
              <p class="font-semibold text-[#202223]">Pagamento</p>
              <p>{cardName}</p>
              <p class="font-mono">{formatCard(cardNumber)}</p>
            </div>

            <div class="space-y-2">
              {#each items as item}
                <div class="surface-muted flex items-center gap-3 p-3 text-sm">
                  <img src={getProductImage(item.sku, 96, 72)} alt={item.name} class="h-12 w-16 rounded-lg object-cover" />
                  <div class="flex-1">
                    <p class="font-medium text-[#202223]">{item.name}</p>
                    <p class="text-xs text-[#8c9196]">{item.quantity} × {formatCurrency(item.unitPrice)}</p>
                  </div>
                  <p class="font-semibold text-[#202223]">{formatCurrency(item.quantity * item.unitPrice)}</p>
                </div>
              {/each}
            </div>

            {#if submitError}
              <p class="rounded-xl border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700">{submitError}</p>
            {/if}

            <div class="flex gap-3">
              <button type="button" on:click={() => (step = 'payment')} class="btn-secondary flex-1">Indietro</button>
              <button on:click={placeOrder} disabled={isSubmitting} class="btn-primary flex-1 disabled:cursor-not-allowed disabled:opacity-60">
                {isSubmitting ? 'Invio ordine...' : 'Conferma ordine'}
              </button>
            </div>
          </div>
        {/if}
      </div>

      <aside class="lg:sticky lg:top-24">
        <div class="surface-card p-5 text-sm">
          <h3 class="font-title text-2xl font-bold text-[#202223]">Riepilogo</h3>
          <ul class="mt-4 space-y-2">
            {#each items as item}
              <li class="flex justify-between gap-2 text-[#4a4f55]">
                <span class="truncate">{item.name} ×{item.quantity}</span>
                <span>{formatCurrency(item.quantity * item.unitPrice)}</span>
              </li>
            {/each}
          </ul>

          <dl class="mt-4 space-y-2 border-t border-[#e1e3e5] pt-3 text-[#4a4f55]">
            <div class="flex justify-between"><dt>Subtotale</dt><dd>{formatCurrency(subtotal)}</dd></div>
            <div class="flex justify-between"><dt>Spedizione</dt><dd>{shipping === 0 ? 'Gratis' : formatCurrency(shipping)}</dd></div>
            <div class="flex justify-between"><dt>IVA (22%)</dt><dd>{formatCurrency(tax)}</dd></div>
            <div class="flex justify-between border-t border-[#e1e3e5] pt-2 text-base font-bold text-[#202223]"><dt>Totale</dt><dd>{formatCurrency(total)}</dd></div>
          </dl>
        </div>
      </aside>
    </div>
  {/if}
</div>
