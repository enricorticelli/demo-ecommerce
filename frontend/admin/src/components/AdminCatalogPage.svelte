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

  type CatalogTab = 'products' | 'brands' | 'categories' | 'collections';

  let isLoading = true;
  let isSaving = false;
  let message = '';
  let error = '';
  let activeTab: CatalogTab = 'products';
  let isModalOpen = false;
  let isEditMode = false;
  let editingId: string | null = null;
  const pageSize = 20;

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
  let searchInput = '';

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

  async function loadData() {
    isLoading = true;
    message = '';
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

      if (!productForm.brandId && brands.length > 0) productForm.brandId = brands[0].id;
      if (!productForm.categoryId && categories.length > 0) productForm.categoryId = categories[0].id;
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore caricamento catalogo';
    } finally {
      isLoading = false;
    }
  }

  function switchTab(tab: CatalogTab) {
    activeTab = tab;
    searchInput = searchByTab[tab];
    closeModal();
  }

  async function applySearch() {
    const normalizedSearch = searchInput.trim();
    setSearch(activeTab, normalizedSearch);
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
    const page = pageByTab[activeTab];
    if (page <= 1 || isLoading) return;
    setPage(activeTab, page - 1);
    await loadData();
  }

  async function goToNextPage() {
    if (!hasNextByTab[activeTab] || isLoading) return;
    setPage(activeTab, pageByTab[activeTab] + 1);
    await loadData();
  }

  function resetForms() {
    brandForm = { name: '', slug: '', description: '' };
    categoryForm = { name: '', slug: '', description: '' };
    collectionForm = { name: '', slug: '', description: '', isFeatured: false };
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

  function openCreateModal() {
    resetForms();
    isEditMode = false;
    editingId = null;
    isModalOpen = true;
  }

  function openEditModal(entity: Product | Brand | Category | Collection) {
    resetForms();
    isEditMode = true;
    editingId = (entity as { id: string }).id;

    if (activeTab === 'products') {
      const product = entity as Product;
      productForm = {
        sku: product.sku,
        name: product.name,
        description: product.description,
        price: product.price,
        brandId: product.brandId,
        categoryId: product.categoryId,
        collectionIds: [...product.collectionIds],
        isNewArrival: product.isNewArrival,
        isBestSeller: product.isBestSeller
      };
    } else if (activeTab === 'brands') {
      const brand = entity as Brand;
      brandForm = { name: brand.name, slug: brand.slug, description: brand.description };
    } else if (activeTab === 'categories') {
      const category = entity as Category;
      categoryForm = { name: category.name, slug: category.slug, description: category.description };
    } else {
      const collection = entity as Collection;
      collectionForm = {
        name: collection.name,
        slug: collection.slug,
        description: collection.description,
        isFeatured: collection.isFeatured
      };
    }

    isModalOpen = true;
  }

  function closeModal() {
    isModalOpen = false;
    isEditMode = false;
    editingId = null;
  }

  async function saveActiveEntity() {
    isSaving = true;
    message = '';
    error = '';

    try {
      if (activeTab === 'products') {
        if (isEditMode && editingId) {
          await updateProduct(editingId, productForm);
          message = 'Prodotto aggiornato';
        } else {
          await createProduct(productForm);
          message = 'Prodotto creato';
        }
      } else if (activeTab === 'brands') {
        if (isEditMode && editingId) {
          await updateBrand(editingId, brandForm);
          message = 'Brand aggiornato';
        } else {
          await createBrand(brandForm);
          message = 'Brand creato';
        }
      } else if (activeTab === 'categories') {
        if (isEditMode && editingId) {
          await updateCategory(editingId, categoryForm);
          message = 'Categoria aggiornata';
        } else {
          await createCategory(categoryForm);
          message = 'Categoria creata';
        }
      } else {
        if (isEditMode && editingId) {
          await updateCollection(editingId, collectionForm);
          message = 'Collezione aggiornata';
        } else {
          await createCollection(collectionForm);
          message = 'Collezione creata';
        }
      }

      closeModal();
      await loadData();
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore salvataggio entita';
    } finally {
      isSaving = false;
    }
  }

  async function removeActiveEntity(id: string) {
    message = '';
    error = '';

    try {
      if (activeTab === 'products') {
        await deleteProduct(id);
        message = 'Prodotto eliminato';
      } else if (activeTab === 'brands') {
        await deleteBrand(id);
        message = 'Brand eliminato';
      } else if (activeTab === 'categories') {
        await deleteCategory(id);
        message = 'Categoria eliminata';
      } else {
        await deleteCollection(id);
        message = 'Collezione eliminata';
      }

      await loadData();
    } catch (err) {
      error = err instanceof Error ? err.message : 'Errore eliminazione entita';
    }
  }

  onMount(async () => {
    await loadData();
    searchInput = searchByTab[activeTab];
    resetForms();
  });
</script>

<div class="space-y-6">
  <section class="surface-card p-5">
    <div class="flex flex-wrap items-center justify-between gap-3">
      <div>
        <h1 class="text-3xl font-extrabold text-[#1c2430]">Catalogo</h1>
        <p class="mt-2 text-sm text-[#5a6472]">Gestione entita catalogo con tab separati e modale di inserimento.</p>
      </div>
      <button class="btn-primary" on:click={openCreateModal}>Aggiungi</button>
    </div>
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

  {#if isLoading}
    <div class="surface-card h-56 animate-pulse"></div>
  {:else}
    <section class="surface-card p-5">
      <div class="mb-3 flex flex-wrap items-end gap-2">
        <div class="min-w-[320px] flex-1">
          <label class="mb-1 block text-xs font-semibold uppercase tracking-[0.12em] text-[#5a6472]" for="catalog-search">
            Ricerca {tabLabels[activeTab].toLowerCase()}
          </label>
          <input
            id="catalog-search"
            class="form-input"
            bind:value={searchInput}
            placeholder="Nome, slug, SKU, descrizione..."
            on:keydown={(event) => {
              if (event.key === 'Enter') {
                applySearch();
              }
            }}
          />
        </div>
        <button class="btn-primary" on:click={applySearch} disabled={isLoading}>Cerca</button>
        <button class="btn-secondary" on:click={clearSearch} disabled={isLoading || !searchByTab[activeTab]}>Reset</button>
      </div>

      <div class="mb-3 flex items-center justify-between gap-2 text-sm text-[#5a6472]">
        <p>
          {tabLabels[activeTab]} · Pagina {pageByTab[activeTab]}
          {#if searchByTab[activeTab]}
            · filtro: <span class="font-semibold text-[#1c2430]">{searchByTab[activeTab]}</span>
          {/if}
        </p>
        <div class="flex gap-2">
          <button class="btn-secondary" on:click={goToPrevPage} disabled={isLoading || pageByTab[activeTab] === 1}>Precedente</button>
          <button class="btn-secondary" on:click={goToNextPage} disabled={isLoading || !hasNextByTab[activeTab]}>Successiva</button>
        </div>
      </div>
      <div class="overflow-x-auto">
        {#if activeTab === 'products'}
          <table class="w-full min-w-[920px] text-left text-sm">
            <thead>
              <tr class="border-b border-[#d9dee8] text-[#5a6472]">
                <th class="px-2 py-2">SKU</th>
                <th class="px-2 py-2">Nome</th>
                <th class="px-2 py-2">Prezzo</th>
                <th class="px-2 py-2 text-right">Azioni</th>
              </tr>
            </thead>
            <tbody>
              {#each products as product}
                <tr class="border-b border-[#edf1f7]">
                  <td class="px-2 py-2 font-mono text-xs">{product.sku}</td>
                  <td class="px-2 py-2">{product.name}</td>
                  <td class="px-2 py-2">EUR {product.price.toFixed(2)}</td>
                  <td class="px-2 py-2">
                    <div class="flex justify-end gap-2">
                      <button class="btn-secondary !px-3" title="Modifica" aria-label="Modifica" on:click={() => openEditModal(product)}>
                        <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                          <path d="M12 20h9" />
                          <path d="M16.5 3.5a2.12 2.12 0 0 1 3 3L7 19l-4 1 1-4 12.5-12.5z" />
                        </svg>
                      </button>
                      <button class="btn-danger !px-3" title="Elimina" aria-label="Elimina" on:click={() => removeActiveEntity(product.id)}>
                        <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                          <path d="M3 6h18" />
                          <path d="M8 6V4h8v2" />
                          <path d="M19 6l-1 14H6L5 6" />
                          <path d="M10 11v6M14 11v6" />
                        </svg>
                      </button>
                    </div>
                  </td>
                </tr>
              {:else}
                <tr>
                  <td class="empty-row" colspan="4">
                    <div class="empty-box">
                      <p class="empty-title">Nessun prodotto presente</p>
                      <p class="empty-description">Crea il primo prodotto per popolare il catalogo.</p>
                    </div>
                  </td>
                </tr>
              {/each}
            </tbody>
          </table>
        {:else if activeTab === 'brands'}
          <table class="w-full min-w-[820px] text-left text-sm">
            <thead>
              <tr class="border-b border-[#d9dee8] text-[#5a6472]">
                <th class="px-2 py-2">Nome</th>
                <th class="px-2 py-2">Slug</th>
                <th class="px-2 py-2">Descrizione</th>
                <th class="px-2 py-2 text-right">Azioni</th>
              </tr>
            </thead>
            <tbody>
              {#each brands as brand}
                <tr class="border-b border-[#edf1f7]">
                  <td class="px-2 py-2">{brand.name}</td>
                  <td class="px-2 py-2 font-mono text-xs">{brand.slug}</td>
                  <td class="px-2 py-2">{brand.description}</td>
                  <td class="px-2 py-2">
                    <div class="flex justify-end gap-2">
                      <button class="btn-secondary !px-3" title="Modifica" aria-label="Modifica" on:click={() => openEditModal(brand)}>
                        <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                          <path d="M12 20h9" />
                          <path d="M16.5 3.5a2.12 2.12 0 0 1 3 3L7 19l-4 1 1-4 12.5-12.5z" />
                        </svg>
                      </button>
                      <button class="btn-danger !px-3" title="Elimina" aria-label="Elimina" on:click={() => removeActiveEntity(brand.id)}>
                        <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                          <path d="M3 6h18" />
                          <path d="M8 6V4h8v2" />
                          <path d="M19 6l-1 14H6L5 6" />
                          <path d="M10 11v6M14 11v6" />
                        </svg>
                      </button>
                    </div>
                  </td>
                </tr>
              {:else}
                <tr>
                  <td class="empty-row" colspan="4">
                    <div class="empty-box">
                      <p class="empty-title">Nessun brand presente</p>
                      <p class="empty-description">Aggiungi un brand per organizzare meglio i prodotti.</p>
                    </div>
                  </td>
                </tr>
              {/each}
            </tbody>
          </table>
        {:else if activeTab === 'categories'}
          <table class="w-full min-w-[820px] text-left text-sm">
            <thead>
              <tr class="border-b border-[#d9dee8] text-[#5a6472]">
                <th class="px-2 py-2">Nome</th>
                <th class="px-2 py-2">Slug</th>
                <th class="px-2 py-2">Descrizione</th>
                <th class="px-2 py-2 text-right">Azioni</th>
              </tr>
            </thead>
            <tbody>
              {#each categories as category}
                <tr class="border-b border-[#edf1f7]">
                  <td class="px-2 py-2">{category.name}</td>
                  <td class="px-2 py-2 font-mono text-xs">{category.slug}</td>
                  <td class="px-2 py-2">{category.description}</td>
                  <td class="px-2 py-2">
                    <div class="flex justify-end gap-2">
                      <button class="btn-secondary !px-3" title="Modifica" aria-label="Modifica" on:click={() => openEditModal(category)}>
                        <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                          <path d="M12 20h9" />
                          <path d="M16.5 3.5a2.12 2.12 0 0 1 3 3L7 19l-4 1 1-4 12.5-12.5z" />
                        </svg>
                      </button>
                      <button class="btn-danger !px-3" title="Elimina" aria-label="Elimina" on:click={() => removeActiveEntity(category.id)}>
                        <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                          <path d="M3 6h18" />
                          <path d="M8 6V4h8v2" />
                          <path d="M19 6l-1 14H6L5 6" />
                          <path d="M10 11v6M14 11v6" />
                        </svg>
                      </button>
                    </div>
                  </td>
                </tr>
              {:else}
                <tr>
                  <td class="empty-row" colspan="4">
                    <div class="empty-box">
                      <p class="empty-title">Nessuna categoria presente</p>
                      <p class="empty-description">Crea una categoria per classificare i prodotti.</p>
                    </div>
                  </td>
                </tr>
              {/each}
            </tbody>
          </table>
        {:else}
          <table class="w-full min-w-[860px] text-left text-sm">
            <thead>
              <tr class="border-b border-[#d9dee8] text-[#5a6472]">
                <th class="px-2 py-2">Nome</th>
                <th class="px-2 py-2">Slug</th>
                <th class="px-2 py-2">In evidenza</th>
                <th class="px-2 py-2 text-right">Azioni</th>
              </tr>
            </thead>
            <tbody>
              {#each collections as collection}
                <tr class="border-b border-[#edf1f7]">
                  <td class="px-2 py-2">{collection.name}</td>
                  <td class="px-2 py-2 font-mono text-xs">{collection.slug}</td>
                  <td class="px-2 py-2">{collection.isFeatured ? 'Si' : 'No'}</td>
                  <td class="px-2 py-2">
                    <div class="flex justify-end gap-2">
                      <button class="btn-secondary !px-3" title="Modifica" aria-label="Modifica" on:click={() => openEditModal(collection)}>
                        <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                          <path d="M12 20h9" />
                          <path d="M16.5 3.5a2.12 2.12 0 0 1 3 3L7 19l-4 1 1-4 12.5-12.5z" />
                        </svg>
                      </button>
                      <button class="btn-danger !px-3" title="Elimina" aria-label="Elimina" on:click={() => removeActiveEntity(collection.id)}>
                        <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                          <path d="M3 6h18" />
                          <path d="M8 6V4h8v2" />
                          <path d="M19 6l-1 14H6L5 6" />
                          <path d="M10 11v6M14 11v6" />
                        </svg>
                      </button>
                    </div>
                  </td>
                </tr>
              {:else}
                <tr>
                  <td class="empty-row" colspan="4">
                    <div class="empty-box">
                      <p class="empty-title">Nessuna collezione presente</p>
                      <p class="empty-description">Aggiungi una collezione per evidenziare gruppi di prodotti.</p>
                    </div>
                  </td>
                </tr>
              {/each}
            </tbody>
          </table>
        {/if}
      </div>
    </section>
  {/if}

  {#if isModalOpen}
    <div class="fixed inset-0 z-50 flex items-center justify-center bg-[#111827]/55 p-4">
      <div class="surface-card w-full max-w-2xl p-5">
        <div class="mb-4 flex items-center justify-between">
          <h3 class="text-2xl font-bold text-[#1c2430]">{isEditMode ? 'Modifica' : 'Aggiungi'}</h3>
          <button class="btn-secondary" on:click={closeModal}>Chiudi</button>
        </div>

        {#if activeTab === 'products'}
          <div class="grid gap-2">
            <input class="form-input" bind:value={productForm.sku} placeholder="SKU" />
            <input class="form-input" bind:value={productForm.name} placeholder="Nome prodotto" />
            <textarea class="form-input" bind:value={productForm.description} rows="2" placeholder="Descrizione"></textarea>
            <input class="form-input" type="number" bind:value={productForm.price} min="0.01" step="0.01" placeholder="Prezzo" />
            <select class="form-input" bind:value={productForm.brandId}>
              {#each brands as brand}
                <option value={brand.id}>{brand.name}</option>
              {/each}
            </select>
            <select class="form-input" bind:value={productForm.categoryId}>
              {#each categories as category}
                <option value={category.id}>{category.name}</option>
              {/each}
            </select>
            <select class="form-input" multiple bind:value={productForm.collectionIds}>
              {#each collections as collection}
                <option value={collection.id}>{collection.name}</option>
              {/each}
            </select>
            <label class="text-sm text-[#5a6472]"><input type="checkbox" bind:checked={productForm.isNewArrival} /> Nuovo arrivo</label>
            <label class="text-sm text-[#5a6472]"><input type="checkbox" bind:checked={productForm.isBestSeller} /> Best seller</label>
          </div>
        {:else if activeTab === 'brands'}
          <div class="grid gap-2">
            <input class="form-input" bind:value={brandForm.name} placeholder="Nome brand" />
            <input class="form-input" bind:value={brandForm.slug} placeholder="Slug" />
            <textarea class="form-input" bind:value={brandForm.description} rows="2" placeholder="Descrizione"></textarea>
          </div>
        {:else if activeTab === 'categories'}
          <div class="grid gap-2">
            <input class="form-input" bind:value={categoryForm.name} placeholder="Nome categoria" />
            <input class="form-input" bind:value={categoryForm.slug} placeholder="Slug" />
            <textarea class="form-input" bind:value={categoryForm.description} rows="2" placeholder="Descrizione"></textarea>
          </div>
        {:else}
          <div class="grid gap-2">
            <input class="form-input" bind:value={collectionForm.name} placeholder="Nome collezione" />
            <input class="form-input" bind:value={collectionForm.slug} placeholder="Slug" />
            <textarea class="form-input" bind:value={collectionForm.description} rows="2" placeholder="Descrizione"></textarea>
            <label class="text-sm text-[#5a6472]"><input type="checkbox" bind:checked={collectionForm.isFeatured} /> In evidenza</label>
          </div>
        {/if}

        <div class="mt-4 flex justify-end gap-2">
          <button class="btn-secondary" on:click={closeModal}>Annulla</button>
          <button class="btn-primary" on:click={saveActiveEntity} disabled={isSaving}>{isSaving ? 'Salvataggio...' : 'Salva'}</button>
        </div>
      </div>
    </div>
  {/if}
</div>
