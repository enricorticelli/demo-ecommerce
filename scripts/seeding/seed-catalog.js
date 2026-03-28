#!/usr/bin/env node

const fs = require('fs');
const path = require('path');

const DEFAULT_COUNTS = {
  brands: 40,
  categories: 80,
  collections: 50,
  products: 2000
};

async function main() {
  const args = parseArgs(process.argv.slice(2));
  const envFromFile = loadEnvFile(path.resolve(process.cwd(), '.env'));
  const resolvedGatewayPort =
    args.gatewayPort ||
    envFromFile.GATEWAY_HOST_PORT ||
    envFromFile.GATEWAY_PORT ||
    process.env.GATEWAY_HOST_PORT ||
    process.env.GATEWAY_PORT ||
    '18080';

  const baseUrl = normalizeBaseUrl(
    args.baseUrl ||
    envFromFile.PUBLIC_GATEWAY_URL ||
    process.env.PUBLIC_GATEWAY_URL ||
    `http://localhost:${resolvedGatewayPort}`
  );
  const timeoutMs = toInt(args.timeoutMs || process.env.SEED_TIMEOUT_MS || envFromFile.SEED_TIMEOUT_MS, 10000);
  const concurrency = Math.max(1, toInt(args.concurrency || process.env.SEED_CONCURRENCY || envFromFile.SEED_CONCURRENCY, 12));
  const dryRun = Boolean(args.dryRun);
  const verbose = Boolean(args.verbose);
  const correlationPrefix = `seed-catalog-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`;

  const http = createHttpClient({
    baseUrl,
    timeoutMs,
    correlationPrefix,
    verbose
  });

  const startedAt = Date.now();
  const report = {
    mode: dryRun ? 'dry-run' : 'reset-seed',
    baseUrl,
    timeoutMs,
    concurrency,
    target: { ...DEFAULT_COUNTS },
    existing: { brands: 0, categories: 0, collections: 0, products: 0 },
    deleted: { brands: 0, categories: 0, collections: 0, products: 0 },
    created: { brands: 0, categories: 0, collections: 0, products: 0 },
    failures: [],
    durationMs: 0
  };

  console.log(`[seed] Starting catalog seeding (${report.mode}) against ${baseUrl}`);

  const existing = await fetchExisting(http);
  report.existing = {
    brands: existing.brands.length,
    categories: existing.categories.length,
    collections: existing.collections.length,
    products: existing.products.length
  };

  logVerbose(verbose, `[seed] Existing: ${JSON.stringify(report.existing)}`);

  const resetOps = [
    { key: 'products', items: existing.products, deletePath: (x) => `/api/store/catalog/v1/products/${x.id}` },
    { key: 'collections', items: existing.collections, deletePath: (x) => `/api/store/catalog/v1/collections/${x.id}` },
    { key: 'categories', items: existing.categories, deletePath: (x) => `/api/store/catalog/v1/categories/${x.id}` },
    { key: 'brands', items: existing.brands, deletePath: (x) => `/api/store/catalog/v1/brands/${x.id}` }
  ];

  for (const op of resetOps) {
    if (dryRun) {
      report.deleted[op.key] = op.items.length;
      console.log(`[seed] [dry-run] Would delete ${op.items.length} ${op.key}`);
      continue;
    }

    const deletion = await runPool(op.items, concurrency, async (item) => {
      await http.request('DELETE', op.deletePath(item));
    });

    report.deleted[op.key] = deletion.ok;
    pushFailures(report.failures, op.key, deletion.errors);
    console.log(`[seed] Deleted ${deletion.ok}/${op.items.length} ${op.key}`);
  }

  const seed = generateSeedData(DEFAULT_COUNTS);

  if (dryRun) {
    report.created = {
      brands: seed.brands.length,
      categories: seed.categories.length,
      collections: seed.collections.length,
      products: seed.products.length
    };
    console.log(`[seed] [dry-run] Would create ${seed.brands.length} brands, ${seed.categories.length} categories, ${seed.collections.length} collections, ${seed.products.length} products`);
    console.log('[seed] [dry-run] Sample product payload:');
    console.log(JSON.stringify(seed.products[0], null, 2));
  } else {
    const brandMap = await createBrands(http, seed.brands, concurrency, report);
    const categoryMap = await createCategories(http, seed.categories, concurrency, report);
    const collectionMap = await createCollections(http, seed.collections, concurrency, report);

    const productPayloads = seed.products.map((product) => ({
      sku: product.sku,
      name: product.name,
      description: product.description,
      price: product.price,
      brandId: brandMap.get(product.brandSlug),
      categoryId: categoryMap.get(product.categorySlug),
      collectionIds: product.collectionSlugs.map((slug) => collectionMap.get(slug)).filter(Boolean),
      isNewArrival: product.isNewArrival,
      isBestSeller: product.isBestSeller
    }));

    const creation = await runPool(productPayloads, concurrency, async (payload) => {
      if (!payload.brandId || !payload.categoryId || payload.collectionIds.length === 0) {
        throw new Error(`Invalid product references for sku '${payload.sku}'`);
      }

      await http.request('POST', '/api/store/catalog/v1/products', payload);
    });

    report.created.products = creation.ok;
    pushFailures(report.failures, 'products', creation.errors);
    console.log(`[seed] Created ${creation.ok}/${productPayloads.length} products`);
  }

  report.durationMs = Date.now() - startedAt;

  const throughput = report.durationMs > 0
    ? Math.round((report.created.products / (report.durationMs / 1000)) * 100) / 100
    : 0;

  console.log('[seed] Completed');
  console.log(JSON.stringify({
    ...report,
    throughputProductsPerSec: throughput,
    failureCount: report.failures.length
  }, null, 2));

  if (report.failures.length > 0) {
    process.exitCode = 1;
  }
}

