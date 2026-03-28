namespace Order.Application.Commands;

public sealed record ClaimGuestOrdersCommand(Guid AuthenticatedUserId, string CustomerEmail);
