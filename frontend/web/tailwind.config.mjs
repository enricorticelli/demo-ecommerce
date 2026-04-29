export default {
  content: ['./src/**/*.{astro,html,js,svelte,ts}'],
  theme: {
    extend: {
      fontFamily: {
        title: ["'Cormorant Garamond'", 'Georgia', 'serif'],
        body: ["'Manrope'", "'Segoe UI'", 'sans-serif'],
      },
      colors: {
        brand: {
          50: '#eef7ff',
          500: '#0ea5e9',
          700: '#0369a1',
        },
      },
    },
  },
  plugins: [],
};
