<script lang="ts">
  import { cartCount } from '../stores/cart';
</script>

<div class="border-b border-[#e1e3e5] bg-[#202223] px-4 py-2 text-center text-xs font-medium text-white md:px-8">
  Spedizione gratuita sopra 120€ · Reso entro 30 giorni
</div>

<header class="sticky top-0 z-40 border-b border-[#e1e3e5] bg-white/95 backdrop-blur">
  <div class="mx-auto flex w-full max-w-[1280px] flex-wrap items-center gap-3 px-4 py-3 md:flex-nowrap md:px-8">
    <a href="/" class="flex items-center gap-3" aria-label="CQRS Commerce home">
      <span class="flex h-9 w-9 items-center justify-center rounded-lg bg-[#008060] text-sm font-bold text-white">CQ</span>
      <span class="font-title text-base font-bold tracking-tight text-[#202223] md:text-lg">CQRS Commerce</span>
    </a>

    <nav class="hidden items-center gap-6 text-sm font-medium text-[#4a4f55] lg:flex">
      <a href="/" class="transition hover:text-[#202223]">Home</a>
      <a href="/search" class="transition hover:text-[#202223]">Shop</a>
    </nav>

    <form action="/search" method="get" class="order-3 w-full md:order-none md:ml-auto md:max-w-md lg:max-w-lg">
      <label class="sr-only" for="global-search">Cerca prodotti</label>
      <div class="relative">
        <input
          id="global-search"
          name="q"
          type="search"
          placeholder="Cerca prodotti, brand, SKU..."
          class="form-input h-10 w-full rounded-xl bg-[#f6f6f7] pr-10"
        />
        <button type="submit" class="absolute right-2 top-1/2 -translate-y-1/2 rounded-lg px-2 py-1 text-xs font-semibold text-[#008060] hover:bg-[#ebf7f3]">
          Cerca
        </button>
      </div>
    </form>

    <a href="/cart" class="relative inline-flex items-center gap-2 rounded-xl border border-[#e1e3e5] px-4 py-2 text-sm font-semibold text-[#202223] transition hover:bg-[#f6f6f7]" aria-label="Apri carrello">
      <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
        <path stroke-linecap="round" stroke-linejoin="round" d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13l-1.5 6h13M9 21a1 1 0 100-2 1 1 0 000 2zm10 0a1 1 0 100-2 1 1 0 000 2z" />
      </svg>
      <span>Carrello</span>
      {#if $cartCount > 0}
        <span class="absolute -right-2 -top-2 flex h-5 min-w-5 items-center justify-center rounded-full bg-[#008060] px-1 text-[10px] font-bold text-white">
          {$cartCount > 99 ? '99+' : $cartCount}
        </span>
      {/if}
    </a>
  </div>
</header>
