using System.ComponentModel.DataAnnotations;

namespace Cart.Api.Contracts.Requests;

public sealed record AddCartItemRequest(
    Guid ProductId,
    [property: Required, StringLength(64)] string Sku,
    [property: Required, StringLength(256)] string Name,
    [property: Range(1, int.MaxValue)] int Quantity,
    [property: Range(typeof(decimal), "0.01", "79228162514264337593543950335")] decimal UnitPrice);
