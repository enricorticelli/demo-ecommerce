<script lang="ts">
  import { onMount } from 'svelte';
  import { fetchCart, removeCartItem, type CartItemDto } from '../lib/api';
  import { getProductImage } from '../lib/catalog-presenter';
  import { formatCurrency } from '../lib/format';
  import { cartId, cartItems, cartTotal, syncCartFromServer } from '../stores/cart';
  import { addToast } from '../stores/ui';

  let isLoading = true;
  let removingId: string | null = null;

  $: items = $cartItems;
  $: subtotal = $cartTotal;
  $: shipping = subtotal > 0 ? (subtotal >= 120 ? 0 : 7.9) : 0;
  $: tax = subtotal * 0.22;
  $: total = subtotal + shipping + tax;
  $: isEmpty = items.length === 0;

  async function syncCart() {
    isLoading = true;
    try {
      const cart = await fetchCart($cartId);
      if (cart) syncCartFromServer(cart.items);
      else syncCartFromServer([]);
    } catch {
      // keep local state
    } finally {
      isLoading = false;
    }
  }

  async function removeItem(item: CartItemDto) {
    removingId = item.productId;
    try {
      await removeCartItem($cartId, item.productId);
      const cart = await fetchCart($cartId);
      if (cart) syncCartFromServer(cart.items);
      else syncCartFromServer([]);
      addToast(`${item.name} rimosso dal carrello`, 'info');
    } catch {
      addToast('Impossibile rimuovere il prodotto', 'error');
    } finally {
      removingId = null;
    }
  }

  onMount(syncCart);
</script>

<div class="space-y-6 reveal">
  <div class="flex flex-wrap items-center justify-between gap-3">
    <div>
      <p class="text-xs font-semibold uppercase tracking-[0.2em] text-[#008060]">Carrello</p>
      <h1 class="font-title text-4xl font-extrabold text-[#202223]">Il tuo ordine</h1>
    </div>
    {#if !isEmpty}
      <span class="status-pill bg-[#f1f8f5] text-[#005940]">{items.reduce((n, item) => n + item.quantity, 0)} articoli</span>
    {/if}
  </div>

  {#if isLoading}
    <div class="space-y-3">
      {#each Array(3) as _}
        <div class="surface-card h-24 animate-pulse"></div>
      {/each}
    </div>
  {:else if isEmpty}
    <div class="surface-card p-14 text-center">
      <h2 class="font-title text-2xl font-bold text-[#202223]">Carrello vuoto</h2>
      <p class="mt-2 text-sm text-[#616161]">Aggiungi prodotti dal catalogo per iniziare.</p>
      <a href="/" class="btn-primary mt-6">Continua lo shopping</a>
    </div>
  {:else}
    <div class="grid gap-6 lg:grid-cols-[minmax(0,1fr)_340px]">
      <div class="space-y-3">
        {#each items as item}
          <article class="surface-card pop-in flex gap-4 p-4">
            <img src={getProductImage(item.sku, 160, 120)} alt={item.name} class="h-24 w-32 rounded-xl object-cover" loading="lazy" />
            <div class="flex flex-1 flex-col gap-3">
              <div class="flex items-start justify-between gap-2">
                <div>
                  <a href="/product/{item.productId}" class="font-title text-lg font-bold text-[#202223]">{item.name}</a>
                  <p class="text-xs text-[#8c9196]">SKU {item.sku}</p>
                </div>
                <button
                  on:click={() => removeItem(item)}
                  disabled={removingId === item.productId}
                  class="rounded-lg border border-rose-200 bg-rose-50 px-3 py-1 text-xs font-semibold text-rose-700 disabled:opacity-50"
                >
                  {removingId === item.productId ? '...' : 'Rimuovi'}
                </button>
              </div>

              <div class="flex items-center justify-between text-sm">
                <p class="text-[#616161]">{item.quantity} x {formatCurrency(item.unitPrice)}</p>
                <p class="font-bold text-[#202223]">{formatCurrency(item.quantity * item.unitPrice)}</p>
              </div>
            </div>
          </article>
        {/each}
      </div>

      <aside class="lg:sticky lg:top-24">
        <div class="surface-card p-5">
          <h2 class="font-title text-2xl font-bold text-[#202223]">Riepilogo</h2>
          <dl class="mt-4 space-y-2 text-sm">
            <div class="flex justify-between text-[#4a4f55]">
              <dt>Subtotale</dt>
              <dd>{formatCurrency(subtotal)}</dd>
            </div>
            <div class="flex justify-between text-[#4a4f55]">
              <dt>Spedizione</dt>
              <dd class:text-[#008060]={shipping === 0}>{shipping === 0 ? 'Gratis' : formatCurrency(shipping)}</dd>
            </div>
            {#if shipping > 0}
              <p class="text-xs text-[#8c9196]">Spedizione gratuita sopra {formatCurrency(120)}</p>
            {/if}
            <div class="flex justify-between text-[#4a4f55]">
              <dt>IVA (22%)</dt>
              <dd>{formatCurrency(tax)}</dd>
            </div>
            <div class="flex justify-between border-t border-[#e1e3e5] pt-3 text-base font-bold text-[#202223]">
              <dt>Totale</dt>
              <dd>{formatCurrency(total)}</dd>
            </div>
          </dl>

          <a href="/checkout" class="btn-primary mt-5 w-full">Procedi al checkout</a>
          <a href="/" class="btn-secondary mt-3 w-full">Continua lo shopping</a>
        </div>
      </aside>
    </div>
  {/if}
</div>
