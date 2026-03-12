<script lang="ts">
  import { onMount } from 'svelte';
  import {
    createAdminUser,
    deleteAdminUser,
    fetchAdminUsers,
    resetAdminUserPassword,
    updateAdminUserPermissions,
    type AdminAccountUser
  } from '../lib/api';
  import { ALL_ADMIN_PERMISSIONS, ADMIN_PERMISSION_LABEL, type AdminPermission } from '../lib/permissions';

  export let canWrite = false;

  const pageSize = 20;
  const domainOrder = ['catalog', 'orders', 'shipping', 'warehouse', 'account'];
  const domainLabel: Record<string, string> = {
    catalog: 'Catalogo',
    orders: 'Ordini',
    shipping: 'Spedizioni',
    warehouse: 'Magazzino',
    account: 'Account'
  };

  let users: AdminAccountUser[] = [];
  let selectedUser: AdminAccountUser | null = null;

  let loadingList = true;
  let creating = false;
  let savingPermissions = false;
  let savingPassword = false;
  let deleting = false;

  let currentPage = 1;
  let hasNextPage = false;
  let searchTerm = '';
  let appliedSearchTerm = '';

  let createForm = {
    username: '',
    password: '',
    permissions: [...ALL_ADMIN_PERMISSIONS] as AdminPermission[],
    isSuperUser: false
  };
  let resetPasswordForm = { newPassword: '', confirmPassword: '' };
  let permissionDraft: AdminPermission[] = [];
  let createPermissionSearch = '';
  let editPermissionSearch = '';
  let activeTab: 'create' | 'profile' | 'permissions' | 'security' = 'profile';

  let message = '';
  let error = '';

  type PermissionGroup = {
    key: string;
    title: string;
    permissions: AdminPermission[];
  };

  const permissionOrder = new Map(ALL_ADMIN_PERMISSIONS.map((permission, index) => [permission, index]));
  const readOnlyPermissions = ALL_ADMIN_PERMISSIONS.filter((permission) => permission.endsWith(':read'));

  function normalizePermissions(permissions: string[]): AdminPermission[] {
    const set = new Set(
      permissions.filter((permission): permission is AdminPermission =>
        ALL_ADMIN_PERMISSIONS.includes(permission as AdminPermission)
      )
    );

    return [...set].sort((a, b) => (permissionOrder.get(a) ?? 0) - (permissionOrder.get(b) ?? 0));
  }

  function getDomain(permission: AdminPermission): string {
    return permission.split(':', 1)[0] ?? 'other';
  }

  function getDomainRank(domain: string): number {
    const index = domainOrder.indexOf(domain);
    if (index >= 0) return index;
    return domainOrder.length + 1;
  }

  function getActionLabel(permission: AdminPermission): string {
    if (permission.endsWith(':read')) return 'Lettura';
    if (permission.endsWith(':write')) return 'Scrittura';
    return 'Accesso';
  }

  function buildPermissionGroups(searchValue: string): PermissionGroup[] {
    const term = searchValue.trim().toLowerCase();
    const grouped = new Map<string, AdminPermission[]>();

    for (const permission of ALL_ADMIN_PERMISSIONS) {
      const label = ADMIN_PERMISSION_LABEL[permission] ?? permission;
      const searchable = `${permission} ${label}`.toLowerCase();
      if (term && !searchable.includes(term)) {
        continue;
      }

      const domain = getDomain(permission);
      const list = grouped.get(domain) ?? [];
      list.push(permission);
      grouped.set(domain, list);
    }

    return [...grouped.entries()]
      .sort((a, b) => {
        const rankDiff = getDomainRank(a[0]) - getDomainRank(b[0]);
        if (rankDiff !== 0) return rankDiff;
        return a[0].localeCompare(b[0]);
      })
      .map(([key, permissions]) => ({
        key,
        title: domainLabel[key] ?? key,
        permissions
      }));
  }

  function getSelectedCountByGroup(groupPermissions: AdminPermission[], selectedPermissions: AdminPermission[]): number {
    const selected = new Set(selectedPermissions);
    return groupPermissions.filter((permission) => selected.has(permission)).length;
  }

  function togglePermission(current: AdminPermission[], permission: AdminPermission): AdminPermission[] {
    if (current.includes(permission)) {
      return current.filter((x) => x !== permission);
    }

    return normalizePermissions([...current, permission]);
  }

  function toggleCreatePermission(permission: AdminPermission) {
    createForm.permissions = togglePermission(createForm.permissions, permission);
  }

  function toggleDraftPermission(permission: AdminPermission) {
    permissionDraft = togglePermission(permissionDraft, permission);
  }

  function usePermissionPresetForCreate(mode: 'all' | 'readonly' | 'none') {
    if (mode === 'all') {
      createForm.permissions = [...ALL_ADMIN_PERMISSIONS];
      return;
    }

    if (mode === 'readonly') {
      createForm.permissions = [...readOnlyPermissions];
      return;
    }

    createForm.permissions = [];
  }

  function usePermissionPresetForEdit(mode: 'all' | 'readonly' | 'none') {
    if (mode === 'all') {
      permissionDraft = [...ALL_ADMIN_PERMISSIONS];
      return;
    }

    if (mode === 'readonly') {
      permissionDraft = [...readOnlyPermissions];
      return;
    }

    permissionDraft = [];
  }

  function selectUser(user: AdminAccountUser) {
    selectedUser = user;
    permissionDraft = normalizePermissions(user.permissions);
    resetPasswordForm = { newPassword: '', confirmPassword: '' };
    activeTab = 'profile';
  }

  function toCreatePermissionsPayload(permissions: AdminPermission[]): AdminPermission[] | null {
    if (permissions.length !== ALL_ADMIN_PERMISSIONS.length) {
      return permissions;
    }

    const requested = new Set(permissions);
    const isDefault = ALL_ADMIN_PERMISSIONS.every((permission) => requested.has(permission));
    return isDefault ? null : permissions;
  }

  async function loadUsers(page = currentPage, preferredUserId: string | null = selectedUser?.id ?? null) {
    loadingList = true;
    error = '';
    currentPage = Math.max(1, page);

    try {
      const offset = (currentPage - 1) * pageSize;
      const loadedUsers = await fetchAdminUsers(200, 0, appliedSearchTerm);
      users = loadedUsers.slice(offset, offset + pageSize);
      hasNextPage = loadedUsers.length > offset + pageSize;

      if (users.length === 0 && currentPage > 1) {
        currentPage = 1;
        users = loadedUsers.slice(0, pageSize);
        hasNextPage = loadedUsers.length > pageSize;
      }

      if (users.length === 0) {
        selectedUser = null;
        permissionDraft = [];
        resetPasswordForm = { newPassword: '', confirmPassword: '' };
        return;
      }

      const nextSelected = users.find((user) => user.id === preferredUserId) ?? users[0];
      selectUser(nextSelected);
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore caricamento utenti';
    } finally {
      loadingList = false;
    }
  }

  async function applySearch() {
    appliedSearchTerm = searchTerm.trim();
    await loadUsers(1);
  }

  async function clearSearch() {
    searchTerm = '';
    appliedSearchTerm = '';
    await loadUsers(1);
  }

  async function goToPrevPage() {
    if (currentPage <= 1 || isBusy) return;
    await loadUsers(currentPage - 1);
  }

  async function goToNextPage() {
    if (!hasNextPage || isBusy) return;
    await loadUsers(currentPage + 1);
  }

  async function createUser() {
    if (!canWrite) return;
    if (isBusy) return;
    if (!createForm.username.trim()) {
      error = 'Username obbligatorio.';
      return;
    }
    if (createForm.password.length < 8) {
      error = 'Password minima 8 caratteri.';
      return;
    }

    creating = true;
    message = '';
    error = '';

    try {
      const created = await createAdminUser({
        username: createForm.username.trim(),
        password: createForm.password,
        permissions: toCreatePermissionsPayload(createForm.permissions),
        isSuperUser: createForm.isSuperUser
      });

      createForm = {
        username: '',
        password: '',
        permissions: [...ALL_ADMIN_PERMISSIONS],
        isSuperUser: false
      };
      message = 'Utente admin creato.';
      await loadUsers(1, created.id);
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore creazione utente admin';
    } finally {
      creating = false;
    }
  }

  async function resetPassword() {
    if (!canWrite) return;
    if (!selectedUser || isBusy) return;

    if (resetPasswordForm.newPassword.length < 8) {
      error = 'Nuova password minima 8 caratteri.';
      return;
    }

    if (resetPasswordForm.newPassword !== resetPasswordForm.confirmPassword) {
      error = 'Le password non coincidono.';
      return;
    }

    savingPassword = true;
    message = '';
    error = '';

    try {
      await resetAdminUserPassword(selectedUser.id, resetPasswordForm.newPassword);
      resetPasswordForm = { newPassword: '', confirmPassword: '' };
      message = `Password aggiornata per ${selectedUser.username}.`;
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore reset password admin';
    } finally {
      savingPassword = false;
    }
  }

  async function removeUser() {
    if (!canWrite) return;
    if (!selectedUser || isBusy) return;

    const confirmed = globalThis.confirm(`Eliminare l'utente admin "${selectedUser.username}"?`);
    if (!confirmed) return;

    deleting = true;
    message = '';
    error = '';

    try {
      const selectedId = selectedUser.id;
      const fallbackUserId = users.find((user) => user.id !== selectedId)?.id ?? null;
      await deleteAdminUser(selectedId);
      message = `Utente ${selectedUser.username} eliminato.`;
      await loadUsers(currentPage, fallbackUserId);
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore eliminazione utente admin';
    } finally {
      deleting = false;
    }
  }

  async function savePermissions() {
    if (!canWrite) return;
    if (!selectedUser || isBusy) return;

    savingPermissions = true;
    message = '';
    error = '';

    try {
      await updateAdminUserPermissions(selectedUser.id, permissionDraft);
      message = `Permessi aggiornati per ${selectedUser.username}. Sessioni utente revocate.`;
      await loadUsers(currentPage, selectedUser.id);
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore aggiornamento permessi admin';
    } finally {
      savingPermissions = false;
    }
  }

  async function useDefaultPermissions() {
    if (!canWrite) return;
    if (!selectedUser || isBusy) return;

    savingPermissions = true;
    message = '';
    error = '';

    try {
      await updateAdminUserPermissions(selectedUser.id, null);
      message = `Permessi default ripristinati per ${selectedUser.username}. Sessioni utente revocate.`;
      await loadUsers(currentPage, selectedUser.id);
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore ripristino permessi default';
    } finally {
      savingPermissions = false;
    }
  }

  $: isBusy = loadingList || creating || savingPermissions || savingPassword || deleting;
  $: createPermissionGroups = buildPermissionGroups(createPermissionSearch);
  $: editPermissionGroups = buildPermissionGroups(editPermissionSearch);

  onMount(() => {
    loadUsers();
  });
