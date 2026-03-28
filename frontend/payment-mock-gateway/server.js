const crypto = require('node:crypto');
const express = require('express');

const app = express();
app.use(express.json());
app.use(express.urlencoded({ extended: false }));

const checkouts = new Map();

const providers = new Set(['stripe', 'paypal', 'satispay']);

app.get('/health/live', (_req, res) => {
  res.json({ status: 'ok' });
});

app.get('/health/ready', (_req, res) => {
  res.json({ status: 'ok' });
});

app.post('/mock/:provider/checkouts', (req, res) => {
  const provider = normalizeProvider(req.params.provider);
  if (!providers.has(provider)) {
    return res.status(400).json({ detail: `Unsupported provider '${req.params.provider}'.` });
  }

  const sessionId = readGuid(req.body, 'sessionId', 'SessionId');
  const orderId = readGuid(req.body, 'orderId', 'OrderId');

  if (!sessionId || !orderId) {
    return res.status(400).json({ detail: 'SessionId and OrderId are required.' });
  }

  const checkoutId = crypto.randomUUID().replace(/-/g, '');

  const checkout = {
    checkoutId,
    providerCode: provider,
    sessionId,
    orderId,
    userId: readGuid(req.body, 'userId', 'UserId') || '00000000-0000-0000-0000-000000000000',
    amount: Number(readText(req.body, 'amount', 'Amount') || 0),
    paymentMethod: readText(req.body, 'paymentMethod', 'PaymentMethod') || provider,
    returnUrl: readText(req.body, 'returnUrl', 'ReturnUrl') || defaultReturnUrl(sessionId, orderId, provider),
    cancelUrl: readText(req.body, 'cancelUrl', 'CancelUrl') || defaultCancelUrl(sessionId, orderId, provider)
  };

  checkouts.set(checkoutId, checkout);

  const publicBaseUrl = (process.env.MockGateway__PublicBaseUrl || 'http://localhost:8082').replace(/\/+$/, '');
  const redirectUrl = `${publicBaseUrl}/checkout/${provider}/${checkoutId}`;

  return res.json({
    providerCode: provider,
    externalCheckoutId: checkoutId,
    redirectUrl,
    status: 'created'
  });
});

app.get('/checkout/:provider/:checkoutId', (req, res) => {
  const provider = normalizeProvider(req.params.provider);
  const checkoutId = req.params.checkoutId;
  const checkout = checkouts.get(checkoutId);

  if (!checkout || checkout.providerCode !== provider) {
    return res.status(404).send('Checkout session not found.');
  }

  const html = buildCheckoutHtml(checkout);
  return res.setHeader('Content-Type', 'text/html; charset=utf-8').send(html);
});

app.post('/checkout/:provider/:checkoutId/decision', async (req, res) => {
  const provider = normalizeProvider(req.params.provider);
  const checkoutId = req.params.checkoutId;
  const checkout = checkouts.get(checkoutId);

  if (!checkout || checkout.providerCode !== provider) {
    return res.status(404).send('Checkout session not found.');
  }

  const decision = String(req.body.decision || '').trim().toLowerCase();
  if (decision !== 'authorize' && decision !== 'reject') {
    return res.status(400).send("Decision must be 'authorize' or 'reject'.");
  }

  const isAuthorized = decision === 'authorize';
  const providerStatus = buildProviderStatus(provider, isAuthorized);
  const payload = {
    eventId: crypto.randomUUID().replace(/-/g, ''),
    sessionId: checkout.sessionId,
    externalCheckoutId: checkout.checkoutId,
    status: providerStatus,
    transactionId: isAuthorized ? `TX-${crypto.randomUUID().replace(/-/g, '')}` : null,
    reason: isAuthorized ? null : 'Payment cancelled by customer'
  };

  const rawPayload = JSON.stringify(payload);
  const webhook = await sendWebhook(provider, rawPayload);

  const targetBaseUrl = isAuthorized ? checkout.returnUrl : checkout.cancelUrl;
  const redirectUrl = appendQueryString(targetBaseUrl, {
    provider,
    checkoutId: checkout.checkoutId,
    decision: isAuthorized ? 'authorized' : 'rejected',
    webhook
  });

  checkouts.delete(checkoutId);
  return res.redirect(302, redirectUrl);
});

const host = process.env.HOST || '0.0.0.0';
const port = Number(process.env.PORT || 8080);

