import { atom } from 'nanostores';

export type ToastType = 'success' | 'error' | 'info';

export type Toast = {
  id: string;
  message: string;
  type: ToastType;
};

export const toasts = atom<Toast[]>([]);

export function addToast(message: string, type: ToastType = 'info', durationMs = 4000): void {
  const id = crypto.randomUUID();
  toasts.set([...toasts.get(), { id, message, type }]);
  setTimeout(() => removeToast(id), durationMs);
}

export function removeToast(id: string): void {
  toasts.set(toasts.get().filter((t) => t.id !== id));
}

// ─── Cart drawer ─────────────────────────────────────────────────────────────

export const cartDrawerOpen = atom<boolean>(false);

export function openCartDrawer(): void {
  cartDrawerOpen.set(true);
}

export function closeCartDrawer(): void {
  cartDrawerOpen.set(false);
}
