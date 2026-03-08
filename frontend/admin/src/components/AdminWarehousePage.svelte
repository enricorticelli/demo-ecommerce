<script lang="ts">
  import { onMount } from 'svelte';
  import { fetchProducts, upsertStock, type Product } from '../lib/api';

  let products: Product[] = [];
  let loading = true;
  let saving = false;
  let savingProductId = '';
  let message = '';
  let error = '';
  const pageSize = 20;
  let currentPage = 1;
  let hasNextPage = false;

  let searchTerm = '';
  let appliedSearchTerm = '';

  let selectedProductId = '';
  let selectedProduct: Product | null = null;
  let quickQuantity = 40;
  let rowQuantityByProductId: Record<string, number> = {};

  function getSelectedProduct(): Product | null {
    if (!selectedProductId) return null;
    return products.find((product) => product.id === selectedProductId) ?? null;
  }

  $: selectedProduct = getSelectedProduct();

  function getRowQuantity(productId: string): number {
    return rowQuantityByProductId[productId] ?? 40;
  }

  function setRowQuantity(productId: string, value: number) {
    rowQuantityByProductId = {
      ...rowQuantityByProductId,
      [productId]: Math.max(0, Number.isFinite(value) ? Math.trunc(value) : 0)
    };

    if (selectedProductId === productId) {
      quickQuantity = getRowQuantity(productId);
    }
  }

  function selectProduct(product: Product) {
    selectedProductId = product.id;
    quickQuantity = getRowQuantity(product.id);
  }

  function updateQuickQuantity(value: number) {
    quickQuantity = Math.max(0, Math.trunc(value));
    if (selectedProductId) {
      setRowQuantity(selectedProductId, quickQuantity);
    }
  }

  function changeQuickBy(delta: number) {
    updateQuickQuantity(quickQuantity + delta);
  }

  async function loadProducts(page = currentPage) {
    loading = true;
    error = '';
    currentPage = Math.max(1, page);

    try {
      const offset = (currentPage - 1) * pageSize;
      products = await fetchProducts({ limit: pageSize, offset, searchTerm: appliedSearchTerm });
      hasNextPage = products.length === pageSize;

      rowQuantityByProductId = products.reduce<Record<string, number>>((acc, product) => {
        acc[product.id] = rowQuantityByProductId[product.id] ?? 40;
        return acc;
      }, {});

      if (!selectedProductId && products.length > 0) {
        selectProduct(products[0]);
      } else if (selectedProductId && !products.some((product) => product.id === selectedProductId)) {
        selectedProductId = '';
      }
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore caricamento prodotti';
    } finally {
      loading = false;
    }
  }

  async function applySearch() {
    appliedSearchTerm = searchTerm.trim();
    await loadProducts(1);
  }

  async function clearSearch() {
    searchTerm = '';
    appliedSearchTerm = '';
    await loadProducts(1);
  }

  async function goToPrevPage() {
    if (currentPage <= 1 || loading || saving) return;
    await loadProducts(currentPage - 1);
  }

  async function goToNextPage() {
    if (!hasNextPage || loading || saving) return;
    await loadProducts(currentPage + 1);
  }

  async function saveStockForProduct(product: Product, quantity: number) {
    const normalizedQuantity = Math.max(0, Math.trunc(quantity));
    saving = true;
    savingProductId = product.id;
    message = '';
    error = '';

    try {
      await upsertStock({
        productId: product.id,
        sku: product.sku,
        availableQuantity: normalizedQuantity
      });
      setRowQuantity(product.id, normalizedQuantity);
      message = `Stock aggiornato: ${product.sku} -> ${normalizedQuantity}`;
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore aggiornamento stock';
    } finally {
      saving = false;
      savingProductId = '';
    }
  }

  async function saveQuickStock() {
    const selected = getSelectedProduct();
    if (!selected || saving) return;
    await saveStockForProduct(selected, quickQuantity);
  }

  onMount(loadProducts);
</script>