app.listen(port, host, () => {
  console.log(`payment-mock-gateway listening on http://${host}:${port}`);
});

function normalizeProvider(value) {
  return String(value || '').trim().toLowerCase();
}

function toProviderConfigPrefix(provider) {
  if (provider === 'paypal') return 'PayPal';
  if (provider === 'satispay') return 'Satispay';
  return 'Stripe';
}

function buildProviderStatus(provider, isAuthorized) {
  if (provider === 'stripe') return isAuthorized ? 'checkout.session.completed' : 'checkout.session.expired';
  if (provider === 'paypal') return isAuthorized ? 'CHECKOUT.ORDER.APPROVED' : 'CHECKOUT.ORDER.CANCELLED';
  if (provider === 'satispay') return isAuthorized ? 'ACCEPTED' : 'CANCELED';
  return isAuthorized ? 'authorized' : 'rejected';
}

async function sendWebhook(provider, rawPayload) {
  const webhookBaseUrl = (process.env.MockGateway__PaymentWebhookBaseUrl || 'http://gateway-api:8080').replace(/\/+$/, '');
  const providerPrefix = toProviderConfigPrefix(provider);
  const webhookSecret = process.env[`MockGateway__Providers__${providerPrefix}__WebhookSecret`] || '';
  const signature = `sha256=${computeHmac(rawPayload, webhookSecret)}`;

  const webhookUrl = `${webhookBaseUrl}/api/store/payment/v1/payments/webhooks/${provider}`;

  try {
    const response = await fetch(webhookUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Mock-Signature': signature
      },
      body: rawPayload
    });

    if (response.ok) return 'ok';
    return `error_${response.status}`;
  } catch (_err) {
    return 'error_network';
  }
}

function computeHmac(payload, secret) {
  return crypto.createHmac('sha256', Buffer.from(secret, 'utf8')).update(payload, 'utf8').digest('hex');
}

function defaultReturnUrl(sessionId, orderId, provider) {
  return `http://localhost:3000/payment/return?orderId=${encodeURIComponent(orderId)}&sessionId=${encodeURIComponent(sessionId)}&provider=${encodeURIComponent(provider)}&result=authorized`;
}

function defaultCancelUrl(sessionId, orderId, provider) {
  return `http://localhost:3000/payment/return?orderId=${encodeURIComponent(orderId)}&sessionId=${encodeURIComponent(sessionId)}&provider=${encodeURIComponent(provider)}&result=cancelled`;
}

function appendQueryString(url, query) {
  const separator = url.includes('?') ? '&' : '?';
  const qs = Object.entries(query)
    .map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(String(v))}`)
    .join('&');
  return `${url}${separator}${qs}`;
}

function providerLabel(provider) {
  if (provider === 'stripe') return 'Stripe';
  if (provider === 'paypal') return 'PayPal';
  if (provider === 'satispay') return 'Satispay';
  return provider;
}

function providerTheme(provider) {
  if (provider === 'stripe') {
    return {
      background: 'linear-gradient(135deg,#f5f7ff,#e8edff)',
      badgeBackground: '#635bff',
      badgeText: '#ffffff',
      cardBorder: '#d7ddff',
      title: '#0a2540',
      text: '#425466',
      mutedBackground: '#f6f9fc',
      mutedBorder: '#d7ddff',
      authorizeBackground: '#635bff',
      authorizeText: '#ffffff'
    };
  }

  if (provider === 'paypal') {
    return {
      background: 'linear-gradient(135deg,#f2f8ff,#e6f4ff)',
      badgeBackground: '#002991',
      badgeText: '#ffffff',
      cardBorder: '#bfe8ff',
      title: '#002991',
      text: '#1f3b5a',
      mutedBackground: '#f4fbff',
      mutedBorder: '#bfe8ff',
      authorizeBackground: '#008cff',
      authorizeText: '#ffffff'
    };
  }

  if (provider === 'satispay') {
    return {
      background: 'linear-gradient(135deg,#fff6f1,#ffebe2)',
      badgeBackground: '#ef4723',
      badgeText: '#ffffff',
      cardBorder: '#ffd8cc',
      title: '#1c0e15',
      text: '#5a3a32',
      mutedBackground: '#fef7ec',
      mutedBorder: '#ffd8cc',
      authorizeBackground: '#ef4723',
      authorizeText: '#ffffff'
    };
  }

  return {
    background: 'linear-gradient(135deg,#f4f6f8,#eaf0f8)',
    badgeBackground: '#1d4ed8',
    badgeText: '#ffffff',
    cardBorder: '#e5e7eb',
    title: '#111827',
    text: '#4b5563',
    mutedBackground: '#f9fafb',
    mutedBorder: '#e5e7eb',
    authorizeBackground: '#111827',
    authorizeText: '#ffffff'
  };
}

function buildCheckoutHtml(checkout) {
  const provider = checkout.providerCode;
  const label = providerLabel(checkout.providerCode);
  const theme = providerTheme(provider);
  const amount = Number.isFinite(checkout.amount) ? checkout.amount.toFixed(2) : '0.00';

  return `<!doctype html>
