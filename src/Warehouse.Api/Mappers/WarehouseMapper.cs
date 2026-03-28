using Warehouse.Api.Contracts.Requests;
using Warehouse.Api.Contracts.Responses;
using Warehouse.Application.Commands;
using Warehouse.Application.Views;

namespace Warehouse.Api.Mappers;

public static class WarehouseMapper
{
	public static UpsertStockCommand ToCommand(this UpsertStockRequest request)
	{
		return new UpsertStockCommand(request.ProductId, request.Sku, request.AvailableQuantity);
	}

	public static ReserveStockCommand ToCommand(this ReserveStockRequest request)
	{
		return new ReserveStockCommand(
			request.OrderId,
			request.Items.Select(x => new ReserveStockItemCommand(x.ProductId, x.Sku, x.Name, x.Quantity, x.UnitPrice)).ToArray());
	}

	public static UpsertStockResponse ToResponse(this WarehouseStockView view)
	{
		return new UpsertStockResponse(view.ProductId, view.Sku, view.AvailableQuantity);
	}

	public static ReserveStockResponse ToResponse(this WarehouseReserveView view)
	{
		return new ReserveStockResponse(view.OrderId, view.Reserved, view.Reason);
	}

	public static GetStockByProductsResponse ToResponse(this IReadOnlyList<WarehouseStockView> views)
	{
		return new GetStockByProductsResponse(
			views.Select(x => new WarehouseStockItemResponse(x.ProductId, x.Sku, x.AvailableQuantity)).ToArray());
	}
}