<div class="space-y-6">
  <section class="surface-card p-5">
    <h1 class="text-3xl font-extrabold text-[#1c2430]">Magazzino</h1>
    <p class="mt-2 text-sm text-[#5a6472]">Operativita stock con ricerca server-side, aggiornamento rapido SKU e update per riga.</p>
    {#if message}
      <p class="mt-3 rounded-lg bg-emerald-50 px-3 py-2 text-sm text-emerald-700">{message}</p>
    {/if}
    {#if error}
      <p class="mt-3 rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700">{error}</p>
    {/if}
  </section>

  <section class="surface-card p-5">
    <div class="flex flex-wrap items-end gap-2">
      <div class="min-w-[320px] flex-1">
        <label class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]" for="warehouse-search">
          Ricerca prodotti
        </label>
        <input
          id="warehouse-search"
          class="form-input"
          bind:value={searchTerm}
          placeholder="SKU, nome, descrizione..."
          on:keydown={(event) => {
            if (event.key === 'Enter') {
              applySearch();
            }
          }}
        />
      </div>
      <button class="btn-primary" on:click={applySearch} disabled={loading || saving}>Cerca</button>
      <button class="btn-secondary" on:click={clearSearch} disabled={loading || saving || !appliedSearchTerm}>Reset</button>
      <button class="btn-secondary" on:click={() => loadProducts()} disabled={loading || saving}>
        {loading ? 'Aggiornamento...' : 'Aggiorna'}
      </button>
    </div>
    <div class="mt-3 flex items-center justify-between gap-2 text-sm text-[#5a6472]">
      <p>
        Pagina {currentPage}
        {#if appliedSearchTerm}
          · filtro: <span class="font-semibold text-[#1c2430]">{appliedSearchTerm}</span>
        {/if}
      </p>
      <div class="flex gap-2">
        <button class="btn-secondary" on:click={goToPrevPage} disabled={loading || saving || currentPage === 1}>Precedente</button>
        <button class="btn-secondary" on:click={goToNextPage} disabled={loading || saving || !hasNextPage}>Successiva</button>
      </div>
    </div>
  </section>

  <section class="surface-card p-5">
    <h2 class="text-2xl font-bold text-[#1c2430]">Aggiornamento rapido</h2>
    {#if selectedProduct}
      <div class="mt-4 grid gap-3 lg:grid-cols-[1.5fr_1fr_auto]">
        <div class="rounded-xl border border-[#d9dee8] bg-[#fcfdff] p-4">
          <p class="text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Prodotto selezionato</p>
          <p class="mt-1 font-mono text-xs text-[#1c2430]">{selectedProduct.sku}</p>
          <p class="mt-1 text-sm text-[#1c2430]">{selectedProduct.name}</p>
          <p class="mt-1 break-all font-mono text-[11px] text-[#5a6472]">{selectedProduct.id}</p>
        </div>
        <div class="space-y-2">
          <label class="block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]" for="quick-stock-qty">
            Nuova quantita
          </label>
          <input
            id="quick-stock-qty"
            class="form-input"
            type="number"
            min="0"
            bind:value={quickQuantity}
            on:input={(event) => updateQuickQuantity(Number((event.currentTarget as HTMLInputElement).value))}
          />
          <div class="flex flex-wrap gap-2">
            <button class="btn-secondary !px-3" on:click={() => changeQuickBy(-25)} disabled={saving || quickQuantity === 0}>-25</button>
            <button class="btn-secondary !px-3" on:click={() => changeQuickBy(-10)} disabled={saving || quickQuantity === 0}>-10</button>
            <button class="btn-secondary !px-3" on:click={() => changeQuickBy(10)} disabled={saving}>+10</button>
            <button class="btn-secondary !px-3" on:click={() => changeQuickBy(25)} disabled={saving}>+25</button>
          </div>
        </div>
        <div class="flex items-end">
          <button class="btn-primary" on:click={saveQuickStock} disabled={saving}>
            {saving && savingProductId === selectedProduct.id ? 'Aggiornamento...' : 'Aggiorna stock'}
          </button>
        </div>
      </div>
    {:else}
      <p class="mt-3 text-sm text-[#5a6472]">Seleziona un prodotto dalla tabella per operare rapidamente.</p>
    {/if}
  </section>

  <section class="surface-card p-5">
    <h2 class="text-2xl font-bold text-[#1c2430]">Lista prodotti</h2>
    {#if loading}
      <div class="mt-4 h-24 animate-pulse rounded-xl bg-[#f0f4fb]"></div>
    {:else}
      <div class="mt-4 overflow-x-auto">
        <table class="w-full min-w-[980px] text-left text-sm">
          <thead>
            <tr class="border-b border-[#d9dee8] text-[#5a6472]">
              <th class="px-2 py-2">SKU</th>
              <th class="px-2 py-2">Nome</th>
              <th class="px-2 py-2">Quantita da impostare</th>
              <th class="px-2 py-2 text-right">Azioni</th>
            </tr>
          </thead>
          <tbody>
            {#each products as product}
              <tr class={`border-b border-[#edf1f7] ${selectedProductId === product.id ? 'bg-[#f0f6ff]' : ''}`}>
                <td class="px-2 py-2 font-mono text-xs">{product.sku}</td>
                <td class="px-2 py-2">{product.name}</td>
                <td class="px-2 py-2">
                  <input
                    class="form-input max-w-[140px]"
                    type="number"
                    min="0"
                    value={getRowQuantity(product.id)}
                    on:input={(event) => setRowQuantity(product.id, Number((event.currentTarget as HTMLInputElement).value))}
                    disabled={saving}
                  />
                </td>
                <td class="px-2 py-2">
                  <div class="flex justify-end gap-2">
                    <button class="btn-secondary" on:click={() => selectProduct(product)} disabled={saving}>
                      Seleziona
                    </button>
                    <button
                      class="btn-primary"
                      on:click={() => saveStockForProduct(product, getRowQuantity(product.id))}
                      disabled={saving}
                    >
                      {saving && savingProductId === product.id ? '...' : 'Aggiorna'}
                    </button>
                  </div>
                </td>
              </tr>
            {:else}
              <tr>
                <td class="empty-row" colspan="4">
                  <div class="empty-box">
                    <p class="empty-title">Nessun prodotto disponibile</p>
                    <p class="empty-description">Modifica il filtro o aggiungi prodotti dal catalogo.</p>
                  </div>
                </td>
              </tr>
            {/each}
          </tbody>
        </table>
      </div>
    {/if}
  </section>
</div>
