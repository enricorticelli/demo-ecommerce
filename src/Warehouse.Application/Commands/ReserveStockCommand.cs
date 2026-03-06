using Shared.BuildingBlocks.Contracts;
using Shared.BuildingBlocks.Cqrs;

namespace Warehouse.Application;

public sealed record ReserveStockCommand(StockReserveRequestedV1 Request) : ICommand<StockReservationResult>;
