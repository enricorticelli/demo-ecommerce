<script lang="ts">
  import { onMount } from 'svelte';
  import {
    fetchProducts,
    fetchNewArrivals,
    fetchBestSellers,
    fetchBrands,
    addCartItem,
    fetchCart,
    type Product,
    type Brand,
  } from '../lib/api';
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
  import { cartId, userId, cartItems, syncCartFromServer } from '../stores/cart';
  import { addToast } from '../stores/ui';

  let products: Product[] = [];
  let brands: Brand[] = [];
  let newArrivals: Product[] = [];
  let bestSellers: Product[] = [];
  let isLoading = true;
  let loadError = '';
  let addingProductId: string | null = null;

  $: featuredProducts = [...products]
    .sort((a, b) => (stableHash(b.sku + b.name) % 100) - (stableHash(a.sku + a.name) % 100))
    .slice(0, 8);

  async function load() {
    isLoading = true;
    loadError = '';

    try {
      const [allProducts, arrivalProducts, bestSellerProducts] = await Promise.all([
        fetchProducts(),
        fetchNewArrivals(),
        fetchBestSellers(),
      ]);
      brands = await fetchBrands();

      products = allProducts;
      newArrivals = arrivalProducts;
      bestSellers = bestSellerProducts;
    } catch {
      loadError = 'Impossibile caricare il catalogo. Verifica che i servizi siano attivi.';
    } finally {
      isLoading = false;
    }
  }

  async function addToCart(product: Product) {
    addingProductId = product.id;

    try {
      await addCartItem($cartId, {
        userId: $userId,
        productId: product.id,
        sku: product.sku,
        name: product.name,
        quantity: 1,
        unitPrice: product.price,
      });
      const cart = await fetchCart($cartId);
      if (cart) syncCartFromServer(cart.items);
      addToast(`${product.name} aggiunto al carrello`, 'success');
    } catch (err) {
      addToast(err instanceof Error ? err.message : 'Errore aggiunta al carrello', 'error');
    } finally {
      addingProductId = null;
    }
  }

  onMount(load);
</script>

