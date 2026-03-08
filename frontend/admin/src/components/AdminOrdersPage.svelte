<script lang="ts">
  import { onMount } from 'svelte';
  import { fetchOrders, type OrderView } from '../lib/api';

  let orders: OrderView[] = [];
  let error = '';
  let loadingOrders = false;
  const pageSize = 20;
  let currentPage = 1;
  let hasNextPage = false;
  let searchTerm = '';
  let appliedSearchTerm = '';

  async function loadOrders(page = currentPage) {
    loadingOrders = true;
    error = '';
    currentPage = Math.max(1, page);

    try {
      const offset = (currentPage - 1) * pageSize;
      orders = await fetchOrders(pageSize, offset, appliedSearchTerm);
      hasNextPage = orders.length === pageSize;
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore caricamento lista ordini';
    } finally {
      loadingOrders = false;
    }
  }

  async function goToPrevPage() {
    if (currentPage <= 1 || loadingOrders) return;
    await loadOrders(currentPage - 1);
  }

  async function goToNextPage() {
    if (!hasNextPage || loadingOrders) return;
    await loadOrders(currentPage + 1);
  }

  async function applySearch() {
    appliedSearchTerm = searchTerm.trim();
    await loadOrders(1);
  }

  async function clearSearch() {
    searchTerm = '';
    appliedSearchTerm = '';
    await loadOrders(1);
  }

  onMount(loadOrders);
</script>

<div class="space-y-6">
  <section class="surface-card p-5">
    <h1 class="text-3xl font-extrabold text-[#1c2430]">Ordini</h1>
    <p class="mt-2 text-sm text-[#5a6472]">Ricerca e monitoraggio stato ordine.</p>
  </section>

  <section class="surface-card p-5">
    <div class="flex flex-wrap items-end gap-2">
      <div class="min-w-[320px] flex-1">
        <label class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]" for="orders-search">
          Ricerca ordini
        </label>
        <input
          id="orders-search"
          class="form-input"
          bind:value={searchTerm}
          placeholder="Order ID, User ID, stato, payment, tracking..."
          on:keydown={(event) => {
            if (event.key === 'Enter') {
              applySearch();
            }
          }}
        />
      </div>
      <button class="btn-primary" on:click={applySearch} disabled={loadingOrders}>Cerca</button>
      <button class="btn-secondary" on:click={clearSearch} disabled={loadingOrders || !appliedSearchTerm}>Reset</button>
    </div>

    {#if error}
      <p class="mt-3 rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700">{error}</p>
    {/if}
  </section>

  <section class="surface-card p-5">
    <div class="mb-3 flex items-center justify-between">
      <h2 class="text-2xl font-bold text-[#1c2430]">Lista ordini</h2>
      <button class="btn-secondary" on:click={() => loadOrders()} disabled={loadingOrders}>
        {loadingOrders ? 'Aggiornamento...' : 'Aggiorna'}
      </button>
    </div>
    <div class="mb-3 flex items-center justify-between gap-2 text-sm text-[#5a6472]">
      <p>
        Pagina {currentPage}
        {#if appliedSearchTerm}
          · filtro: <span class="font-semibold text-[#1c2430]">{appliedSearchTerm}</span>
        {/if}
      </p>
      <div class="flex gap-2">
        <button class="btn-secondary" on:click={goToPrevPage} disabled={loadingOrders || currentPage === 1}>Precedente</button>
        <button class="btn-secondary" on:click={goToNextPage} disabled={loadingOrders || !hasNextPage}>Successiva</button>
      </div>
    </div>
    <div class="overflow-x-auto">
      <table class="w-full min-w-[860px] text-left text-sm">
        <thead>
          <tr class="border-b border-[#d9dee8] text-[#5a6472]">
            <th class="px-2 py-2">Order ID</th>
            <th class="px-2 py-2">Stato</th>
            <th class="px-2 py-2">Tipo utente</th>
            <th class="px-2 py-2">Totale</th>
            <th class="px-2 py-2">User</th>
            <th class="px-2 py-2 text-right">Azione</th>
          </tr>
        </thead>
        <tbody>
          {#each orders as item}
            <tr class="border-b border-[#edf1f7]">
              <td class="px-2 py-2 font-mono text-xs">{item.id}</td>
              <td class="px-2 py-2">{item.status}</td>
              <td class="px-2 py-2">{item.identityType}</td>
              <td class="px-2 py-2">EUR {item.totalAmount.toFixed(2)}</td>
              <td class="px-2 py-2 font-mono text-xs">
                {#if item.identityType === 'Registered'}
                  {item.authenticatedUserId ?? item.userId}
                {:else}
                  {item.anonymousId ?? item.userId}
                {/if}
              </td>
              <td class="px-2 py-2">
                <div class="flex justify-end">
                  <a class="btn-secondary" href={`/orders/${item.id}`}>Dettaglio</a>
                </div>
              </td>
            </tr>
          {:else}
            <tr>
              <td class="empty-row" colspan="6">
                <div class="empty-box">
                  <p class="empty-title">Nessun ordine da mostrare</p>
                  <p class="empty-description">Quando arriveranno nuovi ordini li vedrai in questa lista.</p>
                </div>
              </td>
            </tr>
          {/each}
        </tbody>
      </table>
    </div>
  </section>
</div>
