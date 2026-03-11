<script lang="ts">
  import { onMount } from 'svelte';
  import {
    createCustomerAddress,
    deleteCustomerAddress,
    fetchCustomer,
    fetchCustomerAddresses,
    fetchCustomers,
    resetCustomerPassword,
    updateCustomer,
    updateCustomerAddress,
    type AdminCustomer,
    type AdminCustomerAddress
  } from '../lib/api';

  const pageSize = 20;

  let loadingList = true;
  let loadingDetails = false;
  let savingProfile = false;
  let savingAddress = false;
  let savingPassword = false;

  let customers: AdminCustomer[] = [];
  let selectedCustomer: AdminCustomer | null = null;
  let addresses: AdminCustomerAddress[] = [];
  let selectedAddressId: string | null = null;

  let currentPage = 1;
  let hasNextPage = false;
  let searchTerm = '';
  let appliedSearchTerm = '';

  let profileForm = { firstName: '', lastName: '', phone: '' };
  let addressForm = {
    label: '',
    street: '',
    city: '',
    postalCode: '',
    country: '',
    isDefaultShipping: false,
    isDefaultBilling: false
  };
  let passwordForm = { newPassword: '', confirmPassword: '' };

  let message = '';
  let error = '';

  function resetForms() {
    profileForm = {
      firstName: selectedCustomer?.firstName ?? '',
      lastName: selectedCustomer?.lastName ?? '',
      phone: selectedCustomer?.phone ?? ''
    };

    addressForm = {
      label: '',
      street: '',
      city: '',
      postalCode: '',
      country: '',
      isDefaultShipping: false,
      isDefaultBilling: false
    };

    passwordForm = { newPassword: '', confirmPassword: '' };
    selectedAddressId = null;
  }

  async function loadCustomers(page = currentPage) {
    loadingList = true;
    error = '';
    currentPage = Math.max(1, page);

    try {
      const offset = (currentPage - 1) * pageSize;
      customers = await fetchCustomers(pageSize, offset, appliedSearchTerm);
      hasNextPage = customers.length === pageSize;

      if (customers.length === 0) {
        selectedCustomer = null;
        addresses = [];
        resetForms();
        return;
      }

      const hasCurrent = selectedCustomer && customers.some((x) => x.id === selectedCustomer?.id);
      const customerId = hasCurrent ? selectedCustomer!.id : customers[0].id;
      await selectCustomer(customerId);
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore caricamento clienti';
    } finally {
      loadingList = false;
    }
  }

  async function selectCustomer(customerId: string) {
    loadingDetails = true;
    error = '';

    try {
      const [customer, customerAddresses] = await Promise.all([
        fetchCustomer(customerId),
        fetchCustomerAddresses(customerId)
      ]);

      selectedCustomer = customer;
      addresses = customerAddresses;
      resetForms();
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore caricamento dettaglio cliente';
    } finally {
      loadingDetails = false;
    }
  }

  function editAddress(address: AdminCustomerAddress) {
    selectedAddressId = address.id;
    addressForm = {
      label: address.label,
      street: address.street,
      city: address.city,
      postalCode: address.postalCode,
      country: address.country,
      isDefaultShipping: address.isDefaultShipping,
      isDefaultBilling: address.isDefaultBilling
    };
  }

  function newAddress() {
    selectedAddressId = null;
    addressForm = {
      label: '',
      street: '',
      city: '',
      postalCode: '',
      country: '',
      isDefaultShipping: false,
      isDefaultBilling: false
    };
  }

  async function saveProfile() {
    if (!selectedCustomer || savingProfile) return;
    savingProfile = true;
    message = '';
    error = '';

    try {
      const updated = await updateCustomer(selectedCustomer.id, profileForm);
      selectedCustomer = updated;
      customers = customers.map((x) => (x.id === updated.id ? updated : x));
      message = 'Profilo cliente aggiornato.';
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore aggiornamento profilo cliente';
    } finally {
      savingProfile = false;
    }
  }

  async function saveAddress() {
    if (!selectedCustomer || savingAddress) return;
    savingAddress = true;
    message = '';
    error = '';

    try {
      if (selectedAddressId) {
        await updateCustomerAddress(selectedCustomer.id, selectedAddressId, addressForm);
        message = 'Indirizzo aggiornato.';
      } else {
        await createCustomerAddress(selectedCustomer.id, addressForm);
        message = 'Indirizzo creato.';
      }

      addresses = await fetchCustomerAddresses(selectedCustomer.id);
      newAddress();
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore gestione indirizzo';
    } finally {
      savingAddress = false;
    }
  }

  async function removeAddress(addressId: string) {
    if (!selectedCustomer || savingAddress) return;
    savingAddress = true;
    message = '';
    error = '';

    try {
      await deleteCustomerAddress(selectedCustomer.id, addressId);
      addresses = await fetchCustomerAddresses(selectedCustomer.id);
      if (selectedAddressId === addressId) {
        newAddress();
      }
      message = 'Indirizzo eliminato.';
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore eliminazione indirizzo';
    } finally {
      savingAddress = false;
    }
  }

  async function savePassword() {
    if (!selectedCustomer || savingPassword) return;
    if (passwordForm.newPassword.length < 8) {
      error = 'La password deve avere almeno 8 caratteri.';
      return;
    }

    if (passwordForm.newPassword !== passwordForm.confirmPassword) {
      error = 'Le password non coincidono.';
      return;
    }

    savingPassword = true;
    message = '';
    error = '';

    try {
      await resetCustomerPassword(selectedCustomer.id, passwordForm.newPassword);
      passwordForm = { newPassword: '', confirmPassword: '' };
      message = 'Password cliente aggiornata e sessioni revocate.';
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore reset password cliente';
    } finally {
      savingPassword = false;
    }
  }

  async function applySearch() {
    appliedSearchTerm = searchTerm.trim();
    await loadCustomers(1);
  }

  async function clearSearch() {
    searchTerm = '';
    appliedSearchTerm = '';
    await loadCustomers(1);
  }

  async function goToPrevPage() {
    if (currentPage <= 1 || loadingList) return;
    await loadCustomers(currentPage - 1);
  }

  async function goToNextPage() {
    if (!hasNextPage || loadingList) return;
    await loadCustomers(currentPage + 1);
  }

  onMount(() => {
    loadCustomers();
  });
