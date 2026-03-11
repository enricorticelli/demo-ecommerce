<script lang="ts">
  import { onMount } from 'svelte';
  import {
    createAdminUser,
    deleteAdminUser,
    fetchAdminUsers,
    resetAdminUserPassword,
    type AdminAccountUser
  } from '../lib/api';

  const pageSize = 20;

  let users: AdminAccountUser[] = [];
  let loading = true;
  let saving = false;
  let currentPage = 1;
  let hasNextPage = false;
  let searchTerm = '';
  let appliedSearchTerm = '';

  let createForm = { username: '', password: '' };
  let resetPasswordByUserId: Record<string, string> = {};

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

  async function loadUsers(page = currentPage) {
    loading = true;
    error = '';
    currentPage = Math.max(1, page);

    try {
      const offset = (currentPage - 1) * pageSize;
      users = await fetchAdminUsers(pageSize, offset, appliedSearchTerm);
      hasNextPage = users.length === pageSize;
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore caricamento utenze admin';
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
    if (saving) return;
    if (!createForm.username.trim()) {
      error = 'Username admin obbligatorio.';
      return;
    }
    if (createForm.password.length < 8) {
      error = 'Password admin minima 8 caratteri.';
      return;
    }

    saving = true;
    message = '';
    error = '';

    try {
      await createAdminUser({
        username: createForm.username.trim(),
        password: createForm.password
      });

      createForm = { username: '', password: '' };
      message = 'Utenza admin creata.';
      await loadUsers(1);
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore creazione utenza admin';
    } finally {
      saving = false;
    }
  }

  async function resetPassword(user: AdminAccountUser) {
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
    if (saving) return;

    const confirmed = globalThis.confirm(`Eliminare l'utenza admin "${user.username}"?`);
    if (!confirmed) return;

    saving = true;
    message = '';
    error = '';

    try {
      await deleteAdminUser(user.id);
      message = `Utenza ${user.username} eliminata.`;
      await loadUsers(1);
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore eliminazione utenza admin';
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
    <h1 class="text-3xl font-extrabold text-[#1c2430]">Utenze Admin</h1>
    <p class="mt-2 text-sm text-[#5a6472]">Gestione utenti amministrativi: creazione, reset password ed eliminazione protetta.</p>
    {#if message}
      <p class="mt-3 rounded-lg bg-emerald-50 px-3 py-2 text-sm text-emerald-700">{message}</p>
    {/if}
    {#if error}
      <p class="mt-3 rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700">{error}</p>
    {/if}
  </section>

  <section class="surface-card p-5">
    <h2 class="text-xl font-bold text-[#1c2430]">Crea utenza admin</h2>
    <div class="mt-3 grid gap-3 md:grid-cols-3">
      <input class="form-input" placeholder="Username admin" bind:value={createForm.username} />
      <input class="form-input" type="password" placeholder="Password" bind:value={createForm.password} />
      <button class="btn-primary" on:click={createUser} disabled={saving}>
        {saving ? 'Creazione...' : 'Crea admin'}
      </button>
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
    <h2 class="text-xl font-bold text-[#1c2430]">Lista utenze</h2>
    {#if loading}
      <div class="mt-4 h-24 animate-pulse rounded-xl bg-[#f0f4fb]"></div>
    {:else}
      <div class="mt-4 overflow-x-auto">
        <table class="w-full min-w-[980px] text-left text-sm">
          <thead>
            <tr class="border-b border-[#d9dee8] text-[#5a6472]">
              <th class="px-2 py-2">Username</th>
              <th class="px-2 py-2">Email</th>
              <th class="px-2 py-2">Creato il</th>
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
                  <input
                    class="form-input max-w-[220px]"
                    type="password"
                    placeholder="Nuova password"
                    value={getResetPassword(user.id)}
                    on:input={(event) => setResetPassword(user.id, (event.currentTarget as HTMLInputElement).value)}
                    disabled={saving}
                  />
                </td>
                <td class="px-2 py-2 text-right">
                  <div class="inline-flex gap-2">
                    <button class="btn-secondary !px-3 !py-1.5 text-xs" on:click={() => resetPassword(user)} disabled={saving}>
                      Reset password
                    </button>
                    <button class="btn-secondary !px-3 !py-1.5 text-xs" on:click={() => removeUser(user)} disabled={saving}>
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
