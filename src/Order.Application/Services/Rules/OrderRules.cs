using Order.Application.Abstractions.Rules;
using Order.Application.Commands;
using Shared.BuildingBlocks.Exceptions;

namespace Order.Application.Services.Rules;

public sealed class OrderRules : IOrderRules
{
    public void EnsureCreateCommandIsValid(CreateOrderCommand command)
    {
        if (command.Items.Count == 0)
        {
            throw new ValidationAppException("Order must contain at least one item.");
        }

        if (command.Customer is null)
        {
            throw new ValidationAppException("Customer information is required.");
        }

        if (command.ShippingAddress is null)
        {
            throw new ValidationAppException("Shipping address is required.");
        }

        if (command.BillingAddress is null)
        {
            throw new ValidationAppException("Billing address is required.");
        }

        if (command.TotalAmount <= 0)
        {
            throw new ValidationAppException("Order total amount must be greater than zero.");
        }

        var normalizedIdentity = command.IdentityType.Trim().ToLowerInvariant();
        if (normalizedIdentity == "authenticated" && !command.AuthenticatedUserId.HasValue)
        {
            throw new ValidationAppException("Authenticated orders require an authenticated user id.");
        }

        if (normalizedIdentity == "anonymous" && !command.AnonymousId.HasValue)
        {
            throw new ValidationAppException("Anonymous orders require an anonymous id.");
        }
    }

    public string NormalizeTrackingCode(string? trackingCode)
    {
        return string.IsNullOrWhiteSpace(trackingCode) ? "manual" : trackingCode.Trim();
    }

    public string NormalizeTransactionId(string? transactionId)
    {
        return string.IsNullOrWhiteSpace(transactionId) ? "manual" : transactionId.Trim();
    }

    public string NormalizeCancelReason(string? reason)
    {
        return string.IsNullOrWhiteSpace(reason) ? "Cancelled manually" : reason.Trim();
    }
}