</script>

<div class="space-y-6">
  <section class="surface-card p-5">
    <h1 class="text-3xl font-extrabold text-[#1c2430]">Clienti</h1>
    <p class="mt-2 text-sm text-[#5a6472]">Gestione anagrafica clienti, indirizzi e reset password amministrativo.</p>
    {#if message}
      <p class="mt-3 rounded-lg bg-emerald-50 px-3 py-2 text-sm text-emerald-700">{message}</p>
    {/if}
    {#if error}
      <p class="mt-3 rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700">{error}</p>
    {/if}
  </section>

  <section class="surface-card p-5">
    <div class="flex flex-wrap items-end gap-2">
      <div class="min-w-[300px] flex-1">
        <label class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]" for="customers-search">
          Cerca clienti
        </label>
        <input
          id="customers-search"
          class="form-input"
          bind:value={searchTerm}
          placeholder="email, nome, cognome, telefono..."
          on:keydown={(event) => {
            if (event.key === 'Enter') {
              applySearch();
            }
          }}
        />
      </div>
      <button class="btn-primary" on:click={applySearch} disabled={loadingList}>Cerca</button>
      <button class="btn-secondary" on:click={clearSearch} disabled={loadingList || !appliedSearchTerm}>Reset</button>
      <button class="btn-secondary" on:click={() => loadCustomers()} disabled={loadingList}>
        {loadingList ? 'Aggiornamento...' : 'Aggiorna'}
      </button>
    </div>
    <div class="mt-3 flex items-center justify-between gap-2 text-sm text-[#5a6472]">
      <p>Pagina {currentPage}</p>
      <div class="flex gap-2">
        <button class="btn-secondary" on:click={goToPrevPage} disabled={loadingList || currentPage === 1}>Precedente</button>
        <button class="btn-secondary" on:click={goToNextPage} disabled={loadingList || !hasNextPage}>Successiva</button>
      </div>
    </div>
  </section>

  <section class="grid gap-4 xl:grid-cols-[1fr_2fr]">
    <div class="surface-card p-4">
      <h2 class="text-xl font-bold text-[#1c2430]">Lista</h2>
      {#if loadingList}
        <div class="mt-4 h-28 animate-pulse rounded-xl bg-[#f0f4fb]"></div>
      {:else}
        <div class="mt-4 max-h-[560px] space-y-2 overflow-auto pr-1">
          {#if customers.length === 0}
            <p class="text-sm text-[#5a6472]">Nessun cliente trovato.</p>
          {:else}
            {#each customers as customer}
              <button
                class={`w-full rounded-xl border px-3 py-3 text-left transition ${
                  selectedCustomer?.id === customer.id
                    ? 'border-[#0b5fff] bg-[#eef5ff]'
                    : 'border-[#d9dee8] bg-white hover:bg-[#f8fbff]'
                }`}
                on:click={() => selectCustomer(customer.id)}
              >
                <p class="text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">{customer.username}</p>
                <p class="mt-1 text-sm font-semibold text-[#1c2430]">{customer.firstName} {customer.lastName}</p>
                <p class="mt-1 text-xs text-[#5a6472]">{customer.email}</p>
              </button>
            {/each}
          {/if}
        </div>
      {/if}
    </div>

    <div class="space-y-4">
      {#if !selectedCustomer}
        <div class="surface-card p-5">
          <p class="text-sm text-[#5a6472]">Seleziona un cliente per visualizzare e gestire i dettagli.</p>
        </div>
      {:else}
        <div class="surface-card p-5">
          <h2 class="text-xl font-bold text-[#1c2430]">Profilo cliente</h2>
          {#if loadingDetails}
            <div class="mt-4 h-24 animate-pulse rounded-xl bg-[#f0f4fb]"></div>
          {:else}
            <div class="mt-3 grid gap-3 md:grid-cols-2">
              <div>
                <label class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">ID</label>
                <input class="form-input font-mono text-xs" value={selectedCustomer.id} disabled />
              </div>
              <div>
                <label class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Email</label>
                <input class="form-input" value={selectedCustomer.email} disabled />
              </div>
              <div>
                <label class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Nome</label>
                <input class="form-input" bind:value={profileForm.firstName} />
              </div>
              <div>
                <label class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Cognome</label>
                <input class="form-input" bind:value={profileForm.lastName} />
              </div>
              <div>
                <label class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Telefono</label>
                <input class="form-input" bind:value={profileForm.phone} />
              </div>
              <div>
                <label class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Verificato</label>
                <input class="form-input" value={selectedCustomer.isEmailVerified ? 'Si' : 'No'} disabled />
              </div>
            </div>
            <div class="mt-4 flex justify-end">
              <button class="btn-primary" on:click={saveProfile} disabled={savingProfile}>
                {savingProfile ? 'Salvataggio...' : 'Salva profilo'}
              </button>
            </div>
          {/if}
        </div>

        <div class="surface-card p-5">
          <h2 class="text-xl font-bold text-[#1c2430]">Reset password</h2>
          <div class="mt-3 grid gap-3 md:grid-cols-2">
            <div>
              <label class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Nuova password</label>
              <input class="form-input" type="password" bind:value={passwordForm.newPassword} />
            </div>
            <div>
              <label class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Conferma password</label>
              <input class="form-input" type="password" bind:value={passwordForm.confirmPassword} />
            </div>
          </div>
          <div class="mt-4 flex justify-end">
            <button class="btn-primary" on:click={savePassword} disabled={savingPassword}>
              {savingPassword ? 'Aggiornamento...' : 'Reset password'}
            </button>
          </div>
        </div>

        <div class="surface-card p-5">
          <div class="flex items-center justify-between">
            <h2 class="text-xl font-bold text-[#1c2430]">Indirizzi</h2>
            <button class="btn-secondary" on:click={newAddress}>Nuovo indirizzo</button>
          </div>

          <div class="mt-3 overflow-x-auto">
            <table class="w-full min-w-[760px] text-left text-sm">
              <thead>
                <tr class="border-b border-[#d9dee8] text-[#5a6472]">
                  <th class="px-2 py-2">Label</th>
                  <th class="px-2 py-2">Via</th>
                  <th class="px-2 py-2">Citta</th>
                  <th class="px-2 py-2">CAP</th>
                  <th class="px-2 py-2">Nazione</th>
                  <th class="px-2 py-2 text-right">Azioni</th>
                </tr>
              </thead>
              <tbody>
                {#each addresses as address}
                  <tr class="border-b border-[#edf1f7]">
                    <td class="px-2 py-2">{address.label}</td>
                    <td class="px-2 py-2">{address.street}</td>
                    <td class="px-2 py-2">{address.city}</td>
                    <td class="px-2 py-2">{address.postalCode}</td>
                    <td class="px-2 py-2">{address.country}</td>
                    <td class="px-2 py-2 text-right">
                      <div class="inline-flex gap-2">
                        <button class="btn-secondary !px-3 !py-1.5 text-xs" on:click={() => editAddress(address)}>Modifica</button>
                        <button class="btn-secondary !px-3 !py-1.5 text-xs" on:click={() => removeAddress(address.id)}>Elimina</button>
                      </div>
                    </td>
                  </tr>
                {/each}
              </tbody>
            </table>
          </div>

          <div class="mt-4 rounded-xl border border-[#d9dee8] bg-[#fcfdff] p-4">
            <p class="text-sm font-semibold text-[#1c2430]">{selectedAddressId ? 'Modifica indirizzo' : 'Nuovo indirizzo'}</p>
            <div class="mt-3 grid gap-3 md:grid-cols-2">
              <input class="form-input" placeholder="Label" bind:value={addressForm.label} />
              <input class="form-input" placeholder="Via" bind:value={addressForm.street} />
              <input class="form-input" placeholder="Citta" bind:value={addressForm.city} />
              <input class="form-input" placeholder="CAP" bind:value={addressForm.postalCode} />
              <input class="form-input" placeholder="Nazione" bind:value={addressForm.country} />
              <div class="flex items-center gap-4">
                <label class="inline-flex items-center gap-2 text-sm text-[#1c2430]">
                  <input type="checkbox" bind:checked={addressForm.isDefaultShipping} />
                  Default spedizione
                </label>
                <label class="inline-flex items-center gap-2 text-sm text-[#1c2430]">
                  <input type="checkbox" bind:checked={addressForm.isDefaultBilling} />
                  Default fatturazione
                </label>
              </div>
            </div>
            <div class="mt-4 flex justify-end">
              <button class="btn-primary" on:click={saveAddress} disabled={savingAddress}>
                {savingAddress ? 'Salvataggio...' : selectedAddressId ? 'Aggiorna indirizzo' : 'Crea indirizzo'}
              </button>
            </div>
          </div>
        </div>
      {/if}
    </div>
  </section>
</div>
