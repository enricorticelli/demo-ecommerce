<script lang="ts">
  import { onMount } from 'svelte';
  import { fetchProduct, addCartItem, fetchCart, type Product } from '../lib/api';
  import {
    getProductImage,
    getProductRating,
    getProductReviewCount,
    getProductStock,
    getProductCategory,
    STOCK_LABELS,
    STOCK_COLORS,
    stableHash,
  } from '../lib/catalog-presenter';
  import { formatCurrency } from '../lib/format';
  import { cartId, userId, syncCartFromServer } from '../stores/cart';
  import { addToast } from '../stores/ui';

  export let productId: string;

  let product: Product | null = null;
  let isLoading = true;
  let notFound = false;
  let loadError = '';
  let quantity = 1;
  let adding = false;
  let addedToCart = false;

  $: stock = product ? getProductStock(product.id) : 'in_stock';
  $: rating = product ? getProductRating(product.id) : 0;
  $: reviews = product ? getProductReviewCount(product.id) : 0;
  $: category = product ? getProductCategory(product) : '';
  $: features = product ? generateFeatures(product) : [];

  function generateFeatures(currentProduct: Product): string[] {
    const common = [
      'Consegna rapida in 2-3 giorni',
      'Reso gratuito entro 30 giorni',
      'Assistenza clienti dedicata',
    ];
    const extras = [
      'Packaging premium incluso',
      'Garanzia ufficiale 24 mesi',
      'Qualita verificata dal nostro team',
      'Prodotto tra i piu scelti del mese',
    ];
    const seed = stableHash(`features:${currentProduct.id}`);
    return [...common, extras[seed % extras.length]];
  }

  async function load() {
    isLoading = true;
    notFound = false;
    loadError = '';
    try {
      product = await fetchProduct(productId);
    } catch (err) {
      if (err instanceof Error && err.name === 'NotFoundError') {
        notFound = true;
      } else {
        loadError = 'Impossibile caricare il prodotto. Riprova tra poco.';
      }
    } finally {
      isLoading = false;
    }
  }

  async function addToCart() {
    if (!product) return;

    adding = true;
    addedToCart = false;

    try {
      await addCartItem($cartId, $userId, {
        productId: product.id,
        sku: product.sku,
        name: product.name,
        quantity,
        unitPrice: product.price,
      });
      const cart = await fetchCart($cartId, $userId);
      if (cart) syncCartFromServer(cart.items);
      addToast(`${product.name} aggiunto al carrello`, 'success');
      addedToCart = true;
    } catch (err) {
      addToast(err instanceof Error ? err.message : 'Errore durante l\'aggiunta al carrello', 'error');
    } finally {
      adding = false;
    }
  }

  onMount(load);
</script>

{#if isLoading}
  <div class="grid gap-8 md:grid-cols-2">
    <div class="surface-card aspect-[4/3] animate-pulse"></div>
    <div class="space-y-3">
      <div class="h-4 w-28 animate-pulse rounded bg-slate-200"></div>
      <div class="h-10 w-4/5 animate-pulse rounded bg-slate-200"></div>
      <div class="h-4 w-full animate-pulse rounded bg-slate-200"></div>
      <div class="h-4 w-2/3 animate-pulse rounded bg-slate-200"></div>
    </div>
  </div>
{:else if notFound}
  <div class="surface-card p-12 text-center">
    <h1 class="font-title text-3xl font-bold text-[#202223]">Prodotto non trovato</h1>
    <p class="mt-3 text-sm text-[#616161]">L'articolo richiesto non e disponibile.</p>
    <a href="/" class="btn-secondary mt-5">Torna al catalogo</a>
  </div>
{:else if loadError}
  <div class="surface-card border-rose-200 bg-rose-50 p-6 text-sm text-rose-700">{loadError}</div>
{:else if product}
  <div class="space-y-6 reveal">
    <nav class="flex items-center gap-2 text-sm text-[#616161]">
      <a href="/" class="hover:text-[#202223]">Catalogo</a>
      <span>›</span>
      <span>{category}</span>
      <span>›</span>
      <span class="truncate text-[#202223]">{product.name}</span>
    </nav>

    <div class="grid gap-8 lg:grid-cols-[minmax(0,1fr)_460px]">
      <div class="surface-card overflow-hidden">
        <img src={getProductImage(product.sku, 1024, 768)} alt={product.name} class="aspect-[4/3] w-full object-cover" />
      </div>

      <div class="space-y-5">
        <div>
          <span class="status-pill bg-[#f1f8f5] text-[#005940]">{category}</span>
          <h1 class="mt-3 font-title text-4xl font-extrabold leading-tight text-[#202223]">{product.name}</h1>
          <p class="mt-2 text-sm text-[#8c9196]">{product.sku}</p>
        </div>

        <div class="flex items-center gap-2 text-sm text-[#4a4f55]">
          <span class="font-semibold text-[#202223]">★ {rating}</span>
          <span>({reviews} recensioni)</span>
          <span class="mx-1 text-[#d2d5d8]">|</span>
          <span class="font-semibold {STOCK_COLORS[stock]}">{STOCK_LABELS[stock]}</span>
        </div>

        <p class="text-sm leading-relaxed text-[#4a4f55]">
          {product.description || 'Nessuna descrizione disponibile per questo prodotto.'}
        </p>

        <ul class="surface-muted p-4 text-sm text-[#202223]">
          {#each features as feature}
            <li class="flex items-start gap-2 py-1">
              <span class="mt-1 h-1.5 w-1.5 rounded-full bg-[#008060]"></span>
              <span>{feature}</span>
            </li>
          {/each}
        </ul>

        <div class="surface-card p-5">
          <div class="flex items-baseline justify-between">
            <p class="text-4xl font-extrabold text-[#202223]">{formatCurrency(product.price)}</p>
            <p class="text-xs text-[#8c9196]">IVA inclusa</p>
          </div>

          <div class="mt-4 flex items-center gap-2">
            <label class="text-sm font-semibold text-[#4a4f55]">Quantita</label>
            <div class="flex items-center rounded-xl border border-[#e1e3e5]">
              <button on:click={() => (quantity = Math.max(1, quantity - 1))} class="px-3 py-2 text-[#4a4f55]">-</button>
              <input type="number" min="1" max="10" bind:value={quantity} class="w-14 border-x border-[#e1e3e5] py-2 text-center text-sm outline-none" />
              <button on:click={() => (quantity = Math.min(10, quantity + 1))} class="px-3 py-2 text-[#4a4f55]">+</button>
            </div>
          </div>

          <div class="mt-5 flex gap-3">
            <button on:click={addToCart} disabled={adding} class="btn-primary flex-1 disabled:cursor-not-allowed disabled:opacity-60">
              {adding ? 'Aggiunta in corso...' : addedToCart ? 'Aggiunto al carrello' : 'Aggiungi al carrello'}
            </button>
            <a href="/cart" class="btn-secondary">Vai al carrello</a>
          </div>
        </div>
      </div>
    </div>
  </div>
{/if}
