using System.ComponentModel.DataAnnotations;

namespace Order.Api.Contracts.Requests;

public sealed record ManualCancelOrderRequest([property: StringLength(256)] string? Reason);
