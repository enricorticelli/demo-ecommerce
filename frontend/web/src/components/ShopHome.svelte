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
  <section class="reveal surface-card overflow-hidden">
    <div class="grid gap-0 lg:grid-cols-[1.15fr_1fr]">
      <div class="px-6 py-10 md:px-10 md:py-12">
        <p class="text-xs font-semibold uppercase tracking-[0.2em] text-[#008060]">New Season</p>
        <h1 class="mt-3 max-w-xl font-title text-4xl font-extrabold leading-tight text-[#202223] md:text-5xl">
          Vetrina reale: prodotti in evidenza, brand e promo.
        </h1>
        <p class="mt-4 max-w-lg text-sm leading-relaxed text-[#616161] md:text-base">
          Navigazione commerciale sulla home. Ricerca avanzata disponibile nella pagina Shop dedicata.
        </p>
        <div class="mt-7 flex flex-wrap gap-3">
          <a href="#featured" class="btn-primary">Scopri i prodotti top</a>
          <a href="/search" class="btn-secondary">Apri Shop con filtri</a>
        </div>
        <div class="mt-7 grid max-w-xl grid-cols-3 gap-2 text-xs text-[#616161] md:gap-4 md:text-sm">
          <div class="surface-muted p-3 text-center">
            <p class="font-semibold text-[#202223]">{products.length || '—'}</p>
            <p>Prodotti online</p>
          </div>
          <div class="surface-muted p-3 text-center">
            <p class="font-semibold text-[#202223]">{$cartItems.reduce((n, item) => n + item.quantity, 0)}</p>
            <p>Nel carrello</p>
          </div>
          <div class="surface-muted p-3 text-center">
            <p class="font-semibold text-[#202223]">24/7</p>
            <p>Supporto clienti</p>
          </div>
        </div>
      </div>

      <div class="relative min-h-[320px] bg-[#eef7f3] p-8">
        <div class="absolute -right-10 top-8 h-32 w-32 rounded-full bg-[#b5e3d5]"></div>
        <div class="absolute bottom-4 left-4 h-20 w-20 rounded-full bg-[#d8ecff]"></div>
        <div class="relative z-10 space-y-4">
          <div class="surface-card p-4 shadow-sm">
            <p class="text-xs uppercase tracking-wide text-[#6d7175]">Consegna stimata</p>
            <p class="mt-1 text-lg font-bold text-[#202223]">2-3 giorni lavorativi</p>
            <p class="mt-1 text-sm text-[#616161]">Corriere espresso con tracking incluso.</p>
          </div>
          <div class="surface-card p-4 shadow-sm">
            <p class="text-xs uppercase tracking-wide text-[#6d7175]">Vantaggi store</p>
            <ul class="mt-2 space-y-2 text-sm text-[#202223]">
              <li>Pagamento sicuro</li>
              <li>Reso entro 30 giorni</li>
              <li>Spedizione gratuita oltre 120 euro</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  </section>

  <section class="surface-card p-5 md:p-6">
    <div class="flex flex-wrap items-center justify-between gap-3">
      <h2 class="font-title text-2xl font-bold text-[#202223]">Brand in evidenza</h2>
      <a href="/search" class="text-sm font-semibold text-[#008060]">Vedi tutti i prodotti</a>
    </div>
    <div class="mt-4 flex flex-wrap gap-2">
      {#if brands.length > 0}
        {#each brands.slice(0, 8) as brand}
          <span class="rounded-full border border-[#e1e3e5] bg-[#fbfbfb] px-4 py-2 text-sm font-semibold text-[#4a4f55]">{brand.name}</span>
        {/each}
      {:else}
        <span class="rounded-full border border-[#e1e3e5] bg-[#fbfbfb] px-4 py-2 text-sm font-semibold text-[#4a4f55]">Nessun brand disponibile</span>
      {/if}
    </div>
  </section>

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
