# API Patterns: Requests, Responses, Mappers

Questo documento raccoglie i pattern condivisi usati nei progetti `*.Api` del repository.

## 1) Obiettivo
- Uniformare la forma dei contratti HTTP.
- Ridurre duplicazioni di mapping negli endpoint.
- Rendere i boundary API prevedibili per frontend web/backoffice e integrazioni.

## 2) Struttura consigliata
Per ogni progetto API:
- `Contracts/Requests/*.cs`
- `Contracts/Responses/*.cs`
- `Contracts/*Routes.cs`
- `Endpoints/*Endpoints.cs`
- `Endpoints/*Mapper.cs`

## 3) Requests
Regole:
- Usare record dedicati per input endpoint.
- Applicare validazioni di boundary (DataAnnotations/validator) sul request model.
- Non riusare command applicativi come payload HTTP.

Esempio:
```csharp
public sealed record UpdateShipmentStatusRequest(
    [property: Required, StringLength(32)] string Status);
```

## 4) Responses
Regole:
- Usare record dedicati per output endpoint.
- Restituire shape stabili, allineate ai consumer (`frontend/web`, `frontend/admin`).
- Evitare leakage di dettagli infrastrutturali o dominio interno.

Esempio:
```csharp
public sealed record ShipmentResponse(
    Guid Id,
    Guid OrderId,
    Guid UserId,
    string TrackingCode,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? DeliveredAtUtc);
```

## 5) Mapper statici
Regole:
- Creare classi statiche `*Mapper.cs` per modulo/area endpoint.
- Centralizzare mapping:
  - `Request -> Command/Integration Payload`
  - `View/Model -> Response`
- Non inserire business logic nei mapper.
- Evitare mapping inline complessi negli endpoint.

Esempio:
```csharp
public static class ShippingMapper
{
    public static UpdateShipmentStatusCommand ToUpdateShipmentStatusCommand(
        Guid shipmentId,
        UpdateShipmentStatusRequest request)
        => new(shipmentId, request.Status);

    public static ShipmentResponse ToResponse(ShipmentView view)
        => new(
            view.Id,
            view.OrderId,
            view.UserId,
            view.TrackingCode,
            view.Status,
            view.CreatedAtUtc,
            view.UpdatedAtUtc,
            view.DeliveredAtUtc);
}
```

## 6) Endpoint sottili
Regole:
- L'endpoint orchestra solo:
  - binding input
  - dispatch command/query
  - mapping response tramite mapper
  - codici HTTP/ProblemDetails
- Nessuna logica dominio negli endpoint.

Esempio:
```csharp
var command = ShippingMapper.ToUpdateShipmentStatusCommand(shipmentId, request);
var updated = await commandDispatcher.ExecuteAsync(command, cancellationToken);
return updated is null
    ? TypedResults.NotFound()
    : TypedResults.Ok(ShippingMapper.ToResponse(updated));
```

## 7) Naming conventions
- Request: `<Action><Entity>Request`
- Response: `<Entity>Response` o `<Action><Entity>Response`
- Mapper: `<ModuleOrFeature>Mapper`
- Metodi mapper:
  - `To<CommandName>()`
  - `To<PayloadName>()`
  - `ToResponse()` / `To<SpecificResponseName>()`

## 8) Anti-pattern da evitare
- `new ...Response(...)` ripetuti in piu endpoint dello stesso modulo.
- Mapping annidato lungo direttamente nei metodi endpoint.
- Utilizzare DTO di integrazione come request HTTP pubblici.
- Aggiungere logica di stato/invarianti dentro i mapper.
