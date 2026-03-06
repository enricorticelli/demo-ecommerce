using System.ComponentModel.DataAnnotations;
using Shared.BuildingBlocks.Cqrs;

namespace Cart.Application;

public sealed class AddCartItemToCartCommandValidator : IRequestValidator<AddCartItemToCartCommand>
{
    public IReadOnlyDictionary<string, string[]> Validate(AddCartItemToCartCommand request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.CartId == Guid.Empty)
        {
            errors[nameof(request.CartId)] = ["The CartId field must be a non-empty GUID."];
        }

        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(request.Item, new ValidationContext(request.Item), validationResults, true);
        foreach (var validationResult in validationResults)
        {
            var message = validationResult.ErrorMessage ?? "The value is invalid.";
            var members = validationResult.MemberNames.Any() ? validationResult.MemberNames : ["request"];
            foreach (var member in members)
            {
                if (errors.TryGetValue(member, out var existing))
                {
                    errors[member] = existing.Concat([message]).Distinct().ToArray();
                    continue;
                }

                errors[member] = [message];
            }
        }

        return errors;
    }
}
