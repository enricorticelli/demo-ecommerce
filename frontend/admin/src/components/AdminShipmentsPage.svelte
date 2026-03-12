<script lang="ts">
  import { onMount } from 'svelte';
  import {
    fetchShipments,
    updateShipmentStatus,
    type ShipmentView
  } from '../lib/api';

  export let canWrite = false;

  const pageSize = 20;
  const statuses: ShipmentView['status'][] = ['Preparing', 'Created', 'InTransit', 'Delivered', 'Cancelled'];

  let shipments: ShipmentView[] = [];
  let loading = false;
  let message = '';
  let error = '';

  let currentPage = 1;
  let hasNextPage = false;

  let searchTerm = '';
  let appliedSearchTerm = '';

  function formatDate(value: string | null): string {
    if (!value) return '-';
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return '-';
    return date.toLocaleString('it-IT');
  }

  async function loadShipments(page = currentPage) {
    loading = true;
    error = '';
    message = '';
    currentPage = Math.max(1, page);

    try {
      const offset = (currentPage - 1) * pageSize;
      const loadedShipments = await fetchShipments(200, 0, appliedSearchTerm);
      shipments = loadedShipments.slice(offset, offset + pageSize);
      hasNextPage = loadedShipments.length > offset + pageSize;

      if (shipments.length === 0 && currentPage > 1) {
        currentPage = 1;
        shipments = loadedShipments.slice(0, pageSize);
        hasNextPage = loadedShipments.length > pageSize;
      }
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore caricamento spedizioni';
    } finally {
      loading = false;
    }
  }

  async function goToPrevPage() {
    if (currentPage <= 1 || loading) return;
    await loadShipments(currentPage - 1);
  }

  async function goToNextPage() {
    if (!hasNextPage || loading) return;
    await loadShipments(currentPage + 1);
  }

  async function applySearch() {
    appliedSearchTerm = searchTerm.trim();
    await loadShipments(1);
  }

  async function clearSearch() {
    searchTerm = '';
    appliedSearchTerm = '';
    await loadShipments(1);
  }

  async function changeStatus(shipment: ShipmentView, status: ShipmentView['status']) {
    if (!canWrite) return;
    if (loading || shipment.status === status) return;

    loading = true;
    error = '';
    message = '';

    try {
      const updated = await updateShipmentStatus(shipment.id, status);
      shipments = shipments.map((item) => (item.id === updated.id ? updated : item));
      message = `Stato spedizione ${updated.trackingCode} aggiornato a ${updated.status}.`;
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore aggiornamento stato spedizione';
    } finally {
      loading = false;
    }
  }

  function handleStatusChange(shipment: ShipmentView, event: Event) {
    const target = event.currentTarget as HTMLSelectElement | null;
    const nextStatus = (target?.value ?? shipment.status) as ShipmentView['status'];
    void changeStatus(shipment, nextStatus);
  }

  onMount(loadShipments);
</script>

<div class="space-y-6">
  <section class="surface-card p-5">
    <h1 class="text-3xl font-extrabold text-[#1c2430]">Spedizioni</h1>
    <p class="mt-2 text-sm text-[#5a6472]">Gestione operativa dello stato spedizioni con filtro server-side.</p>

    {#if message}
      <p class="mt-3 rounded-lg bg-emerald-50 px-3 py-2 text-sm text-emerald-700">{message}</p>
    {/if}
    {#if error}
      <p class="mt-3 rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700">{error}</p>
    {/if}
    {#if !canWrite}
      <p class="mt-3 rounded-lg bg-amber-50 px-3 py-2 text-sm text-amber-700">
        Permesso mancante: shipping:write. Aggiornamento stato disabilitato.
      </p>
    {/if}
  </section>

  <section class="surface-card p-5">
    <div class="flex flex-wrap items-end gap-2">
      <div class="min-w-[280px] flex-1">
        <label class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]" for="shipments-search">
          Ricerca spedizioni
        </label>
        <input
          id="shipments-search"
          class="form-input"
          bind:value={searchTerm}
          placeholder="Tracking, orderId, userId, stato..."
          on:keydown={(event) => {
            if (event.key === 'Enter') {
              applySearch();
            }
          }}
        />
      </div>
      <button class="btn-primary" on:click={applySearch} disabled={loading}>Cerca</button>
      <button class="btn-secondary" on:click={clearSearch} disabled={loading || !appliedSearchTerm}>Reset</button>
    </div>
  </section>

  <section class="surface-card p-5">
    <div class="mb-3 flex items-center justify-between gap-2">
      <h2 class="text-2xl font-bold text-[#1c2430]">Lista spedizioni</h2>
      <button class="btn-secondary" on:click={() => loadShipments()} disabled={loading}>
        {loading ? 'Aggiornamento...' : 'Aggiorna'}
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
        <button class="btn-secondary" on:click={goToPrevPage} disabled={loading || currentPage === 1}>Precedente</button>
        <button class="btn-secondary" on:click={goToNextPage} disabled={loading || !hasNextPage}>Successiva</button>
      </div>
    </div>

    <div class="overflow-x-auto">
      <table class="w-full min-w-[1080px] text-left text-sm">
        <thead>
          <tr class="border-b border-[#d9dee8] text-[#5a6472]">
            <th class="px-2 py-2">Tracking</th>
            <th class="px-2 py-2">Stato</th>
            <th class="px-2 py-2">Order ID</th>
            <th class="px-2 py-2">User ID</th>
            <th class="px-2 py-2">Creata il</th>
            <th class="px-2 py-2">Ultimo update</th>
            <th class="px-2 py-2">Consegnata il</th>
            <th class="px-2 py-2">Azione</th>
          </tr>
        </thead>
        <tbody>
          {#each shipments as shipment}
            <tr class="border-b border-[#edf1f7]">
              <td class="px-2 py-2 font-mono text-xs">{shipment.trackingCode}</td>
              <td class="px-2 py-2">
                <span class="rounded-full border border-[#d9dee8] bg-[#f8fbff] px-2 py-0.5 text-xs font-semibold text-[#1c2430]">
                  {shipment.status}
                </span>
              </td>
              <td class="px-2 py-2 font-mono text-xs">{shipment.orderId}</td>
              <td class="px-2 py-2 font-mono text-xs">{shipment.userId}</td>
              <td class="px-2 py-2">{formatDate(shipment.createdAtUtc)}</td>
              <td class="px-2 py-2">{formatDate(shipment.updatedAtUtc)}</td>
              <td class="px-2 py-2">{formatDate(shipment.deliveredAtUtc)}</td>
              <td class="px-2 py-2">
                <select
                  class="form-input"
                  value={shipment.status}
                  on:change={(event) => handleStatusChange(shipment, event)}
                  disabled={loading || !canWrite}
                >
                  {#each statuses as status}
                    <option value={status}>{status}</option>
                  {/each}
                </select>
              </td>
            </tr>
          {:else}
            <tr>
              <td class="empty-row" colspan="8">
                <div class="empty-box">
                  <p class="empty-title">Nessuna spedizione da mostrare</p>
                  <p class="empty-description">Quando vengono create nuove spedizioni compariranno in questa lista.</p>
                </div>
              </td>
            </tr>
          {/each}
        </tbody>
      </table>
    </div>
  </section>
</div>
