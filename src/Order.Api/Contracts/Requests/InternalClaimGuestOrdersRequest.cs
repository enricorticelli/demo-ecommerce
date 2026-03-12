namespace Order.Api.Contracts.Requests;

public sealed record InternalClaimGuestOrdersRequest(Guid AuthenticatedUserId, string CustomerEmail);