<html lang="it">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width,initial-scale=1" />
  <title>${escapeHtml(label)} Checkout Mock</title>
  <style>
    :root { font-family: "Segoe UI", Arial, sans-serif; color-scheme: light; }
    body { margin: 0; background: ${theme.background}; min-height: 100vh; display: grid; place-items: center; }
    .card { width: min(560px, 92vw); background: #fff; border: 1px solid ${theme.cardBorder}; border-radius: 18px; padding: 28px; box-shadow: 0 18px 42px rgba(0,0,0,.10); }
    .header { display: flex; align-items: center; gap: 12px; }
    .badge { display:inline-block; font-size:12px; font-weight:700; letter-spacing:.08em; text-transform:uppercase; color:${theme.badgeText}; background:${theme.badgeBackground}; padding:6px 10px; border-radius:999px; }
    .provider-chip { font-size: 12px; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; color:${theme.title}; border: 1px solid ${theme.cardBorder}; background: #fff; padding: 6px 10px; border-radius: 999px; }
    h1 { margin: 14px 0 6px; font-size: 30px; color:${theme.title}; }
    p { margin: 0; color:${theme.text}; }
    .grid { margin-top: 20px; border:1px solid ${theme.mutedBorder}; border-radius: 12px; padding: 14px; background: ${theme.mutedBackground}; display:grid; gap:8px; font-size:14px; }
    .row { display:flex; justify-content:space-between; gap:12px; }
    .mono { font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace; font-size:12px; color:#111827; word-break:break-all; text-align:right; }
    .amount { font-size: 28px; font-weight: 800; color:#111827; margin-top: 16px; }
    .actions { margin-top: 24px; display:grid; gap:10px; grid-template-columns: 1fr 1fr; }
    button { border:0; border-radius:12px; padding:12px 14px; font-size:15px; font-weight:700; cursor:pointer; }
    .reject { background:#f3f4f6; color:#111827; }
    .authorize { background:${theme.authorizeBackground}; color:${theme.authorizeText}; }
  </style>
</head>
<body>
  <main class="card">
    <div class="header">
      <span class="badge">Hosted Payment</span>
      <span class="provider-chip">${escapeHtml(label)}</span>
    </div>
    <h1>${escapeHtml(label)} mock checkout</h1>
    <p>Pagina esterna allo storefront. Il risultato viene inviato via callback server-to-server.</p>

    <div class="grid">
      <div class="row"><span>Provider</span><strong>${escapeHtml(label)}</strong></div>
      <div class="row"><span>Sessione pagamento</span><span class="mono">${escapeHtml(checkout.sessionId)}</span></div>
      <div class="row"><span>Checkout esterno</span><span class="mono">${escapeHtml(checkout.checkoutId)}</span></div>
      <div class="row"><span>Ordine</span><span class="mono">${escapeHtml(checkout.orderId)}</span></div>
    </div>

    <div class="amount">EUR ${escapeHtml(amount)}</div>

    <form method="post" action="/checkout/${escapeHtml(checkout.providerCode)}/${escapeHtml(checkout.checkoutId)}/decision" class="actions">
      <button class="reject" type="submit" name="decision" value="reject">Annulla pagamento</button>
      <button class="authorize" type="submit" name="decision" value="authorize">Autorizza pagamento</button>
    </form>
  </main>
</body>
</html>`;
}

function escapeHtml(value) {
  return String(value)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/\"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

function readText(body, ...keys) {
  for (const key of keys) {
    if (body[key] !== undefined && body[key] !== null) {
      return String(body[key]);
    }
  }
  return '';
}

function readGuid(body, ...keys) {
  const value = readText(body, ...keys).trim();
  return value || '';
}
