<script lang="ts">
  export let access: {
    catalog: boolean;
    orders: boolean;
    customers: boolean;
    users: boolean;
    shipments: boolean;
    warehouse: boolean;
  };

  $: hasAnySectionAccess =
    access.catalog ||
    access.orders ||
    access.customers ||
    access.users ||
    access.shipments ||
    access.warehouse;
</script>

<div class="space-y-6">
  <section class="surface-card p-6">
    <h1 class="text-3xl font-extrabold text-[#1c2430]">Dashboard Gestionale</h1>
    <p class="mt-2 text-sm text-[#5a6472]">
      Backoffice separato dallo store pubblico. Seleziona un'area operativa.
    </p>
  </section>

  {#if hasAnySectionAccess}
  <section class="grid gap-4 md:grid-cols-2 xl:grid-cols-6">
    {#if access.catalog}
    <a href="/catalog" class="surface-card p-5 transition hover:-translate-y-0.5 hover:shadow-sm">
      <p class="text-xs font-semibold uppercase tracking-[0.18em] text-[#3e4b5d]">Catalogo</p>
      <h2 class="mt-2 text-xl font-extrabold text-[#1c2430]">Prodotti</h2>
      <p class="mt-2 text-sm text-[#5a6472]">Crea, modifica ed elimina articoli.</p>
    </a>
    {/if}

    {#if access.orders}
    <a href="/orders" class="surface-card p-5 transition hover:-translate-y-0.5 hover:shadow-sm">
      <p class="text-xs font-semibold uppercase tracking-[0.18em] text-[#3e4b5d]">Ordini</p>
      <h2 class="mt-2 text-xl font-extrabold text-[#1c2430]">Tracking</h2>
      <p class="mt-2 text-sm text-[#5a6472]">Consulta stato e dettagli ordine.</p>
    </a>
    {/if}

    {#if access.customers}
    <a href="/customers" class="surface-card p-5 transition hover:-translate-y-0.5 hover:shadow-sm">
      <p class="text-xs font-semibold uppercase tracking-[0.18em] text-[#3e4b5d]">Clienti</p>
      <h2 class="mt-2 text-xl font-extrabold text-[#1c2430]">Anagrafiche</h2>
      <p class="mt-2 text-sm text-[#5a6472]">Visualizza e gestisci profili, indirizzi e password.</p>
    </a>
    {/if}

    {#if access.users}
    <a href="/users" class="surface-card p-5 transition hover:-translate-y-0.5 hover:shadow-sm">
      <p class="text-xs font-semibold uppercase tracking-[0.18em] text-[#3e4b5d]">Accessi</p>
      <h2 class="mt-2 text-xl font-extrabold text-[#1c2430]">Utenti</h2>
      <p class="mt-2 text-sm text-[#5a6472]">Crea amministratori, resetta password e gestisci accessi.</p>
    </a>
    {/if}

    {#if access.shipments}
    <a href="/shipments" class="surface-card p-5 transition hover:-translate-y-0.5 hover:shadow-sm">
      <p class="text-xs font-semibold uppercase tracking-[0.18em] text-[#3e4b5d]">Spedizioni</p>
      <h2 class="mt-2 text-xl font-extrabold text-[#1c2430]">Workflow</h2>
      <p class="mt-2 text-sm text-[#5a6472]">Aggiorna stati e monitora tracking code.</p>
    </a>
    {/if}

    {#if access.warehouse}
    <a href="/warehouse" class="surface-card p-5 transition hover:-translate-y-0.5 hover:shadow-sm">
      <p class="text-xs font-semibold uppercase tracking-[0.18em] text-[#3e4b5d]">Magazzino</p>
      <h2 class="mt-2 text-xl font-extrabold text-[#1c2430]">Stock</h2>
      <p class="mt-2 text-sm text-[#5a6472]">Aggiorna disponibilita prodotti.</p>
    </a>
    {/if}
  </section>
  {:else}
  <section class="surface-card p-5">
    <p class="text-sm text-[#5a6472]">
      Nessuna area operativa disponibile per il profilo corrente. Contattare un amministratore per l'assegnazione delle permission.
    </p>
  </section>
  {/if}
</div>
