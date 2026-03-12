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

  let users: AdminAccountUser[] = [];
  let loading = true;
  let saving = false;
  let currentPage = 1;
  let hasNextPage = false;
  let searchTerm = '';
  let appliedSearchTerm = '';

  let createForm = { username: '', password: '', permissions: [...ALL_ADMIN_PERMISSIONS] as AdminPermission[] };
  let resetPasswordByUserId: Record<string, string> = {};
  let permissionDraftByUserId: Record<string, AdminPermission[]> = {};

  let message = '';
  let error = '';

  function getResetPassword(userId: string): string {
    return resetPasswordByUserId[userId] ?? '';
  }

  function setResetPassword(userId: string, value: string) {
    resetPasswordByUserId = {
      ...resetPasswordByUserId,
      [userId]: value
    };
  }

  function handleResetPasswordInput(userId: string, event: Event) {
    const target = event.currentTarget as HTMLInputElement | null;
    setResetPassword(userId, target?.value ?? '');
  }

  function getPermissionDraft(user: AdminAccountUser): AdminPermission[] {
    return permissionDraftByUserId[user.id] ?? (user.permissions as AdminPermission[]);
  }

  function setPermissionDraft(userId: string, permissions: AdminPermission[]) {
    permissionDraftByUserId = {
      ...permissionDraftByUserId,
      [userId]: permissions
    };
  }

  function toggleCreatePermission(permission: AdminPermission) {
    if (createForm.permissions.includes(permission)) {
      createForm.permissions = createForm.permissions.filter((x) => x !== permission);
      return;
    }

    createForm.permissions = [...createForm.permissions, permission];
  }

  function toggleUserPermission(userId: string, permission: AdminPermission) {
    const current = permissionDraftByUserId[userId] ?? [];
    if (current.includes(permission)) {
      setPermissionDraft(userId, current.filter((x) => x !== permission));
      return;
    }

    setPermissionDraft(userId, [...current, permission]);
  }

  function resetCreatePermissionsToDefault() {
    createForm.permissions = [...ALL_ADMIN_PERMISSIONS];
  }

  function toCreatePermissionsPayload(permissions: AdminPermission[]): AdminPermission[] | null {
    if (permissions.length !== ALL_ADMIN_PERMISSIONS.length) {
      return permissions;
    }

    const requested = new Set(permissions);
    const isDefault = ALL_ADMIN_PERMISSIONS.every((permission) => requested.has(permission));
    return isDefault ? null : permissions;
  }

  async function loadUsers(page = currentPage) {
    loading = true;
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

      permissionDraftByUserId = Object.fromEntries(
        users.map((user) => [user.id, [...(user.permissions as AdminPermission[])]])
      );
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore caricamento utenti';
    } finally {
      loading = false;
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
    if (currentPage <= 1 || loading || saving) return;
    await loadUsers(currentPage - 1);
  }

  async function goToNextPage() {
    if (!hasNextPage || loading || saving) return;
    await loadUsers(currentPage + 1);
  }

  async function createUser() {
    if (!canWrite) return;
    if (saving) return;
    if (!createForm.username.trim()) {
      error = 'Username obbligatorio.';
      return;
    }
    if (createForm.password.length < 8) {
      error = 'Password minima 8 caratteri.';
      return;
    }

    saving = true;
    message = '';
    error = '';

    try {
      await createAdminUser({
        username: createForm.username.trim(),
        password: createForm.password,
        permissions: toCreatePermissionsPayload(createForm.permissions)
      });

      createForm = { username: '', password: '', permissions: [...ALL_ADMIN_PERMISSIONS] };
      message = 'Utente admin creato.';
      await loadUsers(1);
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore creazione utente admin';
    } finally {
      saving = false;
    }
  }

  async function resetPassword(user: AdminAccountUser) {
    if (!canWrite) return;
    if (saving) return;

    const newPassword = getResetPassword(user.id);
    if (newPassword.length < 8) {
      error = 'Nuova password minima 8 caratteri.';
      return;
    }

    saving = true;
    message = '';
    error = '';

    try {
      await resetAdminUserPassword(user.id, newPassword);
      setResetPassword(user.id, '');
      message = `Password aggiornata per ${user.username}.`;
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore reset password admin';
    } finally {
      saving = false;
    }
  }

  async function removeUser(user: AdminAccountUser) {
    if (!canWrite) return;
    if (saving) return;

    const confirmed = globalThis.confirm(`Eliminare l'utente admin "${user.username}"?`);
    if (!confirmed) return;

    saving = true;
    message = '';
    error = '';

    try {
      await deleteAdminUser(user.id);
      message = `Utente ${user.username} eliminato.`;
      await loadUsers(1);
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore eliminazione utente admin';
    } finally {
      saving = false;
    }
  }

  async function savePermissions(user: AdminAccountUser) {
    if (!canWrite) return;
    if (saving) return;

    saving = true;
    message = '';
    error = '';

    try {
      const permissions = getPermissionDraft(user);
      await updateAdminUserPermissions(user.id, permissions);
      message = `Permessi aggiornati per ${user.username}. Sessioni utente revocate.`;
      await loadUsers(currentPage);
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore aggiornamento permessi admin';
    } finally {
      saving = false;
    }
  }

  async function useDefaultPermissions(user: AdminAccountUser) {
    if (!canWrite) return;
    if (saving) return;

    saving = true;
    message = '';
    error = '';

    try {
      await updateAdminUserPermissions(user.id, null);
      message = `Permessi default ripristinati per ${user.username}. Sessioni utente revocate.`;
      await loadUsers(currentPage);
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore ripristino permessi default';
    } finally {
      saving = false;
    }
  }

  onMount(() => {
    loadUsers();
  });
