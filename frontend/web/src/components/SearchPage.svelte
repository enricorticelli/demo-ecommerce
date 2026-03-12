<script lang="ts">
  import { onMount } from 'svelte';
  import { fetchProducts, addCartItem, fetchCart, type Product } from '../lib/api';
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

  export let initialQuery = '';

  type SortMode = 'featured' | 'price-asc' | 'price-desc' | 'name';
  type StockFacet = 'all' | 'in_stock' | 'low_stock' | 'preorder' | 'out_of_stock';
  type PriceFacet = 'all' | 'under-50' | '50-100' | '100-200' | 'over-200';

  let products: Product[] = [];
  let isLoading = true;
  let loadError = '';
  let searchTerm = '';
  let selectedBrand = 'all';
  let selectedCategory = 'all';
  let selectedStock: StockFacet = 'all';
  let selectedPrice: PriceFacet = 'all';
  let sortMode: SortMode = 'featured';
  let addingProductId: string | null = null;
  let mobileFiltersOpen = false;

  const stockLabels: Record<StockFacet, string> = {
    all: 'Tutti',
    in_stock: 'Disponibili',
    low_stock: 'Scorte basse',
    preorder: 'Disponibili in 48h',
    out_of_stock: 'Esauriti',
  };

  const priceLabels: Record<PriceFacet, string> = {
    all: 'Tutti i prezzi',
    'under-50': 'Meno di 50€',
    '50-100': '50€ - 100€',
    '100-200': '100€ - 200€',
    'over-200': 'Oltre 200€',
  };

  $: categories = Array.from(new Set(products.map((product) => getProductCategory(product)))).sort((a, b) =>
    a.localeCompare(b)
  );
  $: brands = Array.from(new Set(products.map((product) => product.brandName))).sort((a, b) =>
    a.localeCompare(b)
  );

  $: results = products
    .filter((product) => {
      const query = searchTerm.trim().toLowerCase();
      const matchQuery =
        !query ||
        product.name.toLowerCase().includes(query) ||
        product.description.toLowerCase().includes(query) ||
        product.sku.toLowerCase().includes(query);

      const category = getProductCategory(product);
      const matchCategory = selectedCategory === 'all' || category === selectedCategory;
      const matchBrand = selectedBrand === 'all' || product.brandName === selectedBrand;

      const stock = getProductStock(product.id);
      const matchStock = selectedStock === 'all' || stock === selectedStock;

      const matchPrice =
        selectedPrice === 'all' ||
        (selectedPrice === 'under-50' && product.price < 50) ||
        (selectedPrice === '50-100' && product.price >= 50 && product.price <= 100) ||
        (selectedPrice === '100-200' && product.price > 100 && product.price <= 200) ||
        (selectedPrice === 'over-200' && product.price > 200);

      return matchQuery && matchCategory && matchBrand && matchStock && matchPrice;
    })
    .sort((a, b) => {
      if (sortMode === 'price-asc') return a.price - b.price;
      if (sortMode === 'price-desc') return b.price - a.price;
      if (sortMode === 'name') return a.name.localeCompare(b.name);
      return (stableHash(b.sku + b.name) % 100) - (stableHash(a.sku + a.name) % 100);
    });

  function clearFilters() {
    selectedCategory = 'all';
    selectedBrand = 'all';
    selectedStock = 'all';
    selectedPrice = 'all';
    sortMode = 'featured';
  }

  async function load() {
    isLoading = true;
    loadError = '';

    try {
      products = await fetchProducts();
    } catch {
      loadError = 'Impossibile caricare i prodotti. Verifica che i servizi siano attivi.';
    } finally {
      isLoading = false;
    }
  }

  async function addToCart(product: Product) {
    addingProductId = product.id;

    try {
      await addCartItem($cartId, $userId, {
        productId: product.id,
        sku: product.sku,
        name: product.name,
        quantity: 1,
        unitPrice: product.price,
      });
      const cart = await fetchCart($cartId, $userId);
      if (cart) syncCartFromServer(cart.items);
      addToast(`${product.name} aggiunto al carrello`, 'success');
    } catch (err) {
      addToast(err instanceof Error ? err.message : 'Errore aggiunta al carrello', 'error');
    } finally {
      addingProductId = null;
    }
  }

  onMount(() => {
    searchTerm = initialQuery.trim();
    load();
  });
