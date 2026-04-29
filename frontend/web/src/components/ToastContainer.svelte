<script lang="ts">
  import { toasts, removeToast } from '../stores/ui';

  const ICONS: Record<string, string> = {
    success: '✓',
    error: '✕',
    info: 'i',
  };

  const BG: Record<string, string> = {
    success: 'bg-emerald-600',
    error: 'bg-rose-600',
    info: 'bg-sky-600',
  };
</script>

<div class="pointer-events-none fixed bottom-6 right-6 z-50 flex flex-col gap-2" aria-live="polite">
  {#each $toasts as toast (toast.id)}
    <div
      class="pointer-events-auto flex items-center gap-3 rounded-xl px-4 py-3 text-sm font-medium text-white shadow-lg {BG[toast.type]} pop-in"
    >
      <span
        class="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-white/20 text-xs font-bold"
      >
        {ICONS[toast.type]}
      </span>
      <span>{toast.message}</span>
      <button
        class="ml-2 opacity-60 hover:opacity-100 transition"
        aria-label="Chiudi"
        on:click={() => removeToast(toast.id)}
      >
        ✕
      </button>
    </div>
  {/each}
</div>