function parseArgs(argv) {
  const args = {};

  for (let i = 0; i < argv.length; i += 1) {
    const token = argv[i];

    if (token === '--dry-run') {
      args.dryRun = true;
      continue;
    }

    if (token === '--verbose') {
      args.verbose = true;
      continue;
    }

    if (token === '--base-url') {
      args.baseUrl = argv[++i];
      continue;
    }

    if (token === '--gateway-port') {
      args.gatewayPort = argv[++i];
      continue;
    }

    if (token === '--timeout-ms') {
      args.timeoutMs = argv[++i];
      continue;
    }

    if (token === '--concurrency') {
      args.concurrency = argv[++i];
      continue;
    }

    throw new Error(`Unknown argument: ${token}`);
  }

  return args;
}

function loadEnvFile(filePath) {
  if (!fs.existsSync(filePath)) {
    return {};
  }

  const lines = fs.readFileSync(filePath, 'utf8').split(/\r?\n/);
  const result = {};

  for (const line of lines) {
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith('#')) {
      continue;
    }

    const idx = trimmed.indexOf('=');
    if (idx === -1) {
      continue;
    }

    const key = trimmed.slice(0, idx).trim();
    let value = trimmed.slice(idx + 1).trim();

    if ((value.startsWith('"') && value.endsWith('"')) || (value.startsWith("'") && value.endsWith("'"))) {
      value = value.slice(1, -1);
    }

    result[key] = value;
  }

  return result;
}

function createHttpClient({ baseUrl, timeoutMs, correlationPrefix, verbose }) {
  let counter = 0;

  return {
    async request(method, pathName, body) {
      const url = `${baseUrl}${pathName}`;
      const correlationId = `${correlationPrefix}-${++counter}`;
      const maxRetries = 4;
      const baseDelayMs = 250;

      for (let attempt = 1; attempt <= maxRetries; attempt += 1) {
        const controller = new AbortController();
        const timer = setTimeout(() => controller.abort(), timeoutMs);

        try {
          const headers = {
            'x-correlation-id': correlationId
          };
          if (body !== undefined) {
            headers['content-type'] = 'application/json';
          }

          logVerbose(verbose, `[http] ${method} ${url} (attempt ${attempt})`);

          const response = await fetch(url, {
            method,
            headers,
            body: body === undefined ? undefined : JSON.stringify(body),
            signal: controller.signal
          });

          clearTimeout(timer);

          if (!response.ok) {
            const errorBody = await safeReadBody(response);
            const shouldRetry = isTransientStatus(response.status);

            if (shouldRetry && attempt < maxRetries) {
              await delay(backoffMs(baseDelayMs, attempt));
              continue;
            }

            throw new Error(`${method} ${pathName} failed with ${response.status}: ${errorBody}`);
          }

          const contentType = response.headers.get('content-type') || '';
          if (contentType.includes('application/json')) {
            return await response.json();
          }

          return null;
        } catch (error) {
          clearTimeout(timer);

          const transient = isTransientError(error);
          if (transient && attempt < maxRetries) {
            await delay(backoffMs(baseDelayMs, attempt));
            continue;
          }

          throw error;
        }
      }

      throw new Error(`${method} ${pathName} exhausted retries`);
    }
  };
}

