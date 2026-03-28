import { atom, computed } from 'nanostores';
import type { CartItemDto } from '../lib/api';

export type { CartItemDto as CartItem };

// ─── Persistence helpers ──────────────────────────────────────────────────────

function load<T>(key: string, fallback: T): T {
  if (typeof localStorage === 'undefined') return fallback;
  try {
    const raw = localStorage.getItem(key);
    return raw !== null ? (JSON.parse(raw) as T) : fallback;
  } catch {
    return fallback;
  }
}

function save<T>(key: string, value: T): void {
  if (typeof localStorage === 'undefined') return;
  try {
    localStorage.setItem(key, JSON.stringify(value));
  } catch { /* quota exceeded or private mode */ }
}

// ─── Stores ───────────────────────────────────────────────────────────────────

/** Stable cart ID – persisted so refreshes don't lose the cart */
export const cartId = atom<string>(load('cart:id', crypto.randomUUID()));
cartId.subscribe((v) => save('cart:id', v));

/** Anonymous user ID persisted per browser session profile. */
export const userId = atom<string>(load('cart:userId', crypto.randomUUID()));
userId.subscribe((v) => save('cart:userId', v));

/** Cart items mirror – kept in sync with the backend after each mutation */
export const cartItems = atom<CartItemDto[]>(load('cart:items', []));
cartItems.subscribe((v) => save('cart:items', v));

function roundMoney(value: number): number {
  return Math.round((value + Number.EPSILON) * 100) / 100;
}

export const cartTotal = computed(cartItems, (items) =>
  roundMoney(items.reduce((sum, item) => sum + item.quantity * item.unitPrice, 0))
);

export const cartCount = computed(cartItems, (items) =>
  items.reduce((n, item) => n + item.quantity, 0)
);

/** Replace local items from the server response */
export function syncCartFromServer(items: CartItemDto[]): void {
  cartItems.set(items);
}

/** Optimistically clear cart after successful checkout */
export function clearCart(): void {
  cartItems.set([]);
  cartId.set(crypto.randomUUID()); // fresh cart ID for next session
}

/** Reset local cart state after an order completion */
export function startNewCart(): void {
  clearCart();
}
