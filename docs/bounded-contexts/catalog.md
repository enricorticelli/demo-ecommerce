# Bounded Context: Catalog

## Scopo

Gestire il catalogo prodotti e i metadati commerciali (brand, categorie, collezioni).

## Responsabilita

1. Definire e mantenere anagrafiche prodotto.
2. Esporre query per listing e dettaglio prodotti.
3. Garantire coerenza dei dati catalogo interni al contesto.

## Ownership dati

- Prodotti.
- Brand.
- Categorie.
- Collezioni.

## Integrazioni

- Espone API allo storefront via gateway.
- Pubblica eventi quando cambiano informazioni rilevanti per altri contesti.

## Convenzioni implementative adottate

1. Endpoint API sottili: orchestrazione demandata a command/query service.
2. Mapping `View -> Response` centralizzato in mapper statici API.
3. Repository EF Core separati per aggregate (`Brand`, `Category`, `Collection`, `Product`).
4. Regole cross-entity in `Rules` applicative (unicita, referenze, vincoli delete).
5. Ricerca `searchTerm` implementata server-side in query component dedicati.
6. Event publishing via `IDomainEventPublisher` con adapter outbox in infrastructure.
7. Eventi `*V1` in `Shared.BuildingBlocks.Contracts.IntegrationEvents.Catalog` con metadata standard.

## Confini

Nessun altro contesto puo leggere/scrivere direttamente il database catalogo.