async function fetchExisting(http) {
  const [brands, categories, collections, products] = await Promise.all([
    http.request('GET', '/api/store/catalog/v1/brands'),
    http.request('GET', '/api/store/catalog/v1/categories'),
    http.request('GET', '/api/store/catalog/v1/collections'),
    http.request('GET', '/api/store/catalog/v1/products')
  ]);

  return {
    brands: Array.isArray(brands) ? brands : [],
    categories: Array.isArray(categories) ? categories : [],
    collections: Array.isArray(collections) ? collections : [],
    products: Array.isArray(products) ? products : []
  };
}

async function createBrands(http, brands, concurrency, report) {
  const map = new Map();
  const result = await runPool(brands, concurrency, async (brand) => {
    const created = await http.request('POST', '/api/store/catalog/v1/brands', {
      name: brand.name,
      slug: brand.slug,
      description: brand.description
    });

    map.set(brand.slug, created.id);
  });

  report.created.brands = result.ok;
  pushFailures(report.failures, 'brands', result.errors);
  console.log(`[seed] Created ${result.ok}/${brands.length} brands`);
  return map;
}

async function createCategories(http, categories, concurrency, report) {
  const map = new Map();
  const result = await runPool(categories, concurrency, async (category) => {
    const created = await http.request('POST', '/api/store/catalog/v1/categories', {
      name: category.name,
      slug: category.slug,
      description: category.description
    });

    map.set(category.slug, created.id);
  });

  report.created.categories = result.ok;
  pushFailures(report.failures, 'categories', result.errors);
  console.log(`[seed] Created ${result.ok}/${categories.length} categories`);
  return map;
}

async function createCollections(http, collections, concurrency, report) {
  const map = new Map();
  const result = await runPool(collections, concurrency, async (collection) => {
    const created = await http.request('POST', '/api/store/catalog/v1/collections', {
      name: collection.name,
      slug: collection.slug,
      description: collection.description,
      isFeatured: collection.isFeatured
    });

    map.set(collection.slug, created.id);
  });

  report.created.collections = result.ok;
  pushFailures(report.failures, 'collections', result.errors);
  console.log(`[seed] Created ${result.ok}/${collections.length} collections`);
  return map;
}

function generateSeedData(counts) {
  const rnd = mulberry32(20260307);

  const brands = Array.from({ length: counts.brands }, (_, i) => {
    const name = `${pick(rnd, BRAND_ADJECTIVES)} ${pick(rnd, BRAND_NOUNS)} ${i + 1}`;
    const slug = slugify(name);
    return {
      name,
      slug,
      description: `Brand profile ${i + 1} focused on quality, style and performance.`
    };
  });

  const categories = Array.from({ length: counts.categories }, (_, i) => {
    const name = `${pick(rnd, CATEGORY_PREFIXES)} ${pick(rnd, CATEGORY_TYPES)} ${i + 1}`;
    const slug = slugify(name);
    return {
      name,
      slug,
      description: `Category ${name} for merchandising and search optimization.`
    };
  });

  const collections = Array.from({ length: counts.collections }, (_, i) => {
    const season = pick(rnd, COLLECTION_SEASONS);
    const tone = pick(rnd, COLLECTION_TONES);
    const name = `${season} ${tone} Drop ${i + 1}`;
    const slug = slugify(name);
    return {
      name,
      slug,
      description: `${season} collection curated for ${tone.toLowerCase()} aesthetics.`,
      isFeatured: rnd() < 0.32
    };
  });

  const products = Array.from({ length: counts.products }, (_, i) => {
    const brand = pick(rnd, brands);
    const category = pick(rnd, categories);
    const categoryType = extractCategoryType(category.name);

    const collectionCount = weightedCollectionCount(rnd());
    const collectionSlugs = pickUnique(rnd, collections, collectionCount).map((x) => x.slug);

    const model = buildProductModelName(rnd, brand.name, categoryType);
    const sku = `SKU-${String(i + 1).padStart(5, '0')}`;
    const price = priceForCategory(categoryType, rnd);

    return {
      sku,
      name: model,
      description: buildProductDescription(rnd, categoryType),
      price,
      brandSlug: brand.slug,
      categorySlug: category.slug,
      collectionSlugs,
      isNewArrival: rnd() < 0.24,
      isBestSeller: rnd() < 0.14
    };
  });

  return { brands, categories, collections, products };
}

