import React from 'react';
import { createRoot } from 'react-dom/client';

import './admin-overrides.css';
import { completeSignInIfNeeded, getCurrentUser, userManager } from './auth/keycloak';
import { App } from './App';

async function bootstrap() {
  await completeSignInIfNeeded();

  const user = await getCurrentUser();
  if (!user) {
    await userManager.signinRedirect();
    return;
  }

  createRoot(document.getElementById('root')!).render(
    <React.StrictMode>
      <App />
    </React.StrictMode>,
  );
}

bootstrap().catch((error) => {
  console.error(error);
});
