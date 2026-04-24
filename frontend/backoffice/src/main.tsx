import React from 'react';
import { createRoot } from 'react-dom/client';

import './admin-overrides.css';
import { completeSignInIfNeeded } from './auth/keycloak';
import { App } from './App';

async function bootstrap() {
  await completeSignInIfNeeded();

  createRoot(document.getElementById('root')!).render(
    <React.StrictMode>
      <App />
    </React.StrictMode>,
  );
}

bootstrap().catch((error) => {
  console.error(error);
});