<div class="space-y-8">

  <section class="grid gap-4 md:grid-cols-2">
    <article class="surface-card overflow-hidden">
      <div class="bg-[#dff3ec] px-6 py-6">
        <p class="text-xs font-semibold uppercase tracking-[0.2em] text-[#005940]">Promo weekend</p>
        <h3 class="mt-2 font-title text-3xl font-extrabold text-[#202223]">Fino al -20%</h3>
        <p class="mt-2 text-sm text-[#4a4f55]">Su una selezione di articoli bestseller fino a domenica.</p>
        <a href="#best-sellers" class="btn-primary mt-4">Acquista in promo</a>
      </div>
    </article>

    <article class="surface-card overflow-hidden">
      <div class="bg-[#e8f0ff] px-6 py-6">
        <p class="text-xs font-semibold uppercase tracking-[0.2em] text-[#224f96]">Collection drop</p>
        <h3 class="mt-2 font-title text-3xl font-extrabold text-[#202223]">Nuovi arrivi</h3>
        <p class="mt-2 text-sm text-[#4a4f55]">Scopri la nuova capsule con disponibilita limitata.</p>
        <a href="#new-arrivals" class="btn-secondary mt-4">Esplora nuovi arrivi</a>
      </div>
    </article>
  </section>

  {#if loadError}
    <div class="surface-card border-rose-200 bg-rose-50 p-6 text-sm font-medium text-rose-700">
      {loadError}
      <button on:click={load} class="ml-2 underline">Riprova</button>
    </div>
  {/if}

  <section id="new-arrivals" class="space-y-4">
    <div class="flex flex-wrap items-end justify-between gap-3">
      <div>
        <p class="text-xs font-semibold uppercase tracking-[0.2em] text-[#008060]">Fresh drop</p>
        <h2 class="font-title text-3xl font-extrabold text-[#202223]">Nuovi arrivi</h2>
      </div>
      <a href="/search" class="text-sm font-semibold text-[#008060]">Vedi nello shop</a>
    </div>

    {#if isLoading}
      <div class="grid grid-cols-2 gap-4 md:grid-cols-3 xl:grid-cols-4">
        {#each Array(4) as _}
          <div class="surface-card h-80 animate-pulse"></div>
        {/each}
      </div>
    {:else}
      <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {#each newArrivals.slice(0, 4) as product}
          {@const stock = getProductStock(product.id)}
          {@const category = getProductCategory(product)}
          <article class="surface-card pop-in flex flex-col overflow-hidden transition hover:-translate-y-0.5 hover:shadow-md">
            <a href="/product/{product.id}" class="block overflow-hidden border-b border-[#f0f0f1] bg-[#fafafa]">
              <img src={getProductImage(product.sku, 640, 420)} alt={product.name} class="aspect-[4/3] w-full object-cover" loading="lazy" />
            </a>
            <div class="flex flex-1 flex-col gap-3 p-4">
              <span class="status-pill bg-[#f1f8f5] text-[#005940]">{category}</span>
              <a href="/product/{product.id}"><h3 class="font-title text-lg font-bold text-[#202223]">{product.name}</h3></a>
              <p class="text-xs {STOCK_COLORS[stock]}">{STOCK_LABELS[stock]}</p>
              <div class="mt-auto flex items-center justify-between border-t border-[#f0f0f1] pt-3">
                <p class="text-2xl font-bold text-[#202223]">{formatCurrency(product.price)}</p>
                <button on:click={() => addToCart(product)} disabled={addingProductId === product.id} class="btn-primary">
                  {addingProductId === product.id ? '...' : 'Aggiungi'}
                </button>
              </div>
            </div>
          </article>
        {/each}
      </div>
    {/if}
  </section>

  <section id="best-sellers" class="space-y-4">
    <div class="flex flex-wrap items-end justify-between gap-3">
      <div>
        <p class="text-xs font-semibold uppercase tracking-[0.2em] text-[#008060]">Top picks</p>
        <h2 class="font-title text-3xl font-extrabold text-[#202223]">Best sellers</h2>
      </div>
      <a href="/search" class="text-sm font-semibold text-[#008060]">Apri shop completo</a>
    </div>

    {#if isLoading}
      <div class="grid grid-cols-2 gap-4 md:grid-cols-3 xl:grid-cols-4">
        {#each Array(8) as _}
          <div class="surface-card h-80 animate-pulse"></div>
        {/each}
      </div>
    {:else}
      <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {#each bestSellers.slice(0, 8) as product, idx}
          {@const stock = getProductStock(product.id)}
          {@const rating = getProductRating(product.id)}
          {@const reviews = getProductReviewCount(product.id)}
          {@const category = getProductCategory(product)}

          <article class="surface-card pop-in flex flex-col overflow-hidden transition hover:-translate-y-0.5 hover:shadow-md" style="animation-delay: {Math.min(idx, 9) * 35}ms">
            <a href="/product/{product.id}" class="block overflow-hidden border-b border-[#f0f0f1] bg-[#fafafa]">
              <img src={getProductImage(product.sku, 640, 420)} alt={product.name} class="aspect-[4/3] w-full object-cover transition duration-300 hover:scale-105" loading="lazy" />
            </a>

            <div class="flex flex-1 flex-col gap-3 p-4">
              <div class="flex items-center justify-between gap-2">
                <span class="status-pill bg-[#f1f8f5] text-[#005940]">{category}</span>
                <span class="text-xs text-[#4a4f55]">★ {rating} ({reviews})</span>
              </div>

              <div class="flex-1">
                <a href="/product/{product.id}">
                  <h3 class="font-title text-lg font-bold leading-snug text-[#202223]">{product.name}</h3>
                </a>
                <p class="mt-1 line-clamp-2 text-sm text-[#616161]">{product.description || 'Descrizione non disponibile per questo articolo.'}</p>
              </div>

              <div class="flex items-center justify-between text-xs">
                <span class="font-semibold {STOCK_COLORS[stock]}">{STOCK_LABELS[stock]}</span>
                <span class="text-[#8c9196]">SKU {product.sku}</span>
              </div>

              <div class="border-t border-[#f0f0f1] pt-3">
                <div class="mb-3 flex items-baseline justify-between">
                  <p class="text-2xl font-bold text-[#202223]">{formatCurrency(product.price)}</p>
                  <p class="text-xs text-[#8c9196]">IVA inclusa</p>
                </div>
                <button on:click={() => addToCart(product)} disabled={addingProductId === product.id} class="btn-primary w-full">
                  {addingProductId === product.id ? 'Aggiunta...' : 'Aggiungi al carrello'}
                </button>
              </div>
            </div>
          </article>
        {/each}
      </div>
    {/if}
  </section>

  <section id="featured" class="space-y-4">
    <div class="flex flex-wrap items-end justify-between gap-3">
      <div>
        <p class="text-xs font-semibold uppercase tracking-[0.2em] text-[#008060]">Featured collection</p>
        <h2 class="font-title text-3xl font-extrabold text-[#202223]">Prodotti in evidenza</h2>
      </div>
      <a href="/search" class="text-sm font-semibold text-[#008060]">Apri Shop completo</a>
    </div>

    {#if isLoading}
      <div class="grid grid-cols-2 gap-4 md:grid-cols-3 xl:grid-cols-4">
        {#each Array(8) as _}
          <div class="surface-card h-80 animate-pulse"></div>
        {/each}
      </div>
    {:else}
      <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {#each featuredProducts as product}
          {@const stock = getProductStock(product.id)}
          {@const rating = getProductRating(product.id)}
          {@const reviews = getProductReviewCount(product.id)}
          {@const category = getProductCategory(product)}

          <article class="surface-card pop-in flex flex-col overflow-hidden transition hover:-translate-y-0.5 hover:shadow-md">
            <a href="/product/{product.id}" class="block overflow-hidden border-b border-[#f0f0f1] bg-[#fafafa]">
              <img src={getProductImage(product.sku, 640, 420)} alt={product.name} class="aspect-[4/3] w-full object-cover transition duration-300 hover:scale-105" loading="lazy" />
            </a>

            <div class="flex flex-1 flex-col gap-3 p-4">
              <div class="flex items-center justify-between gap-2">
                <span class="status-pill bg-[#f1f8f5] text-[#005940]">{category}</span>
                <span class="text-xs text-[#4a4f55]">★ {rating} ({reviews})</span>
              </div>

              <div class="flex-1">
                <a href="/product/{product.id}">
                  <h3 class="font-title text-lg font-bold leading-snug text-[#202223]">{product.name}</h3>
                </a>
                <p class="mt-1 line-clamp-2 text-sm text-[#616161]">{product.description || 'Descrizione non disponibile per questo articolo.'}</p>
              </div>

              <div class="flex items-center justify-between text-xs">
                <span class="font-semibold {STOCK_COLORS[stock]}">{STOCK_LABELS[stock]}</span>
                <span class="text-[#8c9196]">SKU {product.sku}</span>
              </div>

              <div class="border-t border-[#f0f0f1] pt-3">
                <div class="mb-3 flex items-baseline justify-between">
                  <p class="text-2xl font-bold text-[#202223]">{formatCurrency(product.price)}</p>
                  <p class="text-xs text-[#8c9196]">IVA inclusa</p>
                </div>
                <button on:click={() => addToCart(product)} disabled={addingProductId === product.id} class="btn-primary w-full">
                  {addingProductId === product.id ? 'Aggiunta...' : 'Aggiungi al carrello'}
                </button>
              </div>
            </div>
          </article>
        {/each}
      </div>
    {/if}
  </section>
</div>
