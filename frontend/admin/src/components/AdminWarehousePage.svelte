<script lang="ts">
  import { onMount } from 'svelte';
  import { fetchProducts, upsertStock, type Product } from '../lib/api';

  let products: Product[] = [];
  let loading = true;
  let message = '';
  let error = '';
  const pageSize = 20;
  let currentPage = 1;
  let hasNextPage = false;

  let stockForm = {
    productId: '',
    sku: '',
    availableQuantity: 30
  };

  async function loadProducts(page = currentPage) {
    loading = true;
    currentPage = Math.max(1, page);
    try {
      const offset = (currentPage - 1) * pageSize;
      products = await fetchProducts({ limit: pageSize, offset });
      hasNextPage = products.length === pageSize;
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore caricamento prodotti';
    } finally {
      loading = false;
    }
  }

  async function goToPrevPage() {
    if (currentPage <= 1 || loading) return;
    await loadProducts(currentPage - 1);
  }

  async function goToNextPage() {
    if (!hasNextPage || loading) return;
    await loadProducts(currentPage + 1);
  }

  function useProduct(product: Product) {
    stockForm = {
      productId: product.id,
      sku: product.sku,
      availableQuantity: 40
    };
  }

  async function saveStock() {
    message = '';
    error = '';
    try {
      await upsertStock(stockForm);
      message = `Stock aggiornato per SKU ${stockForm.sku}`;
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore aggiornamento stock';
    }
  }

  onMount(loadProducts);
</script>

<div class="space-y-6">
  <section class="surface-card p-5">
    <h1 class="text-3xl font-extrabold text-[#1c2430]">Magazzino</h1>
    <p class="mt-2 text-sm text-[#5a6472]">Aggiorna disponibilita stock per prodotto.</p>
    {#if message}
      <p class="mt-3 rounded-lg bg-emerald-50 px-3 py-2 text-sm text-emerald-700">{message}</p>
    {/if}
    {#if error}
      <p class="mt-3 rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700">{error}</p>
    {/if}
  </section>

  <section class="surface-card p-5">
    <div class="grid gap-2 md:grid-cols-4">
      <input class="form-input md:col-span-2" bind:value={stockForm.productId} placeholder="Product ID (GUID)" />
      <input class="form-input" bind:value={stockForm.sku} placeholder="SKU" />
      <input class="form-input" type="number" min="0" bind:value={stockForm.availableQuantity} placeholder="Quantita" />
    </div>
    <div class="mt-3">
      <button class="btn-primary" on:click={saveStock}>Aggiorna stock</button>
    </div>
  </section>

  <section class="surface-card p-5">
    <div class="flex items-center justify-between gap-2">
      <h2 class="text-2xl font-bold text-[#1c2430]">Seleziona prodotto</h2>
      <div class="flex gap-2">
        <button class="btn-secondary" on:click={goToPrevPage} disabled={loading || currentPage === 1}>Precedente</button>
        <button class="btn-secondary" on:click={goToNextPage} disabled={loading || !hasNextPage}>Successiva</button>
      </div>
    </div>
    <p class="mt-2 text-sm text-[#5a6472]">Pagina {currentPage}</p>
    {#if loading}
      <div class="mt-4 h-24 animate-pulse rounded-xl bg-[#f0f4fb]"></div>
    {:else}
      <div class="mt-4 overflow-x-auto">
        <table class="w-full min-w-[760px] text-left text-sm">
          <thead>
            <tr class="border-b border-[#d9dee8] text-[#5a6472]">
              <th class="px-2 py-2">SKU</th>
              <th class="px-2 py-2">Nome</th>
              <th class="px-2 py-2">Azione</th>
            </tr>
          </thead>
          <tbody>
            {#each products as product}
              <tr class="border-b border-[#edf1f7]">
                <td class="px-2 py-2 font-mono text-xs">{product.sku}</td>
                <td class="px-2 py-2">{product.name}</td>
                <td class="px-2 py-2">
                  <button class="btn-secondary" on:click={() => useProduct(product)}>Usa nel form</button>
                </td>
              </tr>
            {:else}
              <tr>
                <td class="empty-row" colspan="3">
                  <div class="empty-box">
                    <p class="empty-title">Nessun prodotto disponibile</p>
                    <p class="empty-description">Aggiungi prodotti nel catalogo per gestire lo stock da qui.</p>
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