</script>

<div class="space-y-6">
  <section class="surface-card p-5">
    <h1 class="text-3xl font-extrabold text-[#1c2430]">Utenti</h1>
    <p class="mt-2 text-sm text-[#5a6472]">
      Gestione utenti amministrativi: lista operativa, creazione, reset password e permessi scalabili per area.
    </p>
    {#if message}
      <p class="mt-3 rounded-lg bg-emerald-50 px-3 py-2 text-sm text-emerald-700">{message}</p>
    {/if}
    {#if error}
      <p class="mt-3 rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700">{error}</p>
    {/if}
    {#if !canWrite}
      <p class="mt-3 rounded-lg bg-amber-50 px-3 py-2 text-sm text-amber-700">
        Permesso mancante: account:write. Operazioni su utenti admin disabilitate.
      </p>
    {/if}
  </section>

  <section class="surface-card p-5">
    <div class="flex flex-wrap items-end gap-2">
      <div class="min-w-[300px] flex-1">
        <label class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]" for="users-search">
          Cerca utenti
        </label>
        <input
          id="users-search"
          class="form-input"
          bind:value={searchTerm}
          placeholder="username o email..."
          on:keydown={(event) => {
            if (event.key === 'Enter') {
              applySearch();
            }
          }}
        />
      </div>
      <button class="btn-primary" on:click={applySearch} disabled={isBusy}>Cerca</button>
      <button class="btn-secondary" on:click={clearSearch} disabled={isBusy || !appliedSearchTerm}>Reset</button>
      <button class="btn-secondary" on:click={() => loadUsers()} disabled={isBusy}>
        {loadingList ? 'Aggiornamento...' : 'Aggiorna'}
      </button>
    </div>
    <div class="mt-3 flex items-center justify-between gap-2 text-sm text-[#5a6472]">
      <p>Pagina {currentPage}</p>
      <div class="flex gap-2">
        <button class="btn-secondary" on:click={goToPrevPage} disabled={isBusy || currentPage === 1}>Precedente</button>
        <button class="btn-secondary" on:click={goToNextPage} disabled={isBusy || !hasNextPage}>Successiva</button>
      </div>
    </div>
  </section>

  <section class="grid gap-4 xl:grid-cols-[1fr_2fr]">
    <div class="surface-card p-4">
      <h2 class="text-xl font-bold text-[#1c2430]">Lista</h2>
      {#if loadingList}
        <div class="mt-4 h-28 animate-pulse rounded-xl bg-[#f0f4fb]"></div>
      {:else}
        <div class="mt-4 max-h-[720px] space-y-2 overflow-auto pr-1">
          {#if users.length === 0}
            <p class="text-sm text-[#5a6472]">Nessun utente trovato.</p>
          {:else}
            {#each users as user}
              <button
                class={`w-full rounded-xl border px-3 py-3 text-left transition ${
                  selectedUser?.id === user.id
                    ? 'border-[#0b5fff] bg-[#eef5ff]'
                    : 'border-[#d9dee8] bg-white hover:bg-[#f8fbff]'
                }`}
                on:click={() => selectUser(user)}
              >
                <div class="flex items-start justify-between gap-3">
                  <div>
                    <p class="text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">{user.username}</p>
                    <p class="mt-1 text-sm font-semibold text-[#1c2430]">{user.email}</p>
                    <p class="mt-1 text-xs text-[#5a6472]">Creato: {new Date(user.createdAtUtc).toLocaleString('it-IT')}</p>
                  </div>
                  <div class="flex flex-col items-end gap-1">
                    {#if user.isSuperUser}
                      <span class="rounded-full bg-amber-100 px-2 py-1 text-xs font-semibold text-amber-700">Super user</span>
                    {/if}
                    <span class="rounded-full bg-[#ecf3ff] px-2 py-1 text-xs font-semibold text-[#225ccf]">
                      {user.permissions.length} permessi
                    </span>
                  </div>
                </div>
                <p class="mt-2 text-xs text-[#5a6472]">
                  {user.hasCustomPermissions ? 'Profilo custom' : 'Profilo default'}
                </p>
              </button>
            {/each}
          {/if}
        </div>
      {/if}
    </div>

    <div class="space-y-4">
      <div class="surface-card p-4">
        <div class="flex flex-wrap gap-2">
          <button class={`btn-secondary ${activeTab === 'create' ? '!border-[#0b5fff] !bg-[#eef5ff]' : ''}`} on:click={() => (activeTab = 'create')}>
            Nuovo utente
          </button>
          <button class={`btn-secondary ${activeTab === 'profile' ? '!border-[#0b5fff] !bg-[#eef5ff]' : ''}`} on:click={() => (activeTab = 'profile')}>
            Scheda
          </button>
          <button class={`btn-secondary ${activeTab === 'permissions' ? '!border-[#0b5fff] !bg-[#eef5ff]' : ''}`} on:click={() => (activeTab = 'permissions')}>
            Permessi
          </button>
          <button class={`btn-secondary ${activeTab === 'security' ? '!border-[#0b5fff] !bg-[#eef5ff]' : ''}`} on:click={() => (activeTab = 'security')}>
            Sicurezza
          </button>
        </div>
      </div>

      {#if activeTab === 'create'}
        <div class="grid gap-4 xl:grid-cols-2">
          <div class="surface-card p-5">
            <h2 class="text-xl font-bold text-[#1c2430]">Nuovo utente</h2>
            <div class="mt-3 grid gap-3">
              <div>
                <p class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Username</p>
                <input class="form-input" placeholder="es. operations.lead" bind:value={createForm.username} disabled={!canWrite || isBusy} />
              </div>
              <div>
                <p class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Password iniziale</p>
                <input class="form-input" type="password" placeholder="Minimo 8 caratteri" bind:value={createForm.password} disabled={!canWrite || isBusy} />
              </div>
                <label class="inline-flex items-center gap-2 text-sm text-[#1c2430]">
                  <input type="checkbox" bind:checked={createForm.isSuperUser} disabled={!canWrite || isBusy} />
                  Crea come super user (gestione utenti admin + permessi completi)
                </label>
            </div>

            <div class="mt-4 flex justify-end">
              <button class="btn-primary" on:click={createUser} disabled={!canWrite || isBusy}>
                {creating ? 'Creazione...' : 'Crea utente'}
              </button>
            </div>
          </div>

          <div class="surface-card p-5">
            <div class="flex flex-wrap items-center justify-between gap-2">
              <p class="text-sm font-semibold text-[#1c2430]">Permessi iniziali ({createForm.permissions.length}/{ALL_ADMIN_PERMISSIONS.length})</p>
              <div class="flex flex-wrap gap-2">
                <button class="btn-secondary !px-3 !py-1.5 text-xs" on:click={() => usePermissionPresetForCreate('all')} disabled={!canWrite || isBusy}>Tutti</button>
                <button class="btn-secondary !px-3 !py-1.5 text-xs" on:click={() => usePermissionPresetForCreate('readonly')} disabled={!canWrite || isBusy}>Sola lettura</button>
                <button class="btn-secondary !px-3 !py-1.5 text-xs" on:click={() => usePermissionPresetForCreate('none')} disabled={!canWrite || isBusy}>Nessuno</button>
              </div>
            </div>

            <div class="mt-3">
              <input
                class="form-input"
                placeholder="Filtra permessi (es. ordine, scrittura...)"
                bind:value={createPermissionSearch}
                disabled={!canWrite || isBusy}
              />
            </div>

            <div class="mt-3 max-h-[420px] space-y-2 overflow-auto pr-1">
              {#if createPermissionGroups.length === 0}
                <p class="text-sm text-[#5a6472]">Nessun permesso trovato con il filtro attivo.</p>
              {:else}
                {#each createPermissionGroups as group}
                  <details class="rounded-xl border border-[#d9dee8] bg-[#fcfdff] p-3">
                    <summary class="cursor-pointer text-sm font-semibold text-[#1c2430]">
                      {group.title} ({getSelectedCountByGroup(group.permissions, createForm.permissions)}/{group.permissions.length})
                    </summary>
                    <div class="mt-3 grid gap-2 md:grid-cols-2">
                      {#each group.permissions as permission}
                        <label class="inline-flex items-center gap-2 text-sm text-[#1c2430]">
                          <input
                            type="checkbox"
                            checked={createForm.permissions.includes(permission)}
                            on:change={() => toggleCreatePermission(permission)}
                            disabled={!canWrite || isBusy}
                          />
                          <span>{group.title} - {getActionLabel(permission)}</span>
                        </label>
                      {/each}
                    </div>
                  </details>
                {/each}
              {/if}
            </div>
          </div>
        </div>
      {/if}

      {#if activeTab === 'profile'}
        {#if !selectedUser}
          <div class="surface-card p-5">
            <p class="text-sm text-[#5a6472]">Seleziona un utente dalla lista per visualizzare la scheda.</p>
          </div>
        {:else}
          <div class="grid gap-4 xl:grid-cols-2">
            <div class="surface-card p-5 xl:col-span-2">
              <h2 class="text-xl font-bold text-[#1c2430]">Utente selezionato</h2>
              <div class="mt-3 grid gap-3 md:grid-cols-2">
                <div>
                  <p class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">ID</p>
                  <input class="form-input font-mono text-xs" value={selectedUser.id} disabled />
                </div>
                <div>
                  <p class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Creato il</p>
                  <input class="form-input" value={new Date(selectedUser.createdAtUtc).toLocaleString('it-IT')} disabled />
                </div>
                <div>
                  <p class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Username</p>
                  <input class="form-input" value={selectedUser.username} disabled />
                </div>
                <div>
                  <p class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Email</p>
                  <input class="form-input" value={selectedUser.email} disabled />
                </div>
              </div>

              <div class="mt-3 rounded-xl bg-[#f4f8ff] px-3 py-2 text-sm text-[#2d4a7b]">
                Profilo permessi: {selectedUser.hasCustomPermissions ? 'Custom' : 'Default'}
              </div>
              {#if selectedUser.isSuperUser}
                <div class="mt-2 rounded-xl bg-amber-50 px-3 py-2 text-sm text-amber-700">
                  Questo utente è super user.
                </div>
              {/if}
            </div>
          </div>
        {/if}
      {/if}

      {#if activeTab === 'permissions'}
        {#if !selectedUser}
          <div class="surface-card p-5">
            <p class="text-sm text-[#5a6472]">Seleziona un utente dalla lista per gestire i permessi.</p>
          </div>
        {:else}
          <div class="grid gap-4 xl:grid-cols-[1fr_2fr]">
            <div class="surface-card p-5">
              <h2 class="text-xl font-bold text-[#1c2430]">Azioni rapide</h2>
              <p class="mt-2 text-sm text-[#5a6472]">Utente: {selectedUser.username}</p>
              <p class="mt-1 text-sm text-[#5a6472]">Selezionati: {permissionDraft.length}/{ALL_ADMIN_PERMISSIONS.length}</p>
              <div class="mt-4 grid gap-2">
                <button class="btn-secondary" on:click={() => usePermissionPresetForEdit('all')} disabled={!canWrite || isBusy}>Tutti</button>
                <button class="btn-secondary" on:click={() => usePermissionPresetForEdit('readonly')} disabled={!canWrite || isBusy}>Sola lettura</button>
                <button class="btn-secondary" on:click={() => usePermissionPresetForEdit('none')} disabled={!canWrite || isBusy}>Nessuno</button>
                <button class="btn-secondary" on:click={useDefaultPermissions} disabled={!canWrite || isBusy}>Usa profilo default</button>
              </div>
              <div class="mt-4 flex justify-end">
                <button class="btn-primary" on:click={savePermissions} disabled={!canWrite || isBusy}>
                  {savingPermissions ? 'Salvataggio...' : 'Salva permessi'}
                </button>
              </div>
            </div>

            <div class="surface-card p-5">
              <h2 class="text-xl font-bold text-[#1c2430]">Permessi utente</h2>
              <div class="mt-3">
                <input
                  class="form-input"
                  placeholder="Filtra permessi per area o tipo..."
                  bind:value={editPermissionSearch}
                  disabled={!canWrite || isBusy}
                />
              </div>

              <div class="mt-3 max-h-[460px] space-y-2 overflow-auto pr-1">
                {#if editPermissionGroups.length === 0}
                  <p class="text-sm text-[#5a6472]">Nessun permesso trovato con il filtro attivo.</p>
                {:else}
                  {#each editPermissionGroups as group}
                    <details class="rounded-xl border border-[#d9dee8] bg-[#fcfdff] p-3">
                      <summary class="cursor-pointer text-sm font-semibold text-[#1c2430]">
                        {group.title} ({getSelectedCountByGroup(group.permissions, permissionDraft)}/{group.permissions.length})
                      </summary>
                      <div class="mt-3 grid gap-2 md:grid-cols-2">
                        {#each group.permissions as permission}
                          <label class="inline-flex items-center gap-2 text-sm text-[#1c2430]">
                            <input
                              type="checkbox"
                              checked={permissionDraft.includes(permission)}
                              on:change={() => toggleDraftPermission(permission)}
                              disabled={!canWrite || isBusy}
                            />
                            <span>{group.title} - {getActionLabel(permission)}</span>
                          </label>
                        {/each}
                      </div>
                    </details>
                  {/each}
                {/if}
              </div>
            </div>
          </div>
        {/if}
      {/if}

      {#if activeTab === 'security'}
        {#if !selectedUser}
          <div class="surface-card p-5">
            <p class="text-sm text-[#5a6472]">Seleziona un utente dalla lista per sicurezza e operazioni critiche.</p>
          </div>
        {:else}
          <div class="grid gap-4 xl:grid-cols-2">
            <div class="surface-card p-5">
              <h2 class="text-xl font-bold text-[#1c2430]">Reset password</h2>
              <div class="mt-3 grid gap-3">
                <div>
                  <p class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Nuova password</p>
                  <input class="form-input" type="password" bind:value={resetPasswordForm.newPassword} disabled={!canWrite || isBusy} />
                </div>
                <div>
                  <p class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Conferma password</p>
                  <input class="form-input" type="password" bind:value={resetPasswordForm.confirmPassword} disabled={!canWrite || isBusy} />
                </div>
              </div>
              <div class="mt-4 flex justify-end">
                <button class="btn-primary" on:click={resetPassword} disabled={!canWrite || isBusy}>
                  {savingPassword ? 'Aggiornamento...' : 'Reset password'}
                </button>
              </div>
            </div>

            <div class="surface-card border-rose-200 p-5">
              <h2 class="text-xl font-bold text-rose-700">Zona pericolosa</h2>
              <p class="mt-2 text-sm text-rose-700">
                L'eliminazione revoca immediatamente l'accesso al backoffice per questo utente.
              </p>
              <div class="mt-4 flex justify-end">
                <button class="btn-danger" on:click={removeUser} disabled={!canWrite || isBusy}>
                  {deleting ? 'Eliminazione...' : 'Elimina utente'}
                </button>
              </div>
            </div>
          </div>
        {/if}
      {/if}
    </div>
  </section>
 </div>
