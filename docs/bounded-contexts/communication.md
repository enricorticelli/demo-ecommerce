# Bounded Context: Communication

## Scopo

Gestire comunicazioni esterne asincrone verso utenti finali tramite email.

## Responsabilita

1. Consumare eventi di integrazione rilevanti per notifiche utente.
2. Comporre e inviare email transactional (es. conferma ordine, ordine spedito).
3. Garantire idempotenza su consumer per evitare invii duplicati.

## Ownership dati

- Eventi di integrazione processati (deduplica).
- Configurazione tecnica canale email (SMTP).

## Integrazioni

- Consuma eventi da `Order` e `Shipping`.
- Invia email via SMTP verso provider/mock esterno (Mailpit in locale).

## Confini

Communication non modifica stato di Order/Shipping e non accede ai loro database.
