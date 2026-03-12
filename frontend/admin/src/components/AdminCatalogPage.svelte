<script lang="ts">
  import { onMount } from 'svelte';
  import {
    fetchBrands,
    createBrand,
    updateBrand,
    deleteBrand,
    fetchCategories,
    createCategory,
    updateCategory,
    deleteCategory,
    fetchCollections,
    createCollection,
    updateCollection,
    deleteCollection,
    fetchProducts,
    createProduct,
    updateProduct,
    deleteProduct,
    type Brand,
    type Category,
    type Collection,
    type Product
  } from '../lib/api';

  export let canWrite = false;

  type CatalogTab = 'products' | 'brands' | 'categories' | 'collections';

  const pageSize = 20;

  let isLoading = true;
  let isSaving = false;
  let message = '';
  let error = '';

  let activeTab: CatalogTab = 'products';
  let searchInput = '';

  let pageByTab: Record<CatalogTab, number> = {
    products: 1,
    brands: 1,
    categories: 1,
    collections: 1
  };

  let hasNextByTab: Record<CatalogTab, boolean> = {
    products: false,
    brands: false,
    categories: false,
    collections: false
  };

  let searchByTab: Record<CatalogTab, string> = {
    products: '',
    brands: '',
    categories: '',
    collections: ''
  };

  let selectedByTab: Record<CatalogTab, string | null> = {
    products: null,
    brands: null,
    categories: null,
    collections: null
  };

  let brands: Brand[] = [];
  let categories: Category[] = [];
  let collections: Collection[] = [];
  let products: Product[] = [];

  let brandForm = { name: '', slug: '', description: '' };
  let categoryForm = { name: '', slug: '', description: '' };
  let collectionForm = { name: '', slug: '', description: '', isFeatured: false };
  let productForm = {
    sku: '',
    name: '',
    description: '',
    price: 0,
    brandId: '',
    categoryId: '',
    collectionIds: [] as string[],
    isNewArrival: false,
    isBestSeller: false
  };

  const tabLabels: Record<CatalogTab, string> = {
    products: 'Prodotti',
    brands: 'Brand',
    categories: 'Categorie',
    collections: 'Collezioni'
  };

  function getOffset(tab: CatalogTab): number {
    return (pageByTab[tab] - 1) * pageSize;
  }

  function setPage(tab: CatalogTab, page: number) {
    pageByTab = {
      ...pageByTab,
      [tab]: Math.max(1, page)
    };
  }

  function setHasNext(tab: CatalogTab, hasNext: boolean) {
    hasNextByTab = {
      ...hasNextByTab,
      [tab]: hasNext
    };
  }

  function setSearch(tab: CatalogTab, value: string) {
    searchByTab = {
      ...searchByTab,
      [tab]: value
    };
  }

  function setSelected(tab: CatalogTab, value: string | null) {
    selectedByTab = {
      ...selectedByTab,
      [tab]: value
    };
  }

  function resetBrandForm() {
    brandForm = { name: '', slug: '', description: '' };
  }

  function resetCategoryForm() {
    categoryForm = { name: '', slug: '', description: '' };
  }

  function resetCollectionForm() {
    collectionForm = { name: '', slug: '', description: '', isFeatured: false };
  }

  function resetProductForm() {
    productForm = {
      sku: '',
      name: '',
      description: '',
      price: 0,
      brandId: brands[0]?.id ?? '',
      categoryId: categories[0]?.id ?? '',
      collectionIds: [],
      isNewArrival: false,
      isBestSeller: false
    };
  }

  function selectBrand(brandId: string) {
    const selected = brands.find((x) => x.id === brandId);
    if (!selected) return;
    setSelected('brands', selected.id);
    brandForm = {
      name: selected.name,
      slug: selected.slug,
      description: selected.description
    };
  }

  function selectCategory(categoryId: string) {
    const selected = categories.find((x) => x.id === categoryId);
    if (!selected) return;
    setSelected('categories', selected.id);
    categoryForm = {
      name: selected.name,
      slug: selected.slug,
      description: selected.description
    };
  }

  function selectCollection(collectionId: string) {
    const selected = collections.find((x) => x.id === collectionId);
    if (!selected) return;
    setSelected('collections', selected.id);
    collectionForm = {
      name: selected.name,
      slug: selected.slug,
      description: selected.description,
      isFeatured: selected.isFeatured
    };
  }

  function selectProduct(productId: string) {
    const selected = products.find((x) => x.id === productId);
    if (!selected) return;
    setSelected('products', selected.id);
    productForm = {
      sku: selected.sku,
      name: selected.name,
      description: selected.description,
      price: selected.price,
      brandId: selected.brandId,
      categoryId: selected.categoryId,
      collectionIds: [...selected.collectionIds],
      isNewArrival: selected.isNewArrival,
      isBestSeller: selected.isBestSeller
    };
  }

  function selectEntity(tab: CatalogTab, id: string) {
    if (tab === 'brands') {
      selectBrand(id);
      return;
    }

    if (tab === 'categories') {
      selectCategory(id);
      return;
    }

    if (tab === 'collections') {
      selectCollection(id);
      return;
    }

    selectProduct(id);
  }

  function newActiveEntity() {
    setSelected(activeTab, null);

    if (activeTab === 'brands') {
      resetBrandForm();
      return;
    }

    if (activeTab === 'categories') {
      resetCategoryForm();
      return;
    }

    if (activeTab === 'collections') {
      resetCollectionForm();
      return;
    }

    resetProductForm();
  }

  function switchTab(tab: CatalogTab) {
    activeTab = tab;
    searchInput = searchByTab[tab];
  }

  function syncSelection(tab: CatalogTab, ids: string[]) {
    if (ids.length === 0) {
      setSelected(tab, null);
      if (tab === 'brands') resetBrandForm();
      if (tab === 'categories') resetCategoryForm();
      if (tab === 'collections') resetCollectionForm();
      if (tab === 'products') resetProductForm();
      return;
    }

    const currentId = selectedByTab[tab];
    const nextId = currentId && ids.includes(currentId) ? currentId : ids[0];
    selectEntity(tab, nextId);
  }

  async function loadData() {
    isLoading = true;
    error = '';

    try {
      const [brandData, categoryData, collectionData, productData] = await Promise.all([
        fetchBrands({ limit: pageSize, offset: getOffset('brands'), searchTerm: searchByTab.brands }),
        fetchCategories({ limit: pageSize, offset: getOffset('categories'), searchTerm: searchByTab.categories }),
        fetchCollections({ limit: pageSize, offset: getOffset('collections'), searchTerm: searchByTab.collections }),
        fetchProducts({ limit: pageSize, offset: getOffset('products'), searchTerm: searchByTab.products })
      ]);

      brands = brandData;
      categories = categoryData;
      collections = collectionData;
      products = productData;

      setHasNext('brands', brandData.length === pageSize);
      setHasNext('categories', categoryData.length === pageSize);
      setHasNext('collections', collectionData.length === pageSize);
      setHasNext('products', productData.length === pageSize);

      syncSelection('brands', brands.map((x) => x.id));
      syncSelection('categories', categories.map((x) => x.id));
      syncSelection('collections', collections.map((x) => x.id));
      syncSelection('products', products.map((x) => x.id));
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore caricamento catalogo';
    } finally {
      isLoading = false;
    }
  }

  async function applySearch() {
    setSearch(activeTab, searchInput.trim());
    setPage(activeTab, 1);
    await loadData();
  }

  async function clearSearch() {
    searchInput = '';
    setSearch(activeTab, '');
    setPage(activeTab, 1);
    await loadData();
  }

  async function goToPrevPage() {
    const currentPage = pageByTab[activeTab];
    if (isLoading || currentPage <= 1) return;
    setPage(activeTab, currentPage - 1);
    await loadData();
  }

  async function goToNextPage() {
    if (isLoading || !hasNextByTab[activeTab]) return;
    setPage(activeTab, pageByTab[activeTab] + 1);
    await loadData();
  }

  async function saveActiveEntity() {
    if (!canWrite) return;
    if (isSaving) return;

    isSaving = true;
    message = '';
    error = '';

    try {
      if (activeTab === 'brands') {
        const selectedId = selectedByTab.brands;
        if (selectedId) {
          await updateBrand(selectedId, brandForm);
          message = 'Brand aggiornato.';
        } else {
          const created = await createBrand(brandForm);
          setSelected('brands', created.id);
          message = 'Brand creato.';
        }
      } else if (activeTab === 'categories') {
        const selectedId = selectedByTab.categories;
        if (selectedId) {
          await updateCategory(selectedId, categoryForm);
          message = 'Categoria aggiornata.';
        } else {
          const created = await createCategory(categoryForm);
          setSelected('categories', created.id);
          message = 'Categoria creata.';
        }
      } else if (activeTab === 'collections') {
        const selectedId = selectedByTab.collections;
        if (selectedId) {
          await updateCollection(selectedId, collectionForm);
          message = 'Collezione aggiornata.';
        } else {
          const created = await createCollection(collectionForm);
          setSelected('collections', created.id);
          message = 'Collezione creata.';
        }
      } else {
        if (!productForm.brandId || !productForm.categoryId) {
          throw new Error('Seleziona brand e categoria prima di salvare il prodotto.');
        }

        const selectedId = selectedByTab.products;
        if (selectedId) {
          await updateProduct(selectedId, productForm);
          message = 'Prodotto aggiornato.';
        } else {
          const created = await createProduct(productForm);
          setSelected('products', created.id);
          message = 'Prodotto creato.';
        }
      }

      await loadData();
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore salvataggio entita';
    } finally {
      isSaving = false;
    }
  }

  async function removeActiveEntity() {
    if (!canWrite) return;
    const selectedId = selectedByTab[activeTab];
    if (!selectedId || isSaving) return;

    const confirmed = globalThis.confirm(`Eliminare ${tabLabels[activeTab].slice(0, -1).toLowerCase()} selezionato?`);
    if (!confirmed) return;

    isSaving = true;
    message = '';
    error = '';

    try {
      if (activeTab === 'brands') {
        await deleteBrand(selectedId);
        message = 'Brand eliminato.';
      } else if (activeTab === 'categories') {
        await deleteCategory(selectedId);
        message = 'Categoria eliminata.';
      } else if (activeTab === 'collections') {
        await deleteCollection(selectedId);
        message = 'Collezione eliminata.';
      } else {
        await deleteProduct(selectedId);
        message = 'Prodotto eliminato.';
      }

      setSelected(activeTab, null);
      await loadData();
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore eliminazione entita';
    } finally {
      isSaving = false;
    }
  }

  function getActiveItems(): Array<{ id: string; title: string; subtitle: string }> {
    if (activeTab === 'brands') {
      return brands.map((x) => ({ id: x.id, title: x.name, subtitle: x.slug }));
    }

    if (activeTab === 'categories') {
      return categories.map((x) => ({ id: x.id, title: x.name, subtitle: x.slug }));
    }

    if (activeTab === 'collections') {
      return collections.map((x) => ({ id: x.id, title: x.name, subtitle: x.slug }));
    }

    return products.map((x) => ({ id: x.id, title: x.name, subtitle: x.sku }));
  }

  onMount(async () => {
    await loadData();
    searchInput = searchByTab[activeTab];
  });
