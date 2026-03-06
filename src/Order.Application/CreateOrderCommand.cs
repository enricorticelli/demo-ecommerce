using System.ComponentModel.DataAnnotations;
using Shared.BuildingBlocks.Cqrs;

namespace Order.Application;

public sealed record CreateOrderCommand(Guid CartId, Guid UserId) : ICommand<OrderCreationResult?>, IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CartId == Guid.Empty)
        {
            yield return new ValidationResult("The CartId field must be a non-empty GUID.", [nameof(CartId)]);
        }

        if (UserId == Guid.Empty)
        {
            yield return new ValidationResult("The UserId field must be a non-empty GUID.", [nameof(UserId)]);
        }
    }
}
