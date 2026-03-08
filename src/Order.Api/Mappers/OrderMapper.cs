using Order.Api.Contracts.Requests;
using Order.Api.Contracts.Responses;
using Order.Application.Commands;
using Order.Application.Views;

namespace Order.Api.Mappers;

public static class OrderMapper
{
	public static CreateOrderCommand ToCreateCommand(this CreateOrderRequest request, string correlationId)
	{
		var customer = request.Customer ?? new OrderCustomerRequest(string.Empty, string.Empty, string.Empty, string.Empty);
		var shippingAddress = request.ShippingAddress ?? new OrderAddressRequest(string.Empty, string.Empty, string.Empty, string.Empty);
		var billingAddress = request.BillingAddress ?? new OrderAddressRequest(string.Empty, string.Empty, string.Empty, string.Empty);

		return new CreateOrderCommand(
			request.CartId,
			request.UserId,
			request.IdentityType,
			request.PaymentMethod,
			request.Items.Select(x => new CreateOrderItemCommand(x.ProductId, x.Sku, x.Name, x.Quantity, x.UnitPrice)).ToArray(),
			request.TotalAmount,
			request.AuthenticatedUserId,
			request.AnonymousId,
			new CreateOrderCustomerCommand(customer.FirstName, customer.LastName, customer.Email, customer.Phone),
			new CreateOrderAddressCommand(shippingAddress.Street, shippingAddress.City, shippingAddress.PostalCode, shippingAddress.Country),
			new CreateOrderAddressCommand(billingAddress.Street, billingAddress.City, billingAddress.PostalCode, billingAddress.Country),
			correlationId);
	}

	public static OrderResponse ToResponse(this OrderView view)
	{
		return new OrderResponse(
			view.Id,
			view.CartId,
			view.UserId,
			NormalizeIdentityType(view.IdentityType),
			NormalizePaymentMethod(view.PaymentMethod),
			view.AuthenticatedUserId,
			view.AnonymousId,
			new OrderCustomerResponse(view.Customer.FirstName, view.Customer.LastName, view.Customer.Email, view.Customer.Phone),
			new OrderAddressResponse(view.ShippingAddress.Street, view.ShippingAddress.City, view.ShippingAddress.PostalCode, view.ShippingAddress.Country),
			new OrderAddressResponse(view.BillingAddress.Street, view.BillingAddress.City, view.BillingAddress.PostalCode, view.BillingAddress.Country),
			NormalizeStatus(view.Status),
			view.TotalAmount,
			view.Items.Select(x => new OrderItemResponse(x.ProductId, x.Sku, x.Name, x.Quantity, x.UnitPrice)).ToArray(),
			NullIfWhiteSpace(view.TrackingCode),
			NullIfWhiteSpace(view.TransactionId),
			NullIfWhiteSpace(view.FailureReason));
	}

	private static string NormalizeStatus(string value)
	{
		if (string.Equals(value, "Cancelled", StringComparison.OrdinalIgnoreCase))
		{
			return "Failed";
		}

		return value;
	}

	private static string NormalizeIdentityType(string value)
	{
		if (string.Equals(value, "authenticated", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(value, "registered", StringComparison.OrdinalIgnoreCase))
		{
			return "Registered";
		}

		if (string.Equals(value, "anonymous", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(value, "guest", StringComparison.OrdinalIgnoreCase))
		{
			return "Anonymous";
		}

		return value;
	}

	private static string NormalizePaymentMethod(string value)
	{
		var normalized = value.Trim().ToLowerInvariant().Replace("-", string.Empty).Replace("_", string.Empty).Replace(" ", string.Empty);

		return normalized switch
		{
			"card" or "creditcard" or "stripecard" => "stripe_card",
			"paypal" => "paypal",
			"satispay" => "satispay",
			_ => value
		};
	}

	private static string? NullIfWhiteSpace(string value)
	{
		return string.IsNullOrWhiteSpace(value) ? null : value;
	}
}
