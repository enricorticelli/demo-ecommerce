import { atom, computed } from 'nanostores';

export type CartItem = {
  productId: string;
  sku: string;
  name: string;
  quantity: number;
  unitPrice: number;
};

export const cartId = atom(crypto.randomUUID());
export const userId = atom('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');
export const cartItems = atom<CartItem[]>([]);
export const cartTotal = computed(cartItems, ($items) =>
  $items.reduce((sum, item) => sum + item.quantity * item.unitPrice, 0)
);