async function runPool(items, concurrency, worker) {
  if (items.length === 0) {
    return { ok: 0, failed: 0, errors: [] };
  }

  let index = 0;
  let ok = 0;
  const errors = [];

  const workers = Array.from({ length: Math.min(concurrency, items.length) }, async () => {
    while (true) {
      const current = index;
      index += 1;
      if (current >= items.length) {
        return;
      }

      const item = items[current];
      try {
        await worker(item, current);
        ok += 1;
      } catch (error) {
        errors.push({ index: current, message: normalizeError(error) });
      }
    }
  });

  await Promise.all(workers);

  return {
    ok,
    failed: items.length - ok,
    errors
  };
}

function normalizeBaseUrl(value) {
  return String(value || '').trim().replace(/\/+$/, '');
}

function toInt(value, fallbackValue) {
  const parsed = Number.parseInt(String(value), 10);
  return Number.isFinite(parsed) ? parsed : fallbackValue;
}

function toMoney(value) {
  return Math.round(value * 100) / 100;
}

function slugify(value) {
  return value
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 128);
}

function pick(rnd, list) {
  return list[Math.floor(rnd() * list.length)];
}

function pickUnique(rnd, list, count) {
  const used = new Set();
  const output = [];

  while (output.length < count && used.size < list.length) {
    const idx = Math.floor(rnd() * list.length);
    if (used.has(idx)) {
      continue;
    }

    used.add(idx);
    output.push(list[idx]);
  }

  return output;
}

function weightedCollectionCount(value) {
  if (value < 0.62) {
    return 1;
  }
  if (value < 0.9) {
    return 2;
  }
  return 3;
}

function extractCategoryType(categoryName) {
  const withoutIndex = String(categoryName || '').replace(/\s+\d+$/, '').trim();
  const tokens = withoutIndex.split(/\s+/);
  if (tokens.length === 0) {
    return 'Accessories';
  }

  const audiencePrefixes = new Set([
    'men', 'women', 'kids', 'unisex', 'performance', 'lifestyle', 'outdoor', 'training', 'running', 'travel'
  ]);

  if (tokens.length > 1 && audiencePrefixes.has(tokens[0].toLowerCase())) {
    return tokens.slice(1).join(' ');
  }

  return withoutIndex;
}

function sanitizeBrandName(name) {
  return String(name || '').replace(/\s+\d+$/, '').trim();
}

function buildProductModelName(rnd, brandName, categoryType) {
  const brand = sanitizeBrandName(brandName);
  const line = pick(rnd, PRODUCT_LINES);
  const style = pick(rnd, PRODUCT_STYLES);
  return `${brand} ${line} ${style} ${categoryType}`;
}

function buildProductDescription(rnd, categoryType) {
  const featureA = pick(rnd, PRODUCT_FEATURES);
  const featureB = pick(rnd, PRODUCT_FEATURES.filter((x) => x !== featureA));
  const useCase = pick(rnd, PRODUCT_USE_CASES);
  return `${categoryType} con ${featureA} e ${featureB}, pensato per ${useCase}.`;
}

function priceForCategory(categoryType, rnd) {
  const normalized = String(categoryType || '').toLowerCase();
  const profile = PRICE_PROFILES.find((x) => x.match.some((term) => normalized.includes(term))) || DEFAULT_PRICE_PROFILE;
  return toMoney(profile.min + rnd() * (profile.max - profile.min));
}

function mulberry32(seed) {
  let t = seed >>> 0;
  return function rnd() {
    t += 0x6D2B79F5;
    let x = t;
    x = Math.imul(x ^ (x >>> 15), x | 1);
    x ^= x + Math.imul(x ^ (x >>> 7), x | 61);
    return ((x ^ (x >>> 14)) >>> 0) / 4294967296;
  };
}

function isTransientStatus(status) {
  return status === 408 || status === 429 || status >= 500;
}

function isTransientError(error) {
  if (!error) {
    return false;
  }

  const name = String(error.name || '');
  if (name === 'AbortError') {
    return true;
  }

  const message = String(error.message || '').toLowerCase();
  return message.includes('fetch failed') || message.includes('network') || message.includes('timed out');
}