</script>

<div class="space-y-6">
  <section class="surface-card p-5">
    <h1 class="text-3xl font-extrabold text-[#1c2430]">Catalogo</h1>
    <p class="mt-2 text-sm text-[#5a6472]">Gestione catalogo con lista laterale e pannello modifica dedicato.</p>
    {#if message}
      <p class="mt-3 rounded-lg bg-emerald-50 px-3 py-2 text-sm text-emerald-700">{message}</p>
    {/if}
    {#if error}
      <p class="mt-3 rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700">{error}</p>
    {/if}
  </section>

  <section class="surface-card p-3">
    <div class="flex flex-wrap gap-2">
      <button class={activeTab === 'products' ? 'btn-primary' : 'btn-secondary'} on:click={() => switchTab('products')}>Prodotti</button>
      <button class={activeTab === 'brands' ? 'btn-primary' : 'btn-secondary'} on:click={() => switchTab('brands')}>Brand</button>
      <button class={activeTab === 'categories' ? 'btn-primary' : 'btn-secondary'} on:click={() => switchTab('categories')}>Categorie</button>
      <button class={activeTab === 'collections' ? 'btn-primary' : 'btn-secondary'} on:click={() => switchTab('collections')}>Collezioni</button>
    </div>
  </section>

  <section class="surface-card p-5">
    <div class="flex flex-wrap items-end gap-2">
      <div class="min-w-[300px] flex-1">
        <label class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]" for="catalog-search">
          Cerca {tabLabels[activeTab].toLowerCase()}
        </label>
        <input
          id="catalog-search"
          class="form-input"
          bind:value={searchInput}
          placeholder="nome, slug, sku, descrizione..."
          on:keydown={(event) => {
            if (event.key === 'Enter') {
              applySearch();
            }
          }}
        />
      </div>
      <button class="btn-primary" on:click={applySearch} disabled={isLoading || isSaving}>Cerca</button>
      <button class="btn-secondary" on:click={clearSearch} disabled={isLoading || isSaving || !searchByTab[activeTab]}>Reset</button>
      <button class="btn-secondary" on:click={() => loadData()} disabled={isLoading || isSaving}>
        {isLoading ? 'Aggiornamento...' : 'Aggiorna'}
      </button>
    </div>

    <div class="mt-3 flex items-center justify-between gap-2 text-sm text-[#5a6472]">
      <p>
        {tabLabels[activeTab]} · Pagina {pageByTab[activeTab]}
        {#if searchByTab[activeTab]}
          · filtro: <span class="font-semibold text-[#1c2430]">{searchByTab[activeTab]}</span>
        {/if}
      </p>
      <div class="flex gap-2">
        <button class="btn-secondary" on:click={goToPrevPage} disabled={isLoading || isSaving || pageByTab[activeTab] === 1}>Precedente</button>
        <button class="btn-secondary" on:click={goToNextPage} disabled={isLoading || isSaving || !hasNextByTab[activeTab]}>Successiva</button>
      </div>
    </div>
  </section>

  <section class="grid gap-4 xl:grid-cols-[1fr_2fr]">
    <div class="surface-card p-4">
      <div class="flex items-center justify-between gap-2">
        <h2 class="text-xl font-bold text-[#1c2430]">Lista {tabLabels[activeTab].toLowerCase()}</h2>
        <button class="btn-secondary" on:click={newActiveEntity} disabled={!canWrite || isSaving}>Nuovo</button>
      </div>

      {#if isLoading}
        <div class="mt-4 h-28 animate-pulse rounded-xl bg-[#f0f4fb]"></div>
      {:else}
        <div class="mt-4 max-h-[560px] space-y-2 overflow-auto pr-1">
          {#if getActiveItems().length === 0}
            <p class="text-sm text-[#5a6472]">Nessun elemento trovato.</p>
          {:else}
            {#each getActiveItems() as item}
              <button
                class={`w-full rounded-xl border px-3 py-3 text-left transition ${
                  selectedByTab[activeTab] === item.id
                    ? 'border-[#0b5fff] bg-[#eef5ff]'
                    : 'border-[#d9dee8] bg-white hover:bg-[#f8fbff]'
                }`}
                on:click={() => selectEntity(activeTab, item.id)}
              >
                <p class="text-sm font-semibold text-[#1c2430]">{item.title}</p>
                <p class="mt-1 font-mono text-xs text-[#5a6472]">{item.subtitle}</p>
              </button>
            {/each}
          {/if}
        </div>
      {/if}
    </div>

    <div class="surface-card p-5">
      <div class="flex items-center justify-between gap-2">
        <h2 class="text-xl font-bold text-[#1c2430]">
          {selectedByTab[activeTab] ? `Modifica ${tabLabels[activeTab].slice(0, -1).toLowerCase()}` : `Nuovo ${tabLabels[activeTab].slice(0, -1).toLowerCase()}`}
        </h2>
      </div>

      {#if activeTab === 'products'}
        <div class="mt-3 grid gap-3 md:grid-cols-2">
          <div>
            <p class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">SKU</p>
            <input class="form-input" bind:value={productForm.sku} />
          </div>
          <div>
            <p class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Nome</p>
            <input class="form-input" bind:value={productForm.name} />
          </div>
          <div class="md:col-span-2">
            <p class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Descrizione</p>
            <textarea class="form-input" rows="3" bind:value={productForm.description}></textarea>
          </div>
          <div>
            <p class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Prezzo</p>
            <input class="form-input" type="number" min="0" step="0.01" bind:value={productForm.price} />
          </div>
          <div>
            <p class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Brand</p>
            <select class="form-input" bind:value={productForm.brandId}>
              {#each brands as brand}
                <option value={brand.id}>{brand.name}</option>
              {/each}
            </select>
          </div>
          <div>
            <p class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Categoria</p>
            <select class="form-input" bind:value={productForm.categoryId}>
              {#each categories as category}
                <option value={category.id}>{category.name}</option>
              {/each}
            </select>
          </div>
          <div>
            <p class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Collezioni</p>
            <select class="form-input" multiple bind:value={productForm.collectionIds}>
              {#each collections as collection}
                <option value={collection.id}>{collection.name}</option>
              {/each}
            </select>
          </div>
          <label class="inline-flex items-center gap-2 text-sm text-[#5a6472]">
            <input type="checkbox" bind:checked={productForm.isNewArrival} /> Nuovo arrivo
          </label>
          <label class="inline-flex items-center gap-2 text-sm text-[#5a6472]">
            <input type="checkbox" bind:checked={productForm.isBestSeller} /> Best seller
          </label>
        </div>
      {:else if activeTab === 'brands'}
        <div class="mt-3 grid gap-3">
          <div>
            <p class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Nome</p>
            <input class="form-input" bind:value={brandForm.name} />
          </div>
          <div>
            <p class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Slug</p>
            <input class="form-input" bind:value={brandForm.slug} />
          </div>
          <div>
            <p class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Descrizione</p>
            <textarea class="form-input" rows="3" bind:value={brandForm.description}></textarea>
          </div>
        </div>
      {:else if activeTab === 'categories'}
        <div class="mt-3 grid gap-3">
          <div>
            <p class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Nome</p>
            <input class="form-input" bind:value={categoryForm.name} />
          </div>
          <div>
            <p class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Slug</p>
            <input class="form-input" bind:value={categoryForm.slug} />
          </div>
          <div>
            <p class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Descrizione</p>
            <textarea class="form-input" rows="3" bind:value={categoryForm.description}></textarea>
          </div>
        </div>
      {:else}
        <div class="mt-3 grid gap-3">
          <div>
            <p class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Nome</p>
            <input class="form-input" bind:value={collectionForm.name} />
          </div>
          <div>
            <p class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Slug</p>
            <input class="form-input" bind:value={collectionForm.slug} />
          </div>
          <div>
            <p class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]">Descrizione</p>
            <textarea class="form-input" rows="3" bind:value={collectionForm.description}></textarea>
          </div>
          <label class="inline-flex items-center gap-2 text-sm text-[#5a6472]">
            <input type="checkbox" bind:checked={collectionForm.isFeatured} /> In evidenza
          </label>
        </div>
      {/if}

      {#if !canWrite}
        <p class="mt-4 rounded-lg bg-amber-50 px-3 py-2 text-sm text-amber-700">
          Permesso mancante: `catalog:write`. Modifiche disabilitate in sola lettura.
        </p>
      {/if}

      <div class="mt-4 flex flex-wrap justify-end gap-2">
        <button class="btn-secondary" on:click={newActiveEntity} disabled={!canWrite || isSaving}>Nuovo</button>
        <button class="btn-primary" on:click={saveActiveEntity} disabled={!canWrite || isSaving}>
          {isSaving ? 'Salvataggio...' : 'Salva'}
        </button>
        <button class="btn-danger" on:click={removeActiveEntity} disabled={!canWrite || isSaving || !selectedByTab[activeTab]}>
          Elimina
        </button>
      </div>
    </div>
  </section>
</div>
