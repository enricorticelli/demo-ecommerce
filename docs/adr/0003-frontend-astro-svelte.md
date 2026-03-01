# ADR 0003: Use Astro SSR + Svelte islands

## Status
Accepted

## Context
Frontend must be very fast, basic, and lightweight.

## Decision
Use Astro SSR for page rendering and Svelte islands for interactivity, with Nanostores for minimal shared state.

## Consequences
- Small client bundles and fast page load
- Easy progressive enhancement
- Team must manage mixed Astro/Svelte patterns