async function safeReadBody(response) {
  try {
    const text = await response.text();
    return text.slice(0, 500);
  } catch {
    return '<body-unavailable>';
  }
}

function backoffMs(baseDelayMs, attempt) {
  const jitter = Math.floor(Math.random() * 50);
  return baseDelayMs * (2 ** (attempt - 1)) + jitter;
}

function delay(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function logVerbose(enabled, message) {
  if (!enabled) {
    return;
  }

  console.log(message);
}

function pushFailures(target, area, errors) {
  for (const error of errors) {
    target.push({ area, ...error });
  }
}

function normalizeError(error) {
  if (!error) {
    return 'unknown error';
  }

  if (error instanceof Error) {
    return error.message;
  }

  return String(error);
}

const BRAND_ADJECTIVES = [
  'North', 'Urban', 'Aero', 'Prime', 'Nova', 'Atlas', 'Motion', 'Core', 'Peak', 'Pulse',
  'Vertex', 'Element', 'Drift', 'Forge', 'Echo', 'Nimbus', 'Summit', 'Vector', 'Ridge', 'Lumen'
];

const BRAND_NOUNS = [
  'Wear', 'Lab', 'Collective', 'Studio', 'Makers', 'Thread', 'Works', 'House', 'Athletics', 'Engine',
  'Supply', 'Fabric', 'Outfit', 'Motion', 'Gear', 'Edition', 'Form', 'Line', 'Craft', 'Union'
];

const CATEGORY_PREFIXES = [
  'Men', 'Women', 'Kids', 'Unisex', 'Performance', 'Lifestyle', 'Outdoor', 'Training', 'Running', 'Travel'
];

const CATEGORY_TYPES = [
  'Shoes', 'Sneakers', 'Jackets', 'Hoodies', 'Pants', 'Shorts', 'Tees', 'Accessories', 'Bags', 'Socks',
  'Caps', 'Sweatshirts', 'Tanks', 'Leggings', 'Sandals', 'Boots'
];

const COLLECTION_SEASONS = [
  'Spring', 'Summer', 'Autumn', 'Winter', 'Resort', 'Holiday', 'Urban', 'Trail', 'Studio', 'Archive'
];

const COLLECTION_TONES = [
  'Essential', 'Premium', 'Minimal', 'Bold', 'Monochrome', 'Heritage', 'Neon', 'Earth', 'Classic', 'Future'
];

const PRODUCT_LINES = [
  'Essential', 'Active', 'Performance', 'Everyday', 'Urban', 'Trail', 'Studio', 'Heritage', 'Motion', 'Core'
];

const PRODUCT_STYLES = [
  'Fit', 'Flex', 'Comfort', 'Tech', 'Prime', 'Hybrid', 'Pro', 'Light', 'All-Season', 'Performance'
];

const PRODUCT_FEATURES = [
  'materiali traspiranti', 'cuciture rinforzate', 'vestibilita regolare', 'finitura morbida', 'supporto ergonomico',
  'struttura leggera', 'asciugatura rapida', 'resistenza all usura', 'interno confortevole', 'dettagli riflettenti'
];

const PRODUCT_USE_CASES = [
  'allenamento quotidiano', 'tempo libero', 'uso urbano', 'sessioni ad alta intensita', 'lunghe camminate',
  'viaggi e spostamenti', 'attivita outdoor', 'routine activewear'
];

const DEFAULT_PRICE_PROFILE = { min: 19.9, max: 79.9 };

const PRICE_PROFILES = [
  { match: ['socks'], min: 5.9, max: 18.9 },
  { match: ['caps'], min: 12.9, max: 34.9 },
  { match: ['accessories'], min: 9.9, max: 39.9 },
  { match: ['tees', 'tanks'], min: 14.9, max: 44.9 },
  { match: ['hoodies', 'sweatshirts'], min: 34.9, max: 89.9 },
  { match: ['jackets'], min: 49.9, max: 139.9 },
  { match: ['pants', 'leggings'], min: 29.9, max: 89.9 },
  { match: ['shorts'], min: 19.9, max: 54.9 },
  { match: ['bags'], min: 24.9, max: 99.9 },
  { match: ['sandals'], min: 24.9, max: 69.9 },
  { match: ['shoes', 'sneakers'], min: 44.9, max: 149.9 },
  { match: ['boots'], min: 79.9, max: 189.9 }
];

main().catch((error) => {
  console.error(`[seed] Fatal error: ${normalizeError(error)}`);
  process.exit(1);
});
