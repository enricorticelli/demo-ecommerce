export const CATEGORY_MAP: Record<string, string> = {
  ELEC: 'Elettronica',
  HOME: 'Casa',
  FASH: 'Moda',
  SPORT: 'Sport',
  BEAUTY: 'Bellezza',
  BOOK: 'Libri',
  FOOD: 'Alimentari',
  TOYS: 'Giocattoli',
  TOOL: 'Utensili',
  AUTO: 'Auto',
};

export function stableHash(value: string): number {
  let hash = 0;
  for (let i = 0; i < value.length; i++) {
    hash = (hash * 31 + value.charCodeAt(i)) >>> 0;
  }
  return hash;
}

/** Deterministic product image via picsum (consistent seed = consistent image) */
export function getProductImage(sku: string, width = 600, height = 400): string {
  return `https://picsum.photos/seed/${encodeURIComponent(sku)}/${width}/${height}`;
}

/** Deterministic star rating between 3.5 and 5.0 */
export function getProductRating(productId: string): number {
  const hash = stableHash(`rating:${productId}`);
  return parseFloat((3.5 + (hash % 16) / 10).toFixed(1));
}

/** Deterministic review count between 12 and 1200 */
export function getProductReviewCount(productId: string): number {
  return (stableHash(`reviews:${productId}`) % 1189) + 12;
}

export type StockStatus = 'in_stock' | 'low_stock' | 'preorder' | 'out_of_stock';

/** Deterministic stock status */
export function getProductStock(productId: string): StockStatus {
  const bucket = stableHash(`stock:${productId}`) % 11;
  if (bucket < 6) return 'in_stock';
  if (bucket < 9) return 'low_stock';
  if (bucket === 9) return 'preorder';
  return 'out_of_stock';
}

export const STOCK_LABELS: Record<StockStatus, string> = {
  in_stock: 'Disponibile',
  low_stock: 'Ultimi pezzi',
  preorder: 'Disponibile in 48h',
  out_of_stock: 'Esaurito',
};

export const STOCK_COLORS: Record<StockStatus, string> = {
  in_stock: 'text-emerald-600',
  low_stock: 'text-amber-600',
  preorder: 'text-sky-600',
  out_of_stock: 'text-rose-600',
};

/** Infer a display category from SKU prefix or product text */
export function getProductCategory(product: {
  sku: string;
  name: string;
  description: string;
  categoryName?: string;
}): string {
  if (product.categoryName) return product.categoryName;
  const prefix = product.sku.split('-')[0]?.toUpperCase() ?? '';
  if (CATEGORY_MAP[prefix]) return CATEGORY_MAP[prefix];
  const text = `${product.name} ${product.description}`.toLowerCase();
  if (/shoe|shirt|jacket|dress|pants|sneaker|boot|coat|jeans/i.test(text)) return 'Moda';
  if (/kitchen|desk|lamp|sofa|pillow|towel|chair|shelf|rug|vase/i.test(text)) return 'Casa';
  if (/headphone|laptop|phone|tablet|camera|monitor|keyboard|mouse|speaker|charger/i.test(text))
    return 'Elettronica';
  if (/yoga|gym|bike|running|tennis|football|swim|hiking|kettlebell/i.test(text)) return 'Sport';
  if (/cream|serum|shampoo|perfume|lipstick|makeup|mascara|moisturizer/i.test(text))
    return 'Bellezza';
  return 'Lifestyle';
}

/** Ordered list of all categories for filter UI */
export const ALL_CATEGORIES = [
  'Bellezza',
  'Casa',
  'Elettronica',
  'Lifestyle',
  'Moda',
  'Sport',
  ...Object.values(CATEGORY_MAP).filter(
    (v) => !['Bellezza', 'Casa', 'Elettronica', 'Lifestyle', 'Moda', 'Sport'].includes(v)
  ),
];

/** Hero gradient by category */
export const CATEGORY_GRADIENTS: Record<string, string> = {
  Elettronica: 'from-sky-100 to-indigo-200',
  Casa: 'from-amber-100 to-orange-200',
  Moda: 'from-rose-100 to-pink-200',
  Sport: 'from-emerald-100 to-teal-200',
  Bellezza: 'from-fuchsia-100 to-purple-200',
  Lifestyle: 'from-slate-100 to-gray-200',
};

/** Featured product accent colors (cycle through a palette) */
export function getProductAccent(productId: string): string {
  const ACCENTS = [
    'bg-amber-100',
    'bg-sky-100',
    'bg-rose-100',
    'bg-emerald-100',
    'bg-violet-100',
    'bg-orange-100',
  ];
  return ACCENTS[stableHash(productId) % ACCENTS.length];
}