</script>

<div class="space-y-5 reveal">
  <div class="surface-card p-5 md:p-6">
    <p class="text-xs font-semibold uppercase tracking-[0.2em] text-[#008060]">Shop</p>
    <h1 class="mt-2 font-title text-4xl font-extrabold text-[#202223]">Ricerca prodotti</h1>
    <p class="mt-2 text-sm text-[#616161]">Pagina dedicata con faccette filtri e ordinamento.</p>

    <div class="mt-5 grid gap-3 md:grid-cols-[1fr_220px_auto]">
      <label>
        <span class="form-label">Ricerca testuale</span>
        <input bind:value={searchTerm} type="search" class="form-input" placeholder="Nome, SKU, descrizione" />
      </label>
      <label>
        <span class="form-label">Ordina per</span>
        <select bind:value={sortMode} class="form-input">
          <option value="featured">In evidenza</option>
          <option value="price-asc">Prezzo crescente</option>
          <option value="price-desc">Prezzo decrescente</option>
          <option value="name">Nome</option>
        </select>
      </label>
      <button type="button" class="btn-secondary self-end" on:click={clearFilters}>Reset filtri</button>
    </div>

    <button type="button" class="btn-secondary mt-4 md:hidden" on:click={() => (mobileFiltersOpen = !mobileFiltersOpen)}>
      {mobileFiltersOpen ? 'Nascondi filtri' : 'Mostra filtri'}
    </button>
  </div>

  <div class="grid gap-5 md:grid-cols-[280px_minmax(0,1fr)]">
    <aside class={`space-y-4 ${mobileFiltersOpen ? 'block' : 'hidden'} md:block`}>
      <div class="surface-card p-4">
        <p class="text-xs font-semibold uppercase tracking-[0.2em] text-[#6d7175]">Brand</p>
        <div class="mt-3 flex flex-wrap gap-2">
          {#each ['all', ...brands] as brand}
            <button
              on:click={() => (selectedBrand = brand)}
              class="rounded-full border px-3 py-1 text-xs font-semibold transition"
              class:border-[#008060]={selectedBrand === brand}
              class:bg-[#f1f8f5]={selectedBrand === brand}
              class:text-[#005940]={selectedBrand === brand}
              class:border-[#e1e3e5]={selectedBrand !== brand}
              class:bg-white={selectedBrand !== brand}
              class:text-[#4a4f55]={selectedBrand !== brand}
            >
              {brand === 'all' ? 'Tutti' : brand}
            </button>
          {/each}
        </div>
      </div>

      <div class="surface-card p-4">
        <p class="text-xs font-semibold uppercase tracking-[0.2em] text-[#6d7175]">Categoria</p>
        <div class="mt-3 flex flex-wrap gap-2">
          {#each ['all', ...categories] as category}
            <button
              on:click={() => (selectedCategory = category)}
              class="rounded-full border px-3 py-1 text-xs font-semibold transition"
              class:border-[#008060]={selectedCategory === category}
              class:bg-[#f1f8f5]={selectedCategory === category}
              class:text-[#005940]={selectedCategory === category}
              class:border-[#e1e3e5]={selectedCategory !== category}
              class:bg-white={selectedCategory !== category}
              class:text-[#4a4f55]={selectedCategory !== category}
            >
              {category === 'all' ? 'Tutte' : category}
            </button>
          {/each}
        </div>
      </div>

      <div class="surface-card p-4">
        <p class="text-xs font-semibold uppercase tracking-[0.2em] text-[#6d7175]">Disponibilita</p>
        <div class="mt-3 space-y-2">
          {#each Object.entries(stockLabels) as [stockValue, label]}
            <label class="flex cursor-pointer items-center gap-2 text-sm text-[#4a4f55]">
              <input
                type="radio"
                name="stock-filter"
                checked={selectedStock === stockValue}
                on:change={() => (selectedStock = stockValue as StockFacet)}
              />
              <span>{label}</span>
            </label>
          {/each}
        </div>
      </div>

      <div class="surface-card p-4">
        <p class="text-xs font-semibold uppercase tracking-[0.2em] text-[#6d7175]">Prezzo</p>
        <div class="mt-3 space-y-2">
          {#each Object.entries(priceLabels) as [priceValue, label]}
            <label class="flex cursor-pointer items-center gap-2 text-sm text-[#4a4f55]">
              <input
                type="radio"
                name="price-filter"
                checked={selectedPrice === priceValue}
                on:change={() => (selectedPrice = priceValue as PriceFacet)}
              />
              <span>{label}</span>
            </label>
          {/each}
        </div>
      </div>
    </aside>

    <section class="space-y-4">
      <div class="flex items-center justify-between">
        <p class="text-sm text-[#616161]">{results.length} risultati su {products.length} prodotti</p>
      </div>

      {#if isLoading}
        <div class="grid grid-cols-2 gap-4 lg:grid-cols-3 xl:grid-cols-4">
          {#each Array(8) as _}
            <div class="surface-card h-80 animate-pulse"></div>
          {/each}
        </div>
      {:else if loadError}
        <div class="surface-card border-rose-200 bg-rose-50 p-6 text-sm font-medium text-rose-700">
          {loadError}
          <button on:click={load} class="ml-2 underline">Riprova</button>
        </div>
      {:else if results.length === 0}
        <div class="surface-card p-10 text-center">
          <h2 class="font-title text-2xl font-bold text-[#202223]">Nessun risultato</h2>
          <p class="mt-2 text-sm text-[#616161]">Modifica i filtri o la query di ricerca.</p>
          <button on:click={clearFilters} class="btn-secondary mt-4">Reset filtri</button>
        </div>
      {:else}
        <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {#each results as product, index}
            {@const stock = getProductStock(product.id)}
            {@const rating = getProductRating(product.id)}
            {@const reviews = getProductReviewCount(product.id)}
            {@const category = getProductCategory(product)}

            <article
              class="surface-card pop-in flex flex-col overflow-hidden transition hover:-translate-y-0.5 hover:shadow-md"
              style="animation-delay: {Math.min(index, 9) * 35}ms"
            >
              <a href="/product/{product.id}" class="block overflow-hidden border-b border-[#f0f0f1] bg-[#fafafa]">
                <img
                  src={getProductImage(product.sku, 640, 420)}
                  alt={product.name}
                  class="aspect-[4/3] w-full object-cover transition duration-300 hover:scale-105"
                  loading="lazy"
                />
              </a>

              <div class="flex flex-1 flex-col gap-3 p-4">
                <div class="flex items-center justify-between gap-2">
                  <span class="status-pill bg-[#f1f8f5] text-[#005940]">{category}</span>
                  <span class="text-xs text-[#4a4f55]">★ {rating} ({reviews})</span>
                </div>

                <div class="flex-1">
                  <a href="/product/{product.id}">
                    <h2 class="font-title text-lg font-bold leading-snug text-[#202223]">{product.name}</h2>
                  </a>
                  <p class="mt-1 line-clamp-2 text-sm text-[#616161]">
                    {product.description || 'Descrizione non disponibile per questo articolo.'}
                  </p>
                </div>

                <div class="flex items-center justify-between text-xs">
                  <span class="font-semibold {STOCK_COLORS[stock]}">{STOCK_LABELS[stock]}</span>
                  <span class="text-[#8c9196]">{product.sku}</span>
                </div>

                <div class="border-t border-[#f0f0f1] pt-3">
                  <div class="mb-3 flex items-baseline justify-between">
                    <p class="text-2xl font-bold text-[#202223]">{formatCurrency(product.price)}</p>
                    <p class="text-xs text-[#8c9196]">IVA inclusa</p>
                  </div>
                  <button
                    on:click={() => addToCart(product)}
                    disabled={addingProductId === product.id}
                    class="btn-primary w-full"
                  >
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
</div>
