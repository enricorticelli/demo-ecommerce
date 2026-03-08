<script lang="ts">
  import { onMount } from 'svelte';
  import { createOrder, getPaymentSessionByOrder, pollOrderUntilDone, type PaymentSession } from '../lib/api';
  import { getProductImage } from '../lib/catalog-presenter';
  import { formatCurrency } from '../lib/format';
  import { cartId, userId, cartItems, cartTotal } from '../stores/cart';
  import { addToast } from '../stores/ui';

  type CheckoutStep = 'shipping' | 'payment' | 'review';

  let step: CheckoutStep = 'shipping';

  let firstName = 'Mario';
  let lastName = 'Rossi';
  let email = 'mario.rossi@example.com';
  let phone = '+39 333 1234567';
  let address = 'Via Roma 1';
  let city = 'Milano';
  let zip = '20100';
  let country = 'Italia';

  let billingSameAsShipping = true;
  let billingAddress = 'Via Roma 1';
  let billingCity = 'Milano';
  let billingZip = '20100';
  let billingCountry = 'Italia';

  let paymentMethod: 'stripe_card' | 'paypal' | 'satispay' = 'stripe_card';

  let isSubmitting = false;
  let submitError = '';

  async function waitForPaymentSession(orderId: string, maxAttempts = 40, intervalMs = 500): Promise<PaymentSession | null> {
    for (let i = 0; i < maxAttempts; i += 1) {
      try {
        const session = await getPaymentSessionByOrder(orderId);
        if (session) {
          return session;
        }
      } catch {
        // transient payment API/gateway error: keep polling
      }

      await new Promise((resolve) => setTimeout(resolve, intervalMs));
    }

    return null;
  }

  $: items = $cartItems;
  $: subtotal = $cartTotal;
  $: shipping = subtotal > 0 ? (subtotal >= 120 ? 0 : 7.9) : 0;
  $: tax = subtotal * 0.22;
  $: total = subtotal + shipping + tax;
  $: isEmpty = items.length === 0;

  const steps = ['Spedizione', 'Pagamento', 'Conferma'];
  const stepIndex: Record<CheckoutStep, number> = { shipping: 0, payment: 1, review: 2 };

  const paymentMethodLabels: Record<'stripe_card' | 'paypal' | 'satispay', string> = {
    stripe_card: 'Carta di credito',
    paypal: 'PayPal',
    satispay: 'Satispay',
  };

  async function placeOrder() {
    isSubmitting = true;
    submitError = '';

    try {
      const result = await createOrder({
        cartId: $cartId,
        userId: $userId,
        identityType: 'Anonymous',
        paymentMethod,
        items: items.map((item) => ({
          productId: item.productId,
          sku: item.sku,
          name: item.name,
          quantity: item.quantity,
          unitPrice: item.unitPrice,
        })),
        totalAmount: subtotal,
        authenticatedUserId: null,
        anonymousId: $userId,
        customer: {
          firstName,
          lastName,
          email,
          phone,
        },
        shippingAddress: {
          street: address,
          city,
          postalCode: zip,
          country,
        },
        billingAddress: {
          street: billingSameAsShipping ? address : billingAddress,
          city: billingSameAsShipping ? city : billingCity,
          postalCode: billingSameAsShipping ? zip : billingZip,
          country: billingSameAsShipping ? country : billingCountry,
        },
      });
      const paymentSession = await waitForPaymentSession(result.orderId);
      if (paymentSession) {
        window.location.href = paymentSession.redirectUrl;
        return;
      }

      const completedOrder = await pollOrderUntilDone(result.orderId, () => undefined, 30, 1000);
      if (completedOrder?.status === 'Completed') {
        window.location.href = `/orders/${result.orderId}`;
        return;
      }

      throw new Error(`Ordine creato (${result.orderId}) ma sessione pagamento non disponibile. Apri /orders/${result.orderId} e riprova.`);
    } catch (err) {
      submitError = err instanceof Error ? err.message : 'Errore durante la creazione dell\'ordine.';
      addToast(submitError, 'error');
    } finally {
      isSubmitting = false;
    }
  }

  onMount(() => {
    const params = new URLSearchParams(window.location.search);
    if (params.get('payment') === 'cancelled') {
      addToast('Pagamento annullato. Puoi riprovare il checkout.', 'info');
    }

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

              <div class="sm:col-span-2 mt-2 border-t border-[#e1e3e5] pt-4">
                <label class="flex items-center gap-2 text-sm text-[#4a4f55]">
                  <input type="checkbox" bind:checked={billingSameAsShipping} />
                  Usa lo stesso indirizzo per fatturazione
                </label>
              </div>

              {#if !billingSameAsShipping}
                <label class="sm:col-span-2">
                  <span class="form-label">Indirizzo fatturazione</span>
                  <input class="form-input" type="text" bind:value={billingAddress} required={!billingSameAsShipping} />
                </label>
                <label>
                  <span class="form-label">Citta fatturazione</span>
                  <input class="form-input" type="text" bind:value={billingCity} required={!billingSameAsShipping} />
                </label>
                <label>
                  <span class="form-label">CAP fatturazione</span>
                  <input class="form-input" type="text" bind:value={billingZip} required={!billingSameAsShipping} />
                </label>
                <label class="sm:col-span-2">
                  <span class="form-label">Paese fatturazione</span>
                  <input class="form-input" type="text" bind:value={billingCountry} required={!billingSameAsShipping} />
                </label>
              {/if}
            </div>

            <button type="submit" class="btn-primary w-full">Continua al pagamento</button>
          </form>
        {:else if step === 'payment'}
          <form class="surface-card space-y-4 p-6" on:submit|preventDefault={() => (step = 'review')}>
            <h2 class="font-title text-2xl font-bold text-[#202223]">Pagamento</h2>

            <div class="rounded-xl border border-[#d0ebe4] bg-[#f1f8f5] px-4 py-3 text-xs text-[#005940]">
              Nessun dato carta viene raccolto dal nostro backend. Il pagamento avverra su pagina hosted esterna.
            </div>

            <div class="space-y-3">
              <label class="flex cursor-pointer items-center justify-between rounded-xl border border-[#d5d9df] px-4 py-3">
                <span class="font-medium text-[#202223]">Carta di credito</span>
                <input type="radio" bind:group={paymentMethod} value="stripe_card" />
              </label>
              <label class="flex cursor-pointer items-center justify-between rounded-xl border border-[#d5d9df] px-4 py-3">
                <span class="font-medium text-[#202223]">PayPal</span>
                <input type="radio" bind:group={paymentMethod} value="paypal" />
              </label>
              <label class="flex cursor-pointer items-center justify-between rounded-xl border border-[#d5d9df] px-4 py-3">
                <span class="font-medium text-[#202223]">Satispay</span>
                <input type="radio" bind:group={paymentMethod} value="satispay" />
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
              <p>{paymentMethodLabels[paymentMethod]}</p>
            </div>

            <div class="surface-muted space-y-1 p-4 text-sm text-[#4a4f55]">
              <p class="font-semibold text-[#202223]">Fatturazione</p>
              {#if billingSameAsShipping}
                <p>Uguale a spedizione</p>
              {:else}
                <p>{billingAddress}, {billingZip} {billingCity}, {billingCountry}</p>
              {/if}
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
