import { useState } from 'react';

import { authProvider } from './authProvider';

export function LoginPage() {
  const [isLoading, setIsLoading] = useState(false);

  const handleLogin = async () => {
    if (isLoading) {
      return;
    }

    setIsLoading(true);

    try {
      await authProvider.login?.({});
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <main
      style={{
        minHeight: '100vh',
        display: 'grid',
        placeItems: 'center',
        padding: '24px',
        background: 'linear-gradient(135deg, #f7f2ea 0%, #ebe3d4 100%)',
      }}
    >
      <section
        style={{
          width: 'min(420px, 100%)',
          padding: '32px',
          borderRadius: '24px',
          background: '#fffaf2',
          boxShadow: '0 24px 80px rgba(84, 58, 20, 0.12)',
        }}
      >
        <p style={{ margin: 0, fontSize: '12px', letterSpacing: '0.16em', textTransform: 'uppercase', color: '#8b6b3f' }}>
          Demo E-commerce
        </p>
        <h1 style={{ margin: '12px 0 8px', fontSize: '32px', lineHeight: 1.1, color: '#2c2216' }}>Backoffice</h1>
        <p style={{ margin: 0, color: '#5f4d37', lineHeight: 1.5 }}>
          Accedi con il tuo account Keycloak per gestire catalogo, collezioni e stock.
        </p>
        <button
          type="button"
          onClick={handleLogin}
          disabled={isLoading}
          style={{
            marginTop: '24px',
            width: '100%',
            border: 0,
            borderRadius: '999px',
            padding: '14px 18px',
            fontSize: '15px',
            fontWeight: 700,
            cursor: isLoading ? 'wait' : 'pointer',
            color: '#fffaf2',
            background: '#2c2216',
          }}
        >
          {isLoading ? 'Reindirizzamento...' : 'Continua con Keycloak'}
        </button>
      </section>
    </main>
  );
}