</script>

<div class="space-y-6">
  <section class="surface-card p-5">
    <h1 class="text-3xl font-extrabold text-[#1c2430]">Utenti</h1>
    <p class="mt-2 text-sm text-[#5a6472]">Gestione utenti amministrativi: creazione, reset password ed eliminazione protetta.</p>
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
    <h2 class="text-xl font-bold text-[#1c2430]">Crea utente</h2>
    <div class="mt-3 grid gap-3 md:grid-cols-3">
      <input class="form-input" placeholder="Username" bind:value={createForm.username} />
      <input class="form-input" type="password" placeholder="Password" bind:value={createForm.password} />
      <button class="btn-primary" on:click={createUser} disabled={!canWrite || saving}>
        {saving ? 'Creazione...' : 'Crea utente'}
      </button>
    </div>
    <div class="mt-4 rounded-xl border border-[#d9dee8] bg-[#fcfdff] p-4">
      <div class="mb-3 flex items-center justify-between gap-2">
        <p class="text-sm font-semibold text-[#1c2430]">Permessi iniziali</p>
        <button class="btn-secondary !px-3 !py-1.5 text-xs" on:click={resetCreatePermissionsToDefault} disabled={!canWrite || saving}>
          Usa default
        </button>
      </div>
      <div class="grid gap-2 md:grid-cols-2">
        {#each ALL_ADMIN_PERMISSIONS as permission}
          <label class="inline-flex items-center gap-2 text-sm text-[#1c2430]">
            <input
              type="checkbox"
              checked={createForm.permissions.includes(permission)}
              on:change={() => toggleCreatePermission(permission)}
              disabled={!canWrite || saving}
            />
            {ADMIN_PERMISSION_LABEL[permission]}
          </label>
        {/each}
      </div>
    </div>
  </section>

  <section class="surface-card p-5">
    <div class="flex flex-wrap items-end gap-2">
      <div class="min-w-[300px] flex-1">
        <label class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]" for="admin-users-search">
          Cerca admin
        </label>
        <input
          id="admin-users-search"
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
      <button class="btn-primary" on:click={applySearch} disabled={loading || saving}>Cerca</button>
      <button class="btn-secondary" on:click={clearSearch} disabled={loading || saving || !appliedSearchTerm}>Reset</button>
      <button class="btn-secondary" on:click={() => loadUsers()} disabled={loading || saving}>
        {loading ? 'Aggiornamento...' : 'Aggiorna'}
      </button>
    </div>
    <div class="mt-3 flex items-center justify-between gap-2 text-sm text-[#5a6472]">
      <p>Pagina {currentPage}</p>
      <div class="flex gap-2">
        <button class="btn-secondary" on:click={goToPrevPage} disabled={loading || saving || currentPage === 1}>Precedente</button>
        <button class="btn-secondary" on:click={goToNextPage} disabled={loading || saving || !hasNextPage}>Successiva</button>
      </div>
    </div>
  </section>

  <section class="surface-card p-5">
    <h2 class="text-xl font-bold text-[#1c2430]">Lista utenti</h2>
    {#if loading}
      <div class="mt-4 h-24 animate-pulse rounded-xl bg-[#f0f4fb]"></div>
    {:else}
      <div class="mt-4 overflow-x-auto">
        <table class="w-full min-w-[1240px] text-left text-sm">
          <thead>
            <tr class="border-b border-[#d9dee8] text-[#5a6472]">
              <th class="px-2 py-2">Username</th>
              <th class="px-2 py-2">Email</th>
              <th class="px-2 py-2">Creato il</th>
              <th class="px-2 py-2">Permessi</th>
              <th class="px-2 py-2">Nuova password</th>
              <th class="px-2 py-2 text-right">Azioni</th>
            </tr>
          </thead>
          <tbody>
            {#each users as user}
              <tr class="border-b border-[#edf1f7]">
                <td class="px-2 py-2 font-semibold text-[#1c2430]">{user.username}</td>
                <td class="px-2 py-2">{user.email}</td>
                <td class="px-2 py-2">{new Date(user.createdAtUtc).toLocaleString('it-IT')}</td>
                <td class="px-2 py-2">
                  <div class="grid gap-1">
                    {#each ALL_ADMIN_PERMISSIONS as permission}
                      <label class="inline-flex items-center gap-2 text-xs text-[#1c2430]">
                        <input
                          type="checkbox"
                          checked={getPermissionDraft(user).includes(permission)}
                          on:change={() => toggleUserPermission(user.id, permission)}
                          disabled={!canWrite || saving}
                        />
                        {ADMIN_PERMISSION_LABEL[permission]}
                      </label>
                    {/each}
                    {#if !user.hasCustomPermissions}
                      <p class="text-xs text-[#5a6472]">Profilo in modalita default.</p>
                    {/if}
                  </div>
                </td>
                <td class="px-2 py-2">
                  <input
                    class="form-input max-w-[220px]"
                    type="password"
                    placeholder="Nuova password"
                    value={getResetPassword(user.id)}
                    on:input={(event) => handleResetPasswordInput(user.id, event)}
                    disabled={!canWrite || saving}
                  />
                </td>
                <td class="px-2 py-2 text-right">
                  <div class="inline-flex gap-2">
                    <button class="btn-secondary !px-3 !py-1.5 text-xs" on:click={() => savePermissions(user)} disabled={!canWrite || saving}>
                      Salva permessi
                    </button>
                    <button class="btn-secondary !px-3 !py-1.5 text-xs" on:click={() => useDefaultPermissions(user)} disabled={!canWrite || saving}>
                      Default
                    </button>
                    <button class="btn-secondary !px-3 !py-1.5 text-xs" on:click={() => resetPassword(user)} disabled={!canWrite || saving}>
                      Reset password
                    </button>
                    <button class="btn-secondary !px-3 !py-1.5 text-xs" on:click={() => removeUser(user)} disabled={!canWrite || saving}>
                      Elimina
                    </button>
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
