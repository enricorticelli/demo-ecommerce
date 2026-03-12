<script lang="ts">
  import { onMount } from 'svelte';
  import {
    fetchProducts,
    fetchWarehouseStockByProducts,
    upsertStock,
    type Product
  } from '../lib/api';

  export let canWrite = false;

  let products: Product[] = [];
  let loading = true;
  let syncingStock = false;
  let saving = false;
  let savingProductId = '';
  let message = '';
  let error = '';

  const pageSize = 20;
  let currentPage = 1;
  let hasNextPage = false;

  let searchTerm = '';
  let appliedSearchTerm = '';

  let lowStockOnly = false;
  let lowStockThreshold = 15;

  let selectedProductId = '';
  let selectedProduct: Product | null = null;
  let quickQuantity = 0;

  let stockByProductId: Record<string, number> = {};
  let rowQuantityByProductId: Record<string, number> = {};

  function toInt(value: number, min = 0): number {
    if (!Number.isFinite(value)) return min;
    return Math.max(min, Math.trunc(value));
  }

  function normalizedThreshold(): number {
    return toInt(lowStockThreshold, 0);
  }

  function getSelectedProduct(): Product | null {
    if (!selectedProductId) return null;
    return products.find((product) => product.id === selectedProductId) ?? null;
  }

  $: selectedProduct = getSelectedProduct();

  function getCurrentStock(productId: string): number {
    return toInt(stockByProductId[productId] ?? 0, 0);
  }

  function getRowQuantity(productId: string): number {
    if (productId in rowQuantityByProductId) {
      return toInt(rowQuantityByProductId[productId], 0);
    }

    return getCurrentStock(productId);
  }

  function setRowQuantity(productId: string, value: number) {
    const normalizedValue = toInt(value, 0);
    rowQuantityByProductId = {
      ...rowQuantityByProductId,
      [productId]: normalizedValue
    };

    if (selectedProductId === productId) {
      quickQuantity = normalizedValue;
    }
  }

  function getDelta(productId: string): number {
    return getRowQuantity(productId) - getCurrentStock(productId);
  }

  function selectProduct(product: Product) {
    selectedProductId = product.id;
    quickQuantity = getRowQuantity(product.id);
  }

  function updateQuickQuantity(value: number) {
    quickQuantity = toInt(value, 0);
    if (selectedProductId) {
      setRowQuantity(selectedProductId, quickQuantity);
    }
  }

  function changeQuickBy(delta: number) {
    updateQuickQuantity(quickQuantity + delta);
  }

  function getInputValue(event: Event): number {
    const target = event.currentTarget as HTMLInputElement | null;
    return Number(target?.value ?? 0);
  }

  function handleQuickInput(event: Event) {
    updateQuickQuantity(getInputValue(event));
  }

  function handleRowQuantityInput(productId: string, event: Event) {
    setRowQuantity(productId, getInputValue(event));
  }

  function useCurrentStockForQuick() {
    if (!selectedProductId) return;
    updateQuickQuantity(getCurrentStock(selectedProductId));
  }

  function useThresholdForQuick() {
    updateQuickQuantity(normalizedThreshold());
  }

  async function syncStockForProducts(inputProducts: Product[]) {
    syncingStock = true;

    try {
      const ids = inputProducts.map((product) => product.id);
      const stockItems = await fetchWarehouseStockByProducts(
        ids,
        lowStockOnly ? normalizedThreshold() : null
      );

      const stockMap: Record<string, number> = {};
      ids.forEach((id) => {
        stockMap[id] = getCurrentStock(id);
      });

      stockItems.forEach((item) => {
        stockMap[item.productId] = toInt(item.availableQuantity, 0);
      });

      let visibleProducts = inputProducts;
      if (lowStockOnly) {
        const lowStockIds = new Set(stockItems.map((item) => item.productId));
        visibleProducts = inputProducts.filter((product) => lowStockIds.has(product.id));
      }

      products = visibleProducts;
      stockByProductId = stockMap;

      const nextRowQuantityByProductId: Record<string, number> = {};
      products.forEach((product) => {
        const existing = rowQuantityByProductId[product.id];
        nextRowQuantityByProductId[product.id] =
          typeof existing === 'number' ? toInt(existing, 0) : getCurrentStock(product.id);
      });
      rowQuantityByProductId = nextRowQuantityByProductId;

      if (!selectedProductId && products.length > 0) {
        selectProduct(products[0]);
      } else if (selectedProductId && !products.some((product) => product.id === selectedProductId)) {
        selectedProductId = '';
      }
    } finally {
      syncingStock = false;
    }
  }

  async function loadProducts(page = currentPage) {
    loading = true;
    error = '';
    currentPage = Math.max(1, page);

    try {
      const offset = (currentPage - 1) * pageSize;
      const rawProducts = await fetchProducts({
        limit: 200,
        offset: 0,
        searchTerm: appliedSearchTerm
      });

      const pagedProducts = rawProducts.slice(offset, offset + pageSize);
      hasNextPage = rawProducts.length > offset + pageSize;

      if (pagedProducts.length === 0 && currentPage > 1) {
        currentPage = 1;
        await syncStockForProducts(rawProducts.slice(0, pageSize));
        hasNextPage = rawProducts.length > pageSize;
        return;
      }

      await syncStockForProducts(pagedProducts);
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore caricamento magazzino';
    } finally {
      loading = false;
    }
  }

  async function refreshStockOnly() {
    if (loading || saving) return;
    error = '';

    try {
      const offset = (currentPage - 1) * pageSize;
      const rawProducts = await fetchProducts({
        limit: 200,
        offset: 0,
        searchTerm: appliedSearchTerm
      });

      const pagedProducts = rawProducts.slice(offset, offset + pageSize);
      hasNextPage = rawProducts.length > offset + pageSize;

      if (pagedProducts.length === 0 && currentPage > 1) {
        currentPage = 1;
        await syncStockForProducts(rawProducts.slice(0, pageSize));
        hasNextPage = rawProducts.length > pageSize;
        return;
      }

      await syncStockForProducts(pagedProducts);
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore sincronizzazione stock';
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

  async function toggleLowStockFilter() {
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
    if (!canWrite) return;
    const normalizedQuantity = toInt(quantity, 0);
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

      stockByProductId = {
        ...stockByProductId,
        [product.id]: normalizedQuantity
      };

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
    if (!canWrite) return;
    const selected = getSelectedProduct();
    if (!selected || saving) return;
    await saveStockForProduct(selected, quickQuantity);
  }

  onMount(() => {
    void loadProducts(1);
  });
</script>

<div class="space-y-6">
  <section class="surface-card p-5">
    <h1 class="text-3xl font-extrabold text-[#1c2430]">Magazzino</h1>
    <p class="mt-2 text-sm text-[#5a6472]">
      Vista operativa con stock reale, filtro low-stock e aggiornamento guidato per SKU.
    </p>
    {#if message}
      <p class="mt-3 rounded-lg bg-emerald-50 px-3 py-2 text-sm text-emerald-700">{message}</p>
    {/if}
    {#if error}
      <p class="mt-3 rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700">{error}</p>
    {/if}
    {#if !canWrite}
      <p class="mt-3 rounded-lg bg-amber-50 px-3 py-2 text-sm text-amber-700">
        Permesso mancante: warehouse:write. Aggiornamento stock disabilitato.
      </p>
    {/if}
  </section>

  <section class="surface-card p-5">
    <div class="grid gap-3 lg:grid-cols-[2fr_auto_auto]">
      <div>
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
    </div>

    <div class="mt-4 grid gap-3 md:grid-cols-[auto_auto_auto_1fr] md:items-end">
      <label class="inline-flex items-center gap-2 text-sm text-[#1c2430]">
        <input type="checkbox" bind:checked={lowStockOnly} on:change={toggleLowStockFilter} disabled={loading || saving} />
        Solo low stock
      </label>
      <div>
        <label class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]" for="low-stock-threshold">
          Soglia low stock
        </label>
        <input
          id="low-stock-threshold"
          class="form-input max-w-[140px]"
          type="number"
          min="0"
          bind:value={lowStockThreshold}
          on:change={toggleLowStockFilter}
          disabled={loading || saving}
        />
      </div>
      <button class="btn-secondary" on:click={refreshStockOnly} disabled={loading || saving || syncingStock}>
        {syncingStock ? 'Sync stock...' : 'Sync stock'}
      </button>
      <p class="text-sm text-[#5a6472] md:text-right">
        Pagina {currentPage}
        {#if appliedSearchTerm}
          · filtro: <span class="font-semibold text-[#1c2430]">{appliedSearchTerm}</span>
        {/if}
      </p>
    </div>

    <div class="mt-3 flex justify-end gap-2">
      <button class="btn-secondary" on:click={goToPrevPage} disabled={loading || saving || currentPage === 1}>Precedente</button>
      <button class="btn-secondary" on:click={goToNextPage} disabled={loading || saving || !hasNextPage}>Successiva</button>
    </div>
  </section>

  <section class="surface-card p-5">
    <h2 class="text-2xl font-bold text-[#1c2430]">Pannello rapido</h2>
    {#if selectedProduct}
      <div class="mt-4 grid gap-3 xl:grid-cols-[1.4fr_1fr_1fr_auto]">
        <div class="rounded-xl border border-[#d9dee8] bg-[#fcfdff] p-4">
          <p class="text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Prodotto selezionato</p>
          <p class="mt-1 font-mono text-xs text-[#1c2430]">{selectedProduct.sku}</p>
          <p class="mt-1 text-sm text-[#1c2430]">{selectedProduct.name}</p>
          <p class="mt-2 text-sm text-[#5a6472]">
            Stock attuale: <span class="font-semibold text-[#1c2430]">{getCurrentStock(selectedProduct.id)}</span>
          </p>
          <p class="mt-1 text-sm text-[#5a6472]">
            Delta: <span class={getDelta(selectedProduct.id) >= 0 ? 'font-semibold text-emerald-700' : 'font-semibold text-rose-700'}>{getDelta(selectedProduct.id)}</span>
          </p>
        </div>

        <div class="space-y-2">
          <label class="block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]" for="quick-stock-qty">
            Quantita target
          </label>
          <input
            id="quick-stock-qty"
            class="form-input"
            type="number"
            min="0"
            bind:value={quickQuantity}
            on:input={handleQuickInput}
            disabled={!canWrite}
          />
          <div class="flex flex-wrap gap-2">
            <button class="btn-secondary !px-3" on:click={() => changeQuickBy(-10)} disabled={!canWrite || saving || quickQuantity === 0}>-10</button>
            <button class="btn-secondary !px-3" on:click={() => changeQuickBy(-5)} disabled={!canWrite || saving || quickQuantity === 0}>-5</button>
            <button class="btn-secondary !px-3" on:click={() => changeQuickBy(5)} disabled={!canWrite || saving}>+5</button>
            <button class="btn-secondary !px-3" on:click={() => changeQuickBy(10)} disabled={!canWrite || saving}>+10</button>
          </div>
        </div>

        <div class="space-y-2">
          <p class="text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Preset intelligenti</p>
          <button class="btn-secondary w-full" on:click={useCurrentStockForQuick} disabled={!canWrite || saving}>Usa stock attuale</button>
          <button class="btn-secondary w-full" on:click={useThresholdForQuick} disabled={!canWrite || saving}>Porta a soglia ({normalizedThreshold()})</button>
        </div>

        <div class="flex items-end">
          <button class="btn-primary" on:click={saveQuickStock} disabled={!canWrite || saving}>
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
        <table class="w-full min-w-[1120px] text-left text-sm">
          <thead>
            <tr class="border-b border-[#d9dee8] text-[#5a6472]">
              <th class="px-2 py-2">SKU</th>
              <th class="px-2 py-2">Nome</th>
              <th class="px-2 py-2">Stock attuale</th>
              <th class="px-2 py-2">Target</th>
              <th class="px-2 py-2">Delta</th>
              <th class="px-2 py-2 text-right">Azioni</th>
            </tr>
          </thead>
          <tbody>
            {#each products as product}
              <tr class={`border-b border-[#edf1f7] ${selectedProductId === product.id ? 'bg-[#f0f6ff]' : ''}`}>
                <td class="px-2 py-2 font-mono text-xs">{product.sku}</td>
                <td class="px-2 py-2">{product.name}</td>
                <td class="px-2 py-2">
                  <div class="flex items-center gap-2">
                    <span class="font-semibold text-[#1c2430]">{getCurrentStock(product.id)}</span>
                    {#if getCurrentStock(product.id) <= normalizedThreshold()}
                      <span class="rounded-full bg-amber-100 px-2 py-0.5 text-[11px] font-semibold text-amber-800">LOW</span>
                    {/if}
                  </div>
                </td>
                <td class="px-2 py-2">
                  <input
                    class="form-input max-w-[140px]"
                    type="number"
                    min="0"
                    value={getRowQuantity(product.id)}
                    on:input={(event) => handleRowQuantityInput(product.id, event)}
                    disabled={saving}
                  />
                </td>
                <td class="px-2 py-2">
                  <span class={getDelta(product.id) >= 0 ? 'font-semibold text-emerald-700' : 'font-semibold text-rose-700'}>{getDelta(product.id)}</span>
                </td>
                <td class="px-2 py-2">
                  <div class="flex justify-end gap-2">
                    <button class="btn-secondary" on:click={() => selectProduct(product)} disabled={saving}>
                      {selectedProductId === product.id ? 'Selezionato' : 'Seleziona'}
                    </button>
                    <button class="btn-secondary" on:click={() => setRowQuantity(product.id, getCurrentStock(product.id))} disabled={saving}>
                      Attuale
                    </button>
                    <button class="btn-secondary" on:click={() => setRowQuantity(product.id, normalizedThreshold())} disabled={saving}>
                      Soglia
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
                <td class="empty-row" colspan="6">
                  <div class="empty-box">
                    <p class="empty-title">Nessun prodotto disponibile</p>
                    <p class="empty-description">
                      Modifica la ricerca oppure disattiva il filtro low stock.
                    </p>
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